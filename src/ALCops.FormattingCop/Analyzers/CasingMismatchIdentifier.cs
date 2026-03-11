using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.FormattingCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class CasingMismatchIdentifier : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.CasingMismatch,
            DiagnosticDescriptors.CasingMismatchImproveDiagnostic);

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

    #region Declaration Analysis

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

    /// <summary>
    /// Recursively walks the syntax tree, handling dictionary-resolvable cases inline
    /// and collecting nodes that require semantic model resolution for batch processing.
    /// </summary>
    private void WalkNode(
        SymbolAnalysisContext ctx,
        SyntaxNode node,
        List<IdentifierNameSyntax> identifiers,
        List<QualifiedNameSyntax> qualifiedNames,
        List<TriggerDeclarationSyntax> triggers,
        bool skipChildIdentifiers = false)
    {
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
                    WalkNode(ctx, child, identifiers, qualifiedNames, triggers);
                continue;
            }

            // --- Properties: name + value dictionaries ---

            if (child is PropertySyntax prop)
            {
                HandleProperty(ctx, prop, identifiers, qualifiedNames, triggers);
                continue;
            }

            if (child is PropertyNameSyntax propName)
            {
                CompareAgainstDictionary(ctx, propName.Identifier, EnumProvider.PropertyKind.CanonicalNames);
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
                        WalkNode(ctx, attrChild, identifiers, qualifiedNames, triggers);
                }
                continue;
            }

            // --- Caption sub-properties: LabelProperty dictionary ---

            if (child is IdentifierEqualsLiteralSyntax idEqualsLit)
            {
                CompareAgainstDictionary(ctx, idEqualsLit.Identifier, _labelPropertyDictionary);
                continue;
            }

            // --- Areas: AreaKind / ActionAreaKind dictionaries ---

            if (kind == EnumProvider.SyntaxKind.PageArea)
            {
                AnalyzeChildNodeIdentifiers(ctx, child, EnumProvider.AreaKind.CanonicalNames);
                WalkNode(ctx, child, identifiers, qualifiedNames, triggers);
                continue;
            }

            if (kind == EnumProvider.SyntaxKind.PageActionArea)
            {
                AnalyzeChildNodeIdentifiers(ctx, child, EnumProvider.ActionAreaKind.CanonicalNames);
                WalkNode(ctx, child, identifiers, qualifiedNames, triggers);
                continue;
            }

            // --- OptionAccess: SymbolKind dictionary ---

            if (child is OptionAccessExpressionSyntax optAccess)
            {
                AnalyzeOptionAccess(ctx, optAccess);
                continue;
            }

            // --- Nodes collected for semantic batch resolution ---

            if (child is TriggerDeclarationSyntax trigger)
            {
                triggers.Add(trigger);
                WalkNode(ctx, child, identifiers, qualifiedNames, triggers, skipChildIdentifiers: true);
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
                    ProcessNode(ctx, children[i], identifiers, qualifiedNames, triggers);
                continue;
            }

            if (child is KeySyntax keySyntax)
            {
                foreach (var field in keySyntax.Fields)
                    ProcessNode(ctx, field, identifiers, qualifiedNames, triggers);
                continue;
            }

            if (kind == EnumProvider.SyntaxKind.ObjectNameReference)
            {
                if (child.Parent?.Kind != EnumProvider.SyntaxKind.Interface)
                    WalkNode(ctx, child, identifiers, qualifiedNames, triggers);
                continue;
            }

            // --- Identifiers: collect for semantic resolution ---

            if (child is IdentifierNameSyntax idName)
            {
                if (!skipChildIdentifiers && ShouldCollectIdentifier(idName))
                    identifiers.Add(idName);
                continue;
            }

            // --- Default: recurse into child nodes ---

            if (IsEmptyList(child))
                continue;

            bool skipIds = _skipIdentifierParentKinds.Contains(kind);
            WalkNode(ctx, child, identifiers, qualifiedNames, triggers, skipIds);
        }
    }

    private void HandleProperty(
        SymbolAnalysisContext ctx,
        PropertySyntax prop,
        List<IdentifierNameSyntax> identifiers,
        List<QualifiedNameSyntax> qualifiedNames,
        List<TriggerDeclarationSyntax> triggers)
    {
        CompareAgainstDictionary(ctx, prop.Name.Identifier, EnumProvider.PropertyKind.CanonicalNames);

        switch (prop.Value)
        {
            case EnumPropertyValueSyntax enumVal:
                var propName = prop.Name.Identifier.ValueText;
                if (!string.IsNullOrEmpty(propName))
                {
                    if (_propertyValueDictionaries.Value.TryGetValue(propName, out var dict))
                        CompareAgainstDictionary(ctx, enumVal.Value.Identifier, dict);
#if DEBUG
                    else
                        RaiseImproveRuleDiagnostic(ctx, prop.Value, $"Missing '{propName}' ordinals.");
#endif
                }
                break;

            case CommaSeparatedPropertyValueSyntax when
                 string.Equals(prop.Name.Identifier.ValueText, "ApplicationArea", StringComparison.OrdinalIgnoreCase):
                break;

            case CommaSeparatedIdentifierOrLiteralPropertyValueSyntax when
                 string.Equals(prop.Name.Identifier.ValueText, "ValuesAllowed", StringComparison.OrdinalIgnoreCase):
                break;

            case ImagePropertyValueSyntax:
            case StringPropertyValueSyntax:
            case OptionValuePropertyValueSyntax:
            case OptionValuesPropertyValueSyntax:
                break;

            default:
                foreach (var propChild in prop.ChildNodes())
                {
                    if (propChild is not PropertyNameSyntax)
                        WalkNode(ctx, propChild, identifiers, qualifiedNames, triggers);
                }
                break;
        }
    }

    /// <summary>
    /// Processes a single node that may be a leaf (IdentifierName, QualifiedName) or a subtree.
    /// Unlike WalkNode which walks children, this also handles the node itself.
    /// </summary>
    private void ProcessNode(
        SymbolAnalysisContext ctx,
        SyntaxNode node,
        List<IdentifierNameSyntax> identifiers,
        List<QualifiedNameSyntax> qualifiedNames,
        List<TriggerDeclarationSyntax> triggers)
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
            WalkNode(ctx, node, identifiers, qualifiedNames, triggers);
        }
    }

    private static bool ShouldCollectIdentifier(IdentifierNameSyntax idName)
    {
        if (idName.Parent?.Kind == EnumProvider.SyntaxKind.PageSystemPart)
            return false;

        if (string.Equals(idName.Identifier.ValueText, "Rec", StringComparison.OrdinalIgnoreCase))
            return false;

        if (idName.Parent?.Parent is PermissionSyntax permissionSyntax &&
            permissionSyntax.ObjectType.Kind == EnumProvider.SyntaxKind.SystemKeyword)
            return false;

        return true;
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
                CompareAgainstDictionary(ctx, name.Identifier, _symbolKindDictionary);
        }
    }

    #endregion

    #region Semantic Resolution (Phase 2)

    private static void ResolveIdentifiers(
        SymbolAnalysisContext ctx,
        SemanticModel semanticModel,
        List<IdentifierNameSyntax> identifiers)
    {
        var groupNodes = identifiers
            .Where(node => node.Parent.Kind != EnumProvider.SyntaxKind.PragmaWarningDirectiveTrivia &&
                           node.Parent.Kind != EnumProvider.SyntaxKind.UnaryNotExpression)
            .ToLookup(node => node.Identifier.ValueText, StringComparer.Ordinal);

#if NET8_0_OR_GREATER
        var continueKeywordText = SyntaxFacts.GetText(EnumProvider.SyntaxKind.ContinueKeyword);
#endif

        foreach (var groupNode in groupNodes)
        {
            var representative = groupNode.OrderBy(node => node.Position).Last();

#if NET8_0_OR_GREATER
            if (string.Equals(representative.Identifier.ValueText, continueKeywordText, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var node in groupNode)
                {
                    ctx.CancellationToken.ThrowIfCancellationRequested();
                    CompareIdentifier(ctx, node.Identifier, continueKeywordText);
                }
                continue;
            }
#endif

            if (semanticModel.GetSymbolInfo(representative, ctx.CancellationToken).Symbol is not ISymbol symbol)
            {
#if DEBUG
                var message = $"SymbolInfo not available for '{representative.Identifier.ValueText?.QuoteIdentifierIfNeededWithReflection()}' on IdentifierNameSyntax.";
                RaiseImproveRuleDiagnostic(ctx, groupNode, message);
#endif
                continue;
            }

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
            var representative = groupNode.OrderBy(node => node.Position).Last();
            var symbol = semanticModel.GetSymbolInfo(representative, ctx.CancellationToken).Symbol;

            if (symbol is null)
            {
#if DEBUG
                var message = $"SymbolInfo not available for '{representative.ToString().QuoteIdentifierIfNeededWithReflection()}' on QualifiedNameSyntax.";
                RaiseImproveRuleDiagnostic(ctx, groupNode, message);
#endif
                continue;
            }

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

    #region Comparators

    private static void CompareIdentifier(SymbolAnalysisContext ctx, SyntaxToken identifier, string? canonical)
    {
        string? tokenText = identifier.ValueText?.UnquoteIdentifier();
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
        string? tokenText = identifier.ValueText?.UnquoteIdentifier();
        if (string.IsNullOrEmpty(tokenText))
            return;

        var lookupDict = lookupDictionary?.Value;

        if (lookupDict is null)
        {
#if DEBUG
            RaiseImproveRuleDiagnostic(ctx, identifier, $"Missing ordinals for '{tokenText}'.");
#endif
            return;
        }

        if (!lookupDict.TryGetValue(tokenText, out string? canonical))
        {
#if DEBUG
            RaiseImproveRuleDiagnostic(ctx, identifier, $"Redundant analysis of '{tokenText}'.");
#endif
            return;
        }

        CompareIdentifier(ctx, identifier, canonical);
    }

    #endregion

    #region Debug Diagnostics

    private static void RaiseImproveRuleDiagnostic(SymbolAnalysisContext ctx, IEnumerable<SyntaxNode> nodes, string message)
    {
        foreach (var node in nodes)
            RaiseImproveRuleDiagnostic(ctx, node.GetLocation(), message);
    }

    private static void RaiseImproveRuleDiagnostic(SymbolAnalysisContext ctx, IEnumerable<SyntaxToken> tokens, string message)
    {
        foreach (var token in tokens)
            RaiseImproveRuleDiagnostic(ctx, token.GetLocation(), message);
    }

    private static void RaiseImproveRuleDiagnostic(SymbolAnalysisContext ctx, SyntaxNode node, string message) =>
        RaiseImproveRuleDiagnostic(ctx, node.GetLocation(), message);

    private static void RaiseImproveRuleDiagnostic(SymbolAnalysisContext ctx, SyntaxToken token, string message) =>
        RaiseImproveRuleDiagnostic(ctx, token.GetLocation(), message);

    private static void RaiseImproveRuleDiagnostic(SymbolAnalysisContext ctx, Location location, string message)
    {
        ctx.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.CasingMismatchImproveDiagnostic,
                location,
                message));
    }

    #endregion

    #region Dictionaries

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

    private static readonly Lazy<Dictionary<string, Lazy<ImmutableDictionary<string, string>>>> _propertyValueDictionaries = new(() =>
        new Dictionary<string, Lazy<ImmutableDictionary<string, string>>>(StringComparer.OrdinalIgnoreCase)
        {
            { "Access",                 EnumProvider.Accessibility.CanonicalNames },
            { "AllowInCustomizations",  EnumProvider.AllowInCustomizationsKind.CanonicalNames },
            { "BlankNumbers",           EnumProvider.BlankNumbersKind.CanonicalNames },
            { "Caption",                _labelPropertyDictionary },
            { "CompressionType",        EnumProvider.CompressionTypeKind.CanonicalNames },
            { "CustomActionType",       EnumProvider.CustomActionTypeKind.CanonicalNames },
            { "CueGroupLayout",         EnumProvider.CuegroupLayoutKind.CanonicalNames },
            { "DataAccessIntent",       EnumProvider.MergeCanonicalNames(
                                            EnumProvider.PageDataAccessIntentKind.CanonicalNames,
                                            EnumProvider.QueryDataAccessIntentKind.CanonicalNames,
                                            EnumProvider.ReportDataAccessIntentKind.CanonicalNames) },
            { "DataClassification",     EnumProvider.DataClassificationKind.CanonicalNames },
            { "DefaultLayout",          EnumProvider.DefaultLayoutKind.CanonicalNames },
            { "Direction",              EnumProvider.DirectionKind.CanonicalNames },
            { "Encoding",               EnumProvider.EncodingKind.CanonicalNames },
            { "EventSubscriberInstance",EnumProvider.EventSubscriberInstanceKind.CanonicalNames },
            { "ExtendedDatatype",       EnumProvider.ExtendedDatatypeKind.CanonicalNames },
            { "ExternalAccess",         EnumProvider.ExternalAccessKind.CanonicalNames },
            { "FieldClass",             EnumProvider.FieldClassKind.CanonicalNames },
            { "FieldValidate",          EnumProvider.FieldValidateKind.CanonicalNames },
            { "Format",                 EnumProvider.FormatKind.CanonicalNames },
            { "FormatEvaluate",         EnumProvider.FormatEvaluateKind.CanonicalNames },
            { "Gesture",                EnumProvider.GestureKind.CanonicalNames },
            { "GridLayout",             EnumProvider.GridLayoutKind.CanonicalNames },
            { "Importance",             EnumProvider.ImportanceKind.CanonicalNames },
            { "MaskType",               EnumProvider.MaskTypeKind.CanonicalNames },
            { "MaxOccurs",              EnumProvider.MaxOccursKind.CanonicalNames },
            { "Method",                 EnumProvider.QueryColumnMethodKind.CanonicalNames },
            { "MinOccurs",              EnumProvider.MinOccursKind.CanonicalNames },
            { "Multiplicity",           EnumProvider.MultiplicityKind.CanonicalNames },
            { "ObsoleteState",          EnumProvider.MergeCanonicalNames(
                                            EnumProvider.FieldClassKind.CanonicalNames,
                                            EnumProvider.FieldObsoleteStateKind.CanonicalNames) },
            { "Occurrence",             EnumProvider.OccurrenceKind.CanonicalNames },
            { "PaperSourceDefaultPage", EnumProvider.PaperSourceDefaultPageKind.CanonicalNames },
            { "PaperSourceFirstPage",   EnumProvider.PaperSourceFirstPageKind.CanonicalNames },
            { "PaperSourceLastPage",    EnumProvider.PaperSourceLastPageKind.CanonicalNames },
            { "QueryType",              EnumProvider.QueryTypeKind.CanonicalNames },
            { "Scope",                  EnumProvider.MergeCanonicalNames(
                                            EnumProvider.TableScopeKind.CanonicalNames,
                                            EnumProvider.PageActionScopeKind.CanonicalNames) },
            { "ShowAs",                 EnumProvider.ShowAsKind.CanonicalNames },
            { "SqlDataType",            EnumProvider.SqlDataTypeKind.CanonicalNames },
            { "SqlJoinType",            EnumProvider.SqlJoinTypeKind.CanonicalNames },
            { "PageType",               EnumProvider.PageTypeKind.CanonicalNames },
            { "PdfFontEmbedding",       EnumProvider.PdfFontEmbeddingKind.CanonicalNames },
            { "PreviewMode",            EnumProvider.PreviewModeKind.CanonicalNames },
            { "PromotedCategory",       EnumProvider.PromotedCategoryKind.CanonicalNames },
            { "PromptMode",             EnumProvider.PromptModeKind.CanonicalNames },
            { "ReadState",              EnumProvider.ReadStateKind.CanonicalNames },
            { "RoleType",               EnumProvider.EntitlementRoleTypeKind.CanonicalNames },
            { "RunPageMode",            EnumProvider.RunPageModeKind.CanonicalNames },
            { "Style",                  EnumProvider.StyleKind.CanonicalNames },
            { "Subtype",                EnumProvider.MergeCanonicalNames(
                                            EnumProvider.CodeunitSubtypeKind.CanonicalNames,
                                            EnumProvider.FieldSubtypeKind.CanonicalNames) },
            { "TableType",              EnumProvider.TableTypeKind.CanonicalNames },
            { "TestHttpRequestPolicy",  EnumProvider.TestHttpRequestPolicyKind.CanonicalNames },
            { "TestIsolation",          EnumProvider.TestIsolationKind.CanonicalNames },
            { "TestPermissions",        EnumProvider.TestPermissionsKind.CanonicalNames },
            { "TextEncoding",           EnumProvider.TextEncodingKind.CanonicalNames },
            { "TextType",               EnumProvider.TextTypeKind.CanonicalNames },
            { "Type",                   EnumProvider.MergeCanonicalNames(
                                            EnumProvider.TypeKind.CanonicalNames,
                                            EnumProvider.EntitlementTypeKind.CanonicalNames) },
            { "TransactionType",        EnumProvider.TransactionTypeKind.CanonicalNames },
            { "TreeInitialState",       EnumProvider.TreeInitialStateKind.CanonicalNames },
            { "UpdatePropagation",      EnumProvider.UpdatePropagationKind.CanonicalNames },
            { "UsageCategory",          EnumProvider.UsageCategoryKind.CanonicalNames },
            { "XmlVersionNo",           EnumProvider.XmlVersionNoKind.CanonicalNames }
        });

    #endregion

    #region Helpers

    // These parent kinds declare new names — skip their IdentifierName children
    // to avoid checking user-defined names against the semantic model.
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
            ParameterListSyntax paramList => paramList.Parameters.Count == 0,
            PropertyListSyntax propList => propList.Properties.Count == 0,
            _ => false
        };

    #endregion
}
