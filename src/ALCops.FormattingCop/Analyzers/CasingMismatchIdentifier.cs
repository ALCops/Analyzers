using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.FormattingCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class CasingMismatchIdentifier : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.CasingMismatch);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSymbolAction(
            AnalyzeDeclarations,
                EnumProvider.SymbolKind.Codeunit,
                EnumProvider.SymbolKind.Entitlement,
                EnumProvider.SymbolKind.Enum,
                EnumProvider.SymbolKind.EnumExtension,
                EnumProvider.SymbolKind.Interface,
                EnumProvider.SymbolKind.Page,
                EnumProvider.SymbolKind.PageExtension,
                EnumProvider.SymbolKind.PermissionSet,
                EnumProvider.SymbolKind.PermissionSetExtension,
                EnumProvider.SymbolKind.Profile,
                EnumProvider.SymbolKind.ProfileExtension,
                EnumProvider.SymbolKind.Query,
                EnumProvider.SymbolKind.Report,
                EnumProvider.SymbolKind.ReportExtension,
                EnumProvider.SymbolKind.Table,
                EnumProvider.SymbolKind.TableExtension,
                EnumProvider.SymbolKind.XmlPort);

    /// <summary>
    /// Entry point: walks the syntax tree for one AL object declaration, collects nodes
    /// that need batched semantic resolution, then resolves them.
    /// </summary>
    private void AnalyzeDeclarations(SymbolAnalysisContext ctx)
    {
        var root = ctx.Symbol.DeclaringSyntaxReference?.GetSyntax(ctx.CancellationToken);
        if (root is null)
            return;

        var semanticModel = ctx.Compilation.GetSemanticModel(root.SyntaxTree);
        var identifiers = new List<IdentifierNameSyntax>();
        var qualifiedNames = new List<QualifiedNameSyntax>();
        var triggers = new List<TriggerDeclarationSyntax>();

        WalkNode(ctx, root, identifiers, qualifiedNames, triggers);

        ResolveIdentifiers(ctx, semanticModel, identifiers);
        ResolveQualifiedNames(ctx, semanticModel, qualifiedNames);
        ResolveTriggers(ctx, semanticModel, triggers);
    }

    #region Tree Walk

    /// <summary>
    /// Iteratively walks the syntax tree using an explicit stack (avoids StackOverflowException
    /// on deeply nested trees such as long concatenation chains). Handles dictionary-resolvable
    /// cases inline, collecting nodes that require batched semantic resolution.
    /// </summary>
    private static void WalkNode(
        SymbolAnalysisContext ctx,
        SyntaxNode root,
        List<IdentifierNameSyntax> identifiers,
        List<QualifiedNameSyntax> qualifiedNames,
        List<TriggerDeclarationSyntax> triggers,
        bool skipChildIdentifiers = false)
    {
        var stack = new Stack<(SyntaxNode node, bool skipIds)>();
        stack.Push((root, skipChildIdentifiers));

        while (stack.Count > 0)
        {
            var (node, currentSkipIds) = stack.Pop();

            foreach (var child in node.ChildNodes())
            {
                ctx.CancellationToken.ThrowIfCancellationRequested();

                var kind = child.Kind;

                if (kind == EnumProvider.SyntaxKind.ObjectId ||
                    kind == EnumProvider.SyntaxKind.LiteralAttributeArgument ||
                    kind == EnumProvider.SyntaxKind.LiteralExpression)
                    continue;

                // --- Data types: NavTypeKind dictionary ---

                if (child is SubtypedDataTypeSyntax subtyped)
                {
                    if (subtyped.Subtype.Kind == EnumProvider.SyntaxKind.ObjectReference)
                        CompareAgainstDictionary(ctx, subtyped.TypeName, _navTypeKindDictionary);
                    continue;
                }

                if (child is DataTypeSyntax dataType)
                {
                    CompareAgainstDictionary(ctx, dataType.TypeName, _navTypeKindDictionary);
                    if (kind == EnumProvider.SyntaxKind.EnumDataType ||
                        kind == EnumProvider.SyntaxKind.LabelDataType)
                        stack.Push((child, false));
                    continue;
                }

                // --- Properties: dictionary-based resolution ---

                if (child is PropertySyntax prop)
                {
                    HandleProperty(ctx, prop, identifiers, qualifiedNames, triggers, stack);
                    continue;
                }

                if (child is PropertyNameSyntax propName)
                {
                    ResolvePropertyName(ctx, propName);
                    continue;
                }

                // --- Attributes: AttributeKind dictionary ---

                if (child is MemberAttributeSyntax)
                {
                    foreach (var attrChild in child.ChildNodes())
                    {
                        if (attrChild is IdentifierNameSyntax attrName)
                            CompareAgainstDictionary(ctx, attrName.Identifier, EnumProvider.AttributeKind.CanonicalNames);
                        else
                            stack.Push((attrChild, false));
                    }
                    continue;
                }

                // --- Label sub-properties (Comment, Locked, MaxLength) ---

                if (child is IdentifierEqualsLiteralSyntax idEqualsLit)
                {
                    CompareAgainstDictionary(ctx, idEqualsLit.Identifier, _labelPropertyDictionary);
                    continue;
                }

                // --- Page areas: AreaKind / ActionAreaKind dictionaries ---

                if (kind == EnumProvider.SyntaxKind.PageArea)
                {
                    AnalyzeChildNodeIdentifiers(ctx, child, EnumProvider.AreaKind.CanonicalNames);
                    stack.Push((child, false));
                    continue;
                }

                if (kind == EnumProvider.SyntaxKind.PageActionArea)
                {
                    AnalyzeChildNodeIdentifiers(ctx, child, EnumProvider.ActionAreaKind.CanonicalNames);
                    stack.Push((child, false));
                    continue;
                }

                // --- Option access: SymbolKind dictionary (e.g., ObjectType::Table) ---

                if (child is OptionAccessExpressionSyntax optAccess)
                {
                    AnalyzeOptionAccess(ctx, optAccess);
                    continue;
                }

                // --- Nodes collected for batched semantic resolution ---

                if (child is TriggerDeclarationSyntax trigger)
                {
                    triggers.Add(trigger);
                    stack.Push((child, true));
                    continue;
                }

                if (child is QualifiedNameSyntax qname)
                {
                    qualifiedNames.Add(qname);
                    continue;
                }

                // --- Structural nodes with specific child handling ---

                if (child is FieldGroupSyntax)
                {
                    var children = child.ChildNodes().ToArray();
                    int start = (children.Length > 0 && children[0] is IdentifierNameSyntax) ? 1 : 0;
                    for (int i = start; i < children.Length; i++)
                        EnqueueNode(children[i], identifiers, qualifiedNames, stack);
                    continue;
                }

                if (child is KeySyntax keySyntax)
                {
                    foreach (var field in keySyntax.Fields)
                        EnqueueNode(field, identifiers, qualifiedNames, stack);
                    continue;
                }

                if (kind == EnumProvider.SyntaxKind.ObjectNameReference)
                {
                    if (child.Parent?.Kind != EnumProvider.SyntaxKind.Interface)
                        stack.Push((child, false));
                    continue;
                }

                // --- Identifiers: collect for batched semantic resolution ---

                if (child is IdentifierNameSyntax idName)
                {
                    if (!currentSkipIds && ShouldCollectIdentifier(idName))
                        identifiers.Add(idName);
                    continue;
                }

                // --- Default: walk child nodes via the stack ---

                if (IsEmptyList(child))
                    continue;

                bool skipIds = _skipIdentifierParentKinds.Contains(kind);
                stack.Push((child, skipIds));
            }
        }
    }

    /// <summary>
    /// Enqueues a single node: collects it if it's an identifier or qualified name,
    /// otherwise pushes it onto the walk stack for further processing.
    /// </summary>
    private static void EnqueueNode(
        SyntaxNode node,
        List<IdentifierNameSyntax> identifiers,
        List<QualifiedNameSyntax> qualifiedNames,
        Stack<(SyntaxNode node, bool skipIds)> stack)
    {
        if (node is IdentifierNameSyntax idName)
        {
            if (ShouldCollectIdentifier(idName))
                identifiers.Add(idName);
        }
        else if (node is QualifiedNameSyntax qname)
        {
            qualifiedNames.Add(qname);
        }
        else
        {
            stack.Push((node, false));
        }
    }

    private static bool ShouldCollectIdentifier(IdentifierNameSyntax idName)
    {
        var parentKind = idName.Parent?.Kind;

        if (parentKind == EnumProvider.SyntaxKind.PageSystemPart)
            return false;

        if (parentKind == EnumProvider.SyntaxKind.PragmaWarningDirectiveTrivia ||
            parentKind == EnumProvider.SyntaxKind.UnaryNotExpression)
            return false;

        if (string.Equals(idName.Identifier.ValueText, "Rec", StringComparison.OrdinalIgnoreCase))
            return false;

        if (idName.Parent?.Parent is PermissionSyntax permissionSyntax &&
            permissionSyntax.ObjectType.Kind == EnumProvider.SyntaxKind.SystemKeyword)
            return false;

        return true;
    }

    private static bool IsEmptyList(SyntaxNode node) =>
        node switch
        {
            ArgumentListSyntax argList => argList.Arguments.Count == 0,
            AttributeArgumentListSyntax attrList => attrList.Arguments.Count == 0,
            FieldGroupListSyntax fldGrpList => fldGrpList.FieldGroups.Count == 0,
            FieldGroupExtensionListSyntax fldGrpExtList => fldGrpExtList.Changes.Count == 0,
            FieldListSyntax fldList => fldList.Fields.Count == 0,
            FieldExtensionListSyntax fldExtList => fldExtList.Fields.Count == 0,
            KeyListSyntax keyList => keyList.Keys.Count == 0,
            PageActionListSyntax pageActList => pageActList.Areas.Count == 0,
            PageActionAreaSyntax pageActArea => pageActArea.Actions.Count == 0,
            PageExtensionActionListSyntax pageExtActList => pageExtActList.Changes.Count == 0,
            PageViewListSyntax pageViewList => pageViewList.Views.Count == 0,
            PageExtensionViewListSyntax pageExtViewList => pageExtViewList.Changes.Count == 0,
            // COMPAT: PageAnalysisViewListSyntax/PageExtensionAnalysisViewListSyntax only exist in net10.0+ SDK.
            // Use SyntaxKind matching + ChildNodes() to avoid compile-time dependency.
            _ when node.Kind == EnumProvider.SyntaxKind.PageAnalysisViewList
                || node.Kind == EnumProvider.SyntaxKind.PageExtensionAnalysisViewList
                => !node.ChildNodes().Any(),
            ParameterListSyntax paramList => paramList.Parameters.Count == 0,
            PropertyListSyntax propList => propList.Properties.Count == 0,
            _ => false
        };

    #endregion

    #region Inline Resolution

    /// <summary>
    /// Handles property syntax: resolves the property name and enum property value
    /// via dictionary lookups, and walks non-trivial property value children.
    /// </summary>
    private static void HandleProperty(
        SymbolAnalysisContext ctx,
        PropertySyntax prop,
        List<IdentifierNameSyntax> identifiers,
        List<QualifiedNameSyntax> qualifiedNames,
        List<TriggerDeclarationSyntax> triggers,
        Stack<(SyntaxNode node, bool skipIds)> stack)
    {
        ResolvePropertyName(ctx, prop.Name);

        switch (prop.Value)
        {
            case EnumPropertyValueSyntax enumVal:
                ResolveEnumPropertyValue(ctx, prop, enumVal);
                break;

            case CommaSeparatedPropertyValueSyntax when
                 string.Equals(prop.Name.Identifier.ValueText, "ApplicationArea", StringComparison.OrdinalIgnoreCase):
            case CommaSeparatedIdentifierOrLiteralPropertyValueSyntax when
                 string.Equals(prop.Name.Identifier.ValueText, "ValuesAllowed", StringComparison.OrdinalIgnoreCase):
            case ImagePropertyValueSyntax:
            case StringPropertyValueSyntax:
            case OptionValuePropertyValueSyntax:
            case OptionValuesPropertyValueSyntax:
                break;

            default:
                foreach (var propChild in prop.ChildNodes())
                {
                    if (propChild is not PropertyNameSyntax)
                        stack.Push((propChild, false));
                }
                break;
        }
    }

    /// <summary>
    /// Resolves property name casing via PropertyKind.CanonicalNames dictionary lookup.
    /// Property names are a closed set defined by the SDK, so no semantic model call is needed.
    /// </summary>
    private static void ResolvePropertyName(SymbolAnalysisContext ctx, PropertyNameSyntax propName)
    {
        CompareAgainstDictionary(ctx, propName.Identifier, EnumProvider.PropertyKind.CanonicalNames);
    }

    /// <summary>
    /// Resolves enum property value casing. Derives PropertyKind from the property name string
    /// and looks up the dynamically-discovered canonical names for value comparison.
    /// </summary>
    private static void ResolveEnumPropertyValue(
        SymbolAnalysisContext ctx,
        PropertySyntax prop,
        EnumPropertyValueSyntax enumVal)
    {
        var propName = prop.Name.Identifier.ValueText;
        if (string.IsNullOrEmpty(propName))
            return;

        if (_enumPropertyValuesByName.Value.TryGetValue(propName, out var dict))
            CompareAgainstDictionary(ctx, enumVal.Value.Identifier, dict);
    }

    private static void AnalyzeChildNodeIdentifiers(
        SymbolAnalysisContext ctx,
        SyntaxNode node,
        Lazy<ImmutableDictionary<string, string>> lookupDictionary)
    {
        foreach (var child in node.ChildNodes())
        {
            if (child is IdentifierNameSyntax idName)
                CompareAgainstDictionary(ctx, idName.Identifier, lookupDictionary);
        }
    }

    private static void AnalyzeOptionAccess(SymbolAnalysisContext ctx, OptionAccessExpressionSyntax node)
    {
        var symbolKindDict = _symbolKindDictionary.Value;

        var exprNode = node.Expression;
        if (exprNode is OptionAccessExpressionSyntax innerOption &&
            innerOption.Expression is IdentifierNameSyntax innerIdName)
        {
            CompareAgainstDictionary(ctx, innerIdName.Identifier, _symbolKindDictionary);
            exprNode = innerOption.Name;
        }

        if (exprNode is not IdentifierNameSyntax expression)
            return;

        string? expressionText = expression.Identifier.ValueText;
        if (string.IsNullOrEmpty(expressionText))
            return;

        if (node.Name is not IdentifierNameSyntax name)
            return;

        string? nameText = name.Identifier.ValueText;
        if (string.IsNullOrEmpty(nameText))
            return;

        if (symbolKindDict.ContainsKey(expressionText))
        {
            CompareAgainstDictionary(ctx, expression.Identifier, _symbolKindDictionary);

            if (symbolKindDict.ContainsKey(nameText))
            {
                var memberDict = string.Equals(expressionText, "ObjectType", StringComparison.OrdinalIgnoreCase)
                    ? _objectTypeMemberDictionary
                    : _symbolKindDictionary;
                CompareAgainstDictionary(ctx, name.Identifier, memberDict);
            }
        }
    }

    #endregion

    #region Batched Semantic Resolution

    private static void ResolveIdentifiers(
        SymbolAnalysisContext ctx,
        SemanticModel semanticModel,
        List<IdentifierNameSyntax> identifiers)
    {
        var groupNodes = identifiers
            .ToLookup(node => node.Identifier.ValueText, StringComparer.Ordinal);

        foreach (var groupNode in groupNodes)
        {
            IdentifierNameSyntax? representative = null;
            foreach (var n in groupNode)
            {
                if (representative is null || n.Position > representative.Position)
                    representative = n;
            }

            if (representative is null)
                continue;

            if (representative.Identifier.ValueText is string identifierText
                && KeywordTexts.Value.Contains(identifierText))
                continue;

            if (semanticModel.GetSymbolInfo(representative, ctx.CancellationToken).Symbol is not ISymbol symbol)
                continue;

            foreach (var node in groupNode)
            {
                ctx.CancellationToken.ThrowIfCancellationRequested();
                CompareIdentifier(ctx, node.Identifier, symbol.Name);
            }
        }
    }

    private static void ResolveQualifiedNames(
        SymbolAnalysisContext ctx,
        SemanticModel semanticModel,
        List<QualifiedNameSyntax> qualifiedNames)
    {
        var groupNodes = qualifiedNames
            .ToLookup(node => node.ToString(), StringComparer.OrdinalIgnoreCase);

        foreach (var groupNode in groupNodes)
        {
            QualifiedNameSyntax? representative = null;
            foreach (var n in groupNode)
            {
                if (representative is null || n.Position > representative.Position)
                    representative = n;
            }

            if (representative is null)
                continue;

            var symbol = semanticModel.GetSymbolInfo(representative, ctx.CancellationToken).Symbol;

            if (symbol is null)
                continue;

            foreach (var node in groupNode)
            {
                if (representative.Left.Kind == EnumProvider.SyntaxKind.IdentifierName)
                {
                    if (symbol.ContainingSymbol is not IObjectTypeSymbol objectTypeSymbol)
                        return;

                    if (symbol.ContainingSymbol.Kind == EnumProvider.SymbolKind.TableExtension)
                    {
                        ITableExtensionTypeSymbol tableExtension = (ITableExtensionTypeSymbol)symbol.ContainingSymbol;
                        if (tableExtension.Target is not IObjectTypeSymbol tableExtensionTypeSymbol)
                            return;
                        objectTypeSymbol = tableExtensionTypeSymbol;
                    }

                    if (node.Left is IdentifierNameSyntax leftNode)
                        CompareIdentifier(ctx, leftNode.Identifier, objectTypeSymbol.Name);

                    if (node.Right is SimpleNameSyntax rightNode)
                        CompareIdentifier(ctx, rightNode.Identifier, symbol.Name);

                    break;
                }
                else
                {
                    CompareIdentifier(ctx, node.Right.Identifier, symbol.Name);
                }
            }
        }
    }

    private static void ResolveTriggers(
        SymbolAnalysisContext ctx,
        SemanticModel semanticModel,
        List<TriggerDeclarationSyntax> triggers)
    {
        foreach (var trigger in triggers)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            var symbol = semanticModel.GetDeclaredSymbol(trigger, ctx.CancellationToken);
            if (symbol is null)
                continue;

            CompareIdentifier(ctx, trigger.Name.Identifier, symbol.Name);
        }
    }

    #endregion

    #region Comparison and Reporting

    private static void CompareIdentifier(SymbolAnalysisContext ctx, SyntaxToken identifier, string? canonical)
    {
        string? tokenText = identifier.ValueText?.UnquoteIdentifier();
        ReportIfCasingMismatch(ctx, identifier, tokenText, canonical);
    }

    private static void ReportIfCasingMismatch(SymbolAnalysisContext ctx, SyntaxToken identifier, string? tokenText, string? canonical)
    {
        if (string.IsNullOrEmpty(tokenText) || string.IsNullOrEmpty(canonical))
            return;

        ReadOnlySpan<char> tokenSpan = tokenText.AsSpan();
        ReadOnlySpan<char> canonicalSpan = canonical.AsSpan();

        if (tokenSpan.Equals(canonicalSpan, StringComparison.Ordinal))
            return;

        var location = identifier.GetLocation();
        if (location is null)
            return;

        var properties = ImmutableDictionary<string, string>.Empty
            .Add("CanonicalText", canonical);

        ctx.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.CasingMismatch,
                location,
                properties,
                canonical,
                tokenText));
    }

    private static void CompareAgainstDictionary(
        SymbolAnalysisContext ctx,
        SyntaxToken identifier,
        Lazy<ImmutableDictionary<string, string>>? lookupDictionary)
    {
        if (lookupDictionary is null)
            return;

        CompareAgainstDictionary(ctx, identifier, lookupDictionary.Value);
    }

    private static void CompareAgainstDictionary(
        SymbolAnalysisContext ctx,
        SyntaxToken identifier,
        ImmutableDictionary<string, string>? lookupDictionary)
    {
        if (lookupDictionary is null)
            return;

        string? tokenText = identifier.ValueText?.UnquoteIdentifier();
        if (string.IsNullOrEmpty(tokenText))
            return;

        if (!lookupDictionary.TryGetValue(tokenText, out string? canonical))
            return;

        ReportIfCasingMismatch(ctx, identifier, tokenText, canonical);
    }

    #endregion

    #region Static Data

    // All AL keyword texts for filtering out identifiers named after keywords.
    // User-defined names matching keywords (e.g., a variable called "Action") are the user's choice.
    private static readonly Lazy<HashSet<string>> KeywordTexts = new(() =>
        Enum.GetNames(typeof(SyntaxKind))
            .Where(name => name.AsSpan().EndsWith("Keyword"))
            .Select(name => Enum.Parse<SyntaxKind>(name))
            .Select(SyntaxFactory.Token)
            .Where(token => token.Kind != SyntaxKind.None)
            .Select(token => token.ValueText)
            .Where(text => !string.IsNullOrEmpty(text))
            .ToHashSet(StringComparer.OrdinalIgnoreCase)!,
        LazyThreadSafetyMode.PublicationOnly);

    private static readonly Lazy<ImmutableDictionary<string, string>> _labelPropertyDictionary = new(() =>
        LabelPropertyHelper.GetAllLabelProperties()
                            .ToImmutableDictionary(s => s, s => s, StringComparer.OrdinalIgnoreCase));

    private static readonly Lazy<ImmutableDictionary<string, string>> _navTypeKindDictionary = new(() =>
    {
        var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kind in Enum.GetNames(typeof(NavTypeKind)))
            builder[kind] = kind;
        builder["Database"] = "Database";
        return builder.ToImmutable();
    });

    private static readonly Lazy<ImmutableDictionary<string, string>> _symbolKindDictionary = new(() =>
    {
        var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kind in Enum.GetNames(typeof(SymbolKind)))
        {
            var key = kind == "XmlPort" ? "Xmlport" : kind;
            builder[key] = key;
        }
        builder["Database"] = "Database";
        builder["ObjectType"] = "ObjectType";
        return builder.ToImmutable();
    });

    // ObjectType option members use SymbolKind enum names directly (e.g., XmlPort, not Xmlport).
    private static readonly Lazy<ImmutableDictionary<string, string>> _objectTypeMemberDictionary = new(() =>
        Enum.GetNames(typeof(SymbolKind))
            .ToImmutableDictionary(s => s, s => s, StringComparer.OrdinalIgnoreCase));

    // Dynamically discovers canonical enum property values from the SDK's PropertyInfoLookup.
    // Iterates all SymbolKind × PropertyKind combinations and merges options per PropertyKind.
    // Self-maintaining: new enum properties added to the AL SDK are automatically picked up.
    private static readonly Lazy<Dictionary<PropertyKind, ImmutableDictionary<string, string>>> _enumPropertyValuesByKind = new(() =>
    {
        var result = new Dictionary<PropertyKind, ImmutableDictionary<string, string>>();

        var lookupMethod = typeof(PropertyInfoLookup).GetMethod("Lookup", BindingFlags.Public | BindingFlags.Static);
        if (lookupMethod is null)
            return result;

        Type[] sdkTypes;
        try { sdkTypes = typeof(PropertyInfoLookup).Assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { sdkTypes = ex.Types.Where(t => t != null).ToArray()!; }

        var enumPropTypeInfo = sdkTypes.FirstOrDefault(t => t?.Name == "EnumPropertyTypeInfo");
        if (enumPropTypeInfo is null)
            return result;

        var optionsProp = enumPropTypeInfo.GetProperty("Options", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (optionsProp is null)
            return result;

        PropertyInfo? nameProp = null;

#if NETSTANDARD2_1
        foreach (var sk in (SymbolKind[])Enum.GetValues(typeof(SymbolKind)))
#else
        foreach (var sk in Enum.GetValues<SymbolKind>())
#endif
        {
#if NETSTANDARD2_1
            foreach (var pk in (PropertyKind[])Enum.GetValues(typeof(PropertyKind)))
#else
            foreach (var pk in Enum.GetValues<PropertyKind>())
#endif
            {
                try
                {
                    var info = lookupMethod.Invoke(null, new object[] { sk, pk });
                    if (info?.GetType() != enumPropTypeInfo)
                        continue;

                    if (optionsProp.GetValue(info) is not IEnumerable options)
                        continue;

                    if (!result.TryGetValue(pk, out _))
                        result[pk] = ImmutableDictionary<string, string>.Empty;

                    var builder = result[pk].ToBuilder();
                    builder.KeyComparer = StringComparer.OrdinalIgnoreCase;

                    foreach (var opt in options)
                    {
                        nameProp ??= opt.GetType().GetProperty("Name");
                        if (nameProp?.GetValue(opt) is string name && !builder.ContainsKey(name))
                            builder[name] = name;
                    }

                    result[pk] = builder.ToImmutable();
                }
                catch
                {
                    // Silently skip combinations that fail (version compatibility)
                }
            }
        }

        return result;
    }, LazyThreadSafetyMode.PublicationOnly);

    // String-keyed version of _enumPropertyValuesByKind for direct property name lookups,
    // avoiding the need for GetDeclaredSymbol to obtain the PropertyKind enum value.
    private static readonly Lazy<Dictionary<string, ImmutableDictionary<string, string>>> _enumPropertyValuesByName = new(() =>
    {
        var result = new Dictionary<string, ImmutableDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in _enumPropertyValuesByKind.Value)
            result[kvp.Key.ToString()] = kvp.Value;
        return result;
    }, LazyThreadSafetyMode.PublicationOnly);

    // Declaration nodes whose child identifiers should be skipped (user-defined names).
    private static readonly HashSet<SyntaxKind> _skipIdentifierParentKinds = new HashSet<SyntaxKind>
    {
        EnumProvider.SyntaxKind.CodeunitObject,
        EnumProvider.SyntaxKind.ControlAddInObject,
        EnumProvider.SyntaxKind.EnumType,
        EnumProvider.SyntaxKind.EnumValue,
        EnumProvider.SyntaxKind.EnumExtensionType,
        EnumProvider.SyntaxKind.Entitlement,
        EnumProvider.SyntaxKind.Field,
        EnumProvider.SyntaxKind.Interface,
        EnumProvider.SyntaxKind.MethodDeclaration,
        EnumProvider.SyntaxKind.Parameter,
        EnumProvider.SyntaxKind.QueryColumn,
        EnumProvider.SyntaxKind.QueryDataItem,
        EnumProvider.SyntaxKind.QueryFilter,
        EnumProvider.SyntaxKind.QueryObject,
        EnumProvider.SyntaxKind.PageObject,
        EnumProvider.SyntaxKind.PageExtensionObject,
        EnumProvider.SyntaxKind.PageCustomizationObject,
        EnumProvider.SyntaxKind.PermissionSet,
        EnumProvider.SyntaxKind.PermissionSetExtension,
        EnumProvider.SyntaxKind.ProfileObject,
        EnumProvider.SyntaxKind.ProfileExtensionObject,
        EnumProvider.SyntaxKind.ReportColumn,
        EnumProvider.SyntaxKind.ReportObject,
        EnumProvider.SyntaxKind.ReportExtensionObject,
        EnumProvider.SyntaxKind.ReportDataItem,
        EnumProvider.SyntaxKind.ReportLabel,
        EnumProvider.SyntaxKind.ReportLayout,
        EnumProvider.SyntaxKind.ReturnValue,
        EnumProvider.SyntaxKind.TableObject,
        EnumProvider.SyntaxKind.TableExtensionObject,
        EnumProvider.SyntaxKind.XmlPortObject
    };

    #endregion
}
