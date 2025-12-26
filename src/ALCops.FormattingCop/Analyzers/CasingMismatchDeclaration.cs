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
public sealed class CasingMismatchDeclaration : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.CasingMismatch,
            DiagnosticDescriptors.CasingMismatchImproveDiagnostic);

    // These SyntaxKind identifiers are not relevant for analysis.
    // Excluding these from the stack while collecting nodes to improve performance.
    private static readonly HashSet<SyntaxKind> _skipAnalyzeIdentifierKinds = new HashSet<SyntaxKind>
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

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSymbolAction(
            this.CheckNodes,
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
    }

    // Collecting nodes by traversing from the top of the object into into's childnodes
    // For each childnode determine to add the node to the stack for analyzing
    // Grouping nodes for increase performance
    private void CheckNodes(SymbolAnalysisContext ctx)
    {
        var node = ctx.Symbol.DeclaringSyntaxReference?.GetSyntax(ctx.CancellationToken);
        if (node is null)
            return;

        Dictionary<AnalyzeKind, List<SyntaxNode>> collectedNodes = new();
        var semanticModel = ctx.Compilation.GetSemanticModel(node.SyntaxTree);

        CollectNodes(ctx, node, collectedNodes);
        AnalyzeNodes(ctx, semanticModel, collectedNodes);
    }

    #region Collect Nodes

    private static void CollectNodes(SymbolAnalysisContext ctx, SyntaxNode root, Dictionary<AnalyzeKind, List<SyntaxNode>> collectedNodes)
    {
        var stack = new Stack<SyntaxNode>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();
            SyntaxNode node = stack.Pop();
            var nodeKind = node.Kind;

            switch (nodeKind)
            {
                // These SyntaxKind don't have any ChildNodes (early exit)
                case var _ when nodeKind == EnumProvider.SyntaxKind.DataType ||
                                nodeKind == EnumProvider.SyntaxKind.LengthDataType ||
                                nodeKind == EnumProvider.SyntaxKind.OptionDataType ||
                                nodeKind == EnumProvider.SyntaxKind.TextConstDataType:
                    AddToCollection(AnalyzeKind.DataType, node, collectedNodes);
                    continue;

                // Possible ChildNodes (ObjectNameReference => IdentifierName) or (CommaSeparatedIdentifierEqualsLiteralList => IdentifierEqualsLiteral)
                case var _ when nodeKind == EnumProvider.SyntaxKind.EnumDataType ||
                                nodeKind == EnumProvider.SyntaxKind.LabelDataType:
                    AddToCollection(AnalyzeKind.DataType, node, collectedNodes);
                    // Continue processing ChildNodes (fall through to end of switch)
                    break;

                case var _ when nodeKind == EnumProvider.SyntaxKind.FieldGroup:
                    CollectFieldGroup(stack, node);
                    // The ChildNodes are handled by the CollectFieldGroup method
                    continue;

                case var _ when nodeKind == EnumProvider.SyntaxKind.IdentifierEqualsLiteral:
                    AddToCollection(AnalyzeKind.IdentifierEqualsLiteral, node, collectedNodes);
                    // ChildNodes are handeld by the AnalyzeIdentifierEqualsLiteraly method
                    continue;

                case var _ when nodeKind == EnumProvider.SyntaxKind.IdentifierName:
                    CollectIdentifierName(stack, node, collectedNodes);
                    // ChildNodes are handeld by the AnalyzeIdentifierEqualsLiteraly method
                    continue;

                case var _ when nodeKind == EnumProvider.SyntaxKind.Key:
                    CollectKey(stack, node);
                    // ChildNodes are handled by the CollectKey method
                    continue;

                case var _ when nodeKind == EnumProvider.SyntaxKind.MemberAccessExpression:
                    CollectMemberAccessExpression(stack, node);
                    // ChildNodes handled by CollectMemberAccessExpression method
                    continue;

                case var _ when nodeKind == EnumProvider.SyntaxKind.MemberAttribute:
                    CollectMemberAttribute(stack, node, collectedNodes);
                    // ChildNodes handled by CollectMemberAttribute method
                    continue;

                case var _ when nodeKind == EnumProvider.SyntaxKind.ObjectNameReference:
                    // TODO: Example case interface "MyInterfaceExt" extends "MyInterface"
                    // Exclude "MyInterface" from analyzing (currently not supported)
                    if (node.Parent.Kind == EnumProvider.SyntaxKind.Interface)
                        continue;
                    // else fall through to process ChildNodes
                    break;

                case var _ when nodeKind == EnumProvider.SyntaxKind.OptionAccessExpression:
                    AddToCollection(AnalyzeKind.OptionAccessExpression, node, collectedNodes);
                    // ChildNodes handled by AnalyzeOptionAccessExpression method
                    continue;

                case var _ when nodeKind == EnumProvider.SyntaxKind.PageArea:
                    AddToCollection(AnalyzeKind.Area, node, collectedNodes);
                    // Continue processing ChildNodes (fall through to end of switch)
                    break;

                case var _ when nodeKind == EnumProvider.SyntaxKind.PageActionArea:
                    AddToCollection(AnalyzeKind.ActionArea, node, collectedNodes);
                    // Continue processing ChildNodes (fall through to end of switch)
                    break;

                case var _ when nodeKind == EnumProvider.SyntaxKind.Property:
                    CollectProperty(stack, node, collectedNodes);
                    // ChildNodes handled by AnalyzeProperty method
                    continue;

                case var _ when nodeKind == EnumProvider.SyntaxKind.PropertyName:
                    AddToCollection(AnalyzeKind.PropertyName, node, collectedNodes);
                    // ChildNodes handled by AnalyzePropertyName method
                    continue;

                case var _ when nodeKind == EnumProvider.SyntaxKind.SubtypedDataType:
                    CollectSubtypedDataType(node, collectedNodes);
                    // ChildNodes handled by AnalyzePropertyName method
                    continue;

                case var _ when nodeKind == EnumProvider.SyntaxKind.QualifiedName:
                    AddToCollection(AnalyzeKind.QualifiedName, node, collectedNodes);
                    // ChildNodes handled by AnalyzeQualifiedName method
                    continue;

                case var _ when nodeKind == EnumProvider.SyntaxKind.TriggerDeclaration:
                    AddToCollection(AnalyzeKind.TriggerDeclaration, node, collectedNodes);
                    // ChildNodes handled by AnalyzeTriggerDeclaration method
                    continue;
            }

            bool skipIdentifier = _skipAnalyzeIdentifierKinds.Contains(node.Kind);
#if DEBUG
            // The .Reverse() creates an intermediate IEnumerable<SyntaxNode> collection, leading to extra memory allocation
            // However, during debugging, it is helpful to process the ChildNodes in a top-down sequence for easier analysis
            foreach (var child in node.ChildNodes().Reverse())
#else
                foreach (var child in node.ChildNodes())
#endif
            {
                SyntaxKind childKind = child.Kind;
                if (childKind == EnumProvider.SyntaxKind.ObjectId ||
                    childKind == EnumProvider.SyntaxKind.LiteralAttributeArgument ||
                    childKind == EnumProvider.SyntaxKind.LiteralExpression
                )
                    continue;

                if (childKind == EnumProvider.SyntaxKind.IdentifierName && skipIdentifier)
                    continue;

                if (IsEmptyList(child))
                    continue;

                stack.Push(child);
            }
        }
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
            ParameterListSyntax paramList => paramList.Parameters.Count == 0,
            PropertyListSyntax propList => propList.Properties.Count == 0,
            _ => false
        };

    private static void AddToCollection(AnalyzeKind kind, SyntaxNode node, Dictionary<AnalyzeKind, List<SyntaxNode>> collectedNodes)
    {
        if (!collectedNodes.TryGetValue(kind, out var list))
        {
            list = new List<SyntaxNode>();
            collectedNodes[kind] = list;
        }
        list.Add(node);
    }

    private static void CollectFieldGroup(Stack<SyntaxNode> stack, SyntaxNode syntaxNode)
    {
        if (syntaxNode is not FieldGroupSyntax node)
            return;

        var children = node.ChildNodes().ToArray();

        // Exclude the IdentifierName (DropDown/Brick)
        int startIndex = (children.Length > 0 && children[0] is IdentifierNameSyntax) ? 1 : 0;

        for (int i = startIndex; i < children.Length; i++)
        {
            stack.Push(children[i]);
        }
    }

    private static void CollectIdentifierName(Stack<SyntaxNode> stack, SyntaxNode syntaxNode, Dictionary<AnalyzeKind, List<SyntaxNode>> collectedNodes)
    {
        if (syntaxNode is not IdentifierNameSyntax node)
            return;

        // Mitigate false positive in Links/Notes systempart
        if (node.Parent.Kind == EnumProvider.SyntaxKind.PageSystemPart)
            return;

        if (string.Equals(node.Identifier.ValueText, "Rec", StringComparison.OrdinalIgnoreCase))
            return;

        // Handle system objects, like AccessByPermission = system "Allow Action Export To Excel" = X;
        // The AL0443 diagnostic will handle this https://learn.microsoft.com/nl-be/dynamics365/business-central/dev-itpro/developer/diagnostics/diagnostic-al443
        if (node.Parent?.Parent is PermissionSyntax permissionSyntax &&
            permissionSyntax.ObjectType.Kind == EnumProvider.SyntaxKind.SystemKeyword)
        {
            return;
        }

        AddToCollection(AnalyzeKind.IdentifierName, node, collectedNodes);
    }

    private static void CollectKey(Stack<SyntaxNode> stack, SyntaxNode syntaxNode)
    {
        if (syntaxNode is not KeySyntax node)
            return;

        foreach (var field in node.Fields)
        {
            stack.Push(field);
        }
    }

    private static void CollectMemberAccessExpression(Stack<SyntaxNode> stack, SyntaxNode syntaxNode)
    {
        if (syntaxNode is not MemberAccessExpressionSyntax node)
            return;

        // Extract the Expression and push it back into the stack.
        // In case of MyRecord.Get(), we're only interested in the MyRecord-part (IdentifierNameSyntax), the .Get() is already handeld
        // or MyRecord.MyField, we're only interested in the MyRecord-part (IdentifierNameSyntax), the .MyField is already handeld
        stack.Push(node.Expression);
    }

    private static void CollectMemberAttribute(Stack<SyntaxNode> stack, SyntaxNode syntaxNode, Dictionary<AnalyzeKind, List<SyntaxNode>> collectedNodes)
    {
        if (syntaxNode is not MemberAttributeSyntax node)
            return;

        var nodesToProcess = node.ChildNodes();
        foreach (var child in nodesToProcess)
        {
            if (child is IdentifierNameSyntax)
                AddToCollection(AnalyzeKind.Attribute, child, collectedNodes);
            else
                stack.Push(child);
        }
    }

    private static void CollectProperty(Stack<SyntaxNode> stack, SyntaxNode syntaxNode, Dictionary<AnalyzeKind, List<SyntaxNode>> collectedNodes)
    {
        if (syntaxNode is not PropertySyntax node)
            return;

        switch (node.Value)
        {
            case EnumPropertyValueSyntax:
                AddToCollection(AnalyzeKind.Property, syntaxNode, collectedNodes);
                break;

            case CommaSeparatedPropertyValueSyntax:
                // TODO: Find a way to validate the values of Application Area property (All, Basic, Suite)
                if (string.Equals(node.Name.Identifier.ValueText, "ApplicationArea", StringComparison.OrdinalIgnoreCase))
                {
                    stack.Push(node.Name);
                }
                else
                {
                    goto default;
                }
                break;

            case CommaSeparatedIdentifierOrLiteralPropertyValueSyntax:
                //TODO: The semanticModel.GetSymbolInfo on the AnalyzeIdentifierName can't resolve these identifiers, so skip adding these to the stack
                if (string.Equals(node.Name.Identifier.ValueText, "ValuesAllowed", StringComparison.OrdinalIgnoreCase))
                    return;
                goto default;

            case ImagePropertyValueSyntax:          // Images are handeld by the AL0482 compiler diagnostic
            case StringPropertyValueSyntax:         // Do not analyze StringLiterals
            case OptionValuePropertyValueSyntax:    // Do not analyze Option
            case OptionValuesPropertyValueSyntax:   // Do not analyze Options
                stack.Push(node.Name);
                break;

            default:
                foreach (var child in syntaxNode.ChildNodes())
                    stack.Push(child);
                break;
        }
    }

    private static void CollectSubtypedDataType(SyntaxNode syntaxNode, Dictionary<AnalyzeKind, List<SyntaxNode>> collectedNodes)
    {
        if (syntaxNode is not SubtypedDataTypeSyntax node)
            return;

        if (node.Subtype.Kind == EnumProvider.SyntaxKind.ObjectReference)
            AddToCollection(AnalyzeKind.DataType, syntaxNode, collectedNodes);
    }

    #endregion

    #region Analyze Nodes

    private void AnalyzeNodes(SymbolAnalysisContext ctx, SemanticModel semanticModel, Dictionary<AnalyzeKind, List<SyntaxNode>> collectedNodes)
    {
        foreach (var kvp in collectedNodes)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            AnalyzeKind kind = kvp.Key;
            List<SyntaxNode> nodes = kvp.Value;

            switch (kind)
            {
                case AnalyzeKind.DataType:
                    AnalyzeDataType(ctx, nodes);
                    continue;

                case AnalyzeKind.IdentifierEqualsLiteral:
                    AnalyzeIdentifierEqualsLiteraly(ctx, nodes, GetOrdinalDictionary(kind));
                    continue;

                case AnalyzeKind.IdentifierName:
                    AnalyzeIdentifierName(ctx, semanticModel, nodes);
                    continue;

                case AnalyzeKind.Attribute:
                    AnalyzeIdentifier(ctx, nodes, GetOrdinalDictionary(kind));
                    continue;

                case AnalyzeKind.OptionAccessExpression:
                    AnalyzeOptionAccessExpression(ctx, semanticModel, nodes);
                    continue;

                case AnalyzeKind.Area:
                case AnalyzeKind.ActionArea:
                    AnalyzeChildNodeIdentifiers(ctx, nodes, GetOrdinalDictionary(kind));
                    continue;

                case AnalyzeKind.Property:
                    AnalyzeProperty(ctx, nodes, GetOrdinalDictionary(kind));
                    break;

                case AnalyzeKind.PropertyName:
                    AnalyzePropertyName(ctx, nodes, GetOrdinalDictionary(kind));
                    break;

                case AnalyzeKind.QualifiedName:
                    AnalyzeQualifiedName(ctx, semanticModel, nodes);
                    break;

                case AnalyzeKind.TriggerDeclaration:
                    AnalyzeTriggerDeclaration(ctx, semanticModel, nodes);
                    break;
            }
        }
    }

    private static List<string> dataTypes = new List<string>();

    private void AnalyzeDataType(SymbolAnalysisContext ctx, List<SyntaxNode> nodes)
    {
        foreach (DataTypeSyntax dataTypeSyntax in nodes)
        {
            var name = dataTypeSyntax.TypeName.ValueText;
            if (string.IsNullOrEmpty(name))
                continue;
            if (!dataTypes.Contains(name))
                dataTypes.Add(name);
        }

        var syntaxNodes = nodes.OfType<DataTypeSyntax>();
        var lookupDictionary = _navTypeKindDictionary;

        foreach (var syntaxNode in syntaxNodes)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();
            CompareAgainstDictionary(ctx, syntaxNode.TypeName, lookupDictionary);
        }
    }

    private void AnalyzeIdentifierEqualsLiteraly(SymbolAnalysisContext ctx, List<SyntaxNode> nodes, Lazy<ImmutableDictionary<string, string>>? lookupDictionary)
    {
        // The EnumProvider.SyntaxKind.IdentifierEqualsLiteral currently are properties from a Caption
        // a Caption property (again currently) shares the same properties like Comment, Locked and/or MaxLength as a Label variable
        foreach (var syntaxNode in nodes)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();
            if (syntaxNode is IdentifierEqualsLiteralSyntax node)
                CompareAgainstDictionary(ctx, node.Identifier, lookupDictionary);
        }
    }

    private void AnalyzeIdentifierName(SymbolAnalysisContext ctx, SemanticModel semanticModel, List<SyntaxNode> nodes)
    {
        // Increasing performance on the GetSymbolInfo method by grouping nodes with the same Identifier
        var groupNodes = nodes
            .OfType<IdentifierNameSyntax>()
            // The GetSymbolInfo from the ctx.SemanticModel will throw an System.NullReferenceException on these ParentKind nodes
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
            // Special handling for 'continue' keyword as the semantic model will returns null on this identifier
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

    private void AnalyzeChildNodeIdentifiers(SymbolAnalysisContext ctx, IEnumerable<SyntaxNode> nodes, Lazy<ImmutableDictionary<string, string>>? lookupDictionary)
    {
        foreach (var syntaxNode in nodes)
            AnalyzeIdentifier(ctx, syntaxNode.ChildNodes(), lookupDictionary);
    }

    private void AnalyzeIdentifier(SymbolAnalysisContext ctx, IEnumerable<SyntaxNode> nodes, Lazy<ImmutableDictionary<string, string>>? lookupDictionary)
    {
        foreach (var syntaxNode in nodes)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();
            if (syntaxNode is IdentifierNameSyntax node)
                CompareAgainstDictionary(ctx, node.Identifier, lookupDictionary);
        }
    }

    private void AnalyzeOptionAccessExpression(SymbolAnalysisContext ctx, SemanticModel semanticModel, List<SyntaxNode> nodes)
    {
        var symbolKindDict = _symbolKindDictionary.Value;

        foreach (var syntaxNode in nodes)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();
            if (syntaxNode is not OptionAccessExpressionSyntax node)
                return;

            // Unwrap an inner OptionAccess if present
            var exprNode = node.Expression;
            if (exprNode is OptionAccessExpressionSyntax innerOption &&
                innerOption.Expression is IdentifierNameSyntax idName)
            {
                CompareAgainstDictionary(ctx, idName.Identifier, _symbolKindDictionary);
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

            // Check if Expression is SymbolKind
            bool isExpressionSymbolKind = symbolKindDict.ContainsKey(expressionText);
            if (isExpressionSymbolKind)
            {
                CompareAgainstDictionary(ctx, expression.Identifier, _symbolKindDictionary);

                // If the name is also a known SymbolKind; process and exit.
                if (symbolKindDict.ContainsKey(nameText))
                {
                    CompareAgainstDictionary(ctx, name.Identifier, _symbolKindDictionary);
                    return;
                }
            }

            // Otherwise, use the semantic model to get symbol info.
            if (semanticModel.GetSymbolInfo(node, ctx.CancellationToken).Symbol is not ISymbol symbol)
            {
#if DEBUG
                var message = $"SymbolInfo not available for '{name.Identifier.ValueText?.QuoteIdentifierIfNeededWithReflection()}' on OptionAccessExpressionSyntax.";
                RaiseImproveRuleDiagnostic(ctx, nodes, message);
#endif
                return;
            }
        }
    }

    private void AnalyzeProperty(SymbolAnalysisContext ctx, List<SyntaxNode> nodes, Lazy<ImmutableDictionary<string, string>>? lookupDictionary)
    {
        var propertyNames = nodes.OfType<PropertySyntax>()
                                 .Select(n => n.Name)
                                 .AsEnumerable<SyntaxNode>();
        AnalyzePropertyName(ctx, propertyNames, lookupDictionary);

        var propOrdinalDict = propertyOrdinalDictionary.Value;
        var syntaxNodes = nodes.OfType<PropertySyntax>();
        foreach (var node in syntaxNodes)
        {
            if (node.Name is not PropertyNameSyntax propertyNameSyntax)
                return;

            var propertyName = propertyNameSyntax.Identifier.ValueText;
            if (string.IsNullOrEmpty(propertyName))
                return;

            if (propOrdinalDict.ContainsKey(propertyName))
            {
                if (node.Value is EnumPropertyValueSyntax enumPropValueSyntax)
                {
                    CompareAgainstDictionary(ctx, enumPropValueSyntax.Value.Identifier, GetOrdinalDictionary(propertyName));
                }
            }
            else
            {
                if (node.Value is EnumPropertyValueSyntax)
                {
                    var message = $"Missing '{propertyName}' ordinals.";
                    RaiseImproveRuleDiagnostic(ctx, node.Value, message);
                }
            }
        }
    }

    private void AnalyzePropertyName(SymbolAnalysisContext ctx, IEnumerable<SyntaxNode> nodes, Lazy<ImmutableDictionary<string, string>>? lookupDictionary)
    {
        var syntaxNodes = nodes.OfType<PropertyNameSyntax>();
        foreach (var node in syntaxNodes)
            CompareAgainstDictionary(ctx, node.Identifier, lookupDictionary);
    }

    private void AnalyzeQualifiedName(SymbolAnalysisContext ctx, SemanticModel semanticModel, List<SyntaxNode> nodes)
    {
        // Increasing performance on the GetSymbolInfo() method by grouping nodes with the same Identifier
        var groupNodes = nodes
            .OfType<QualifiedNameSyntax>()
            // A small overhead for using .ToString() here as we need to include the node.Left and node.Right
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
                    // without namespace
                    if (symbol.ContainingSymbol is not IObjectTypeSymbol objectTypeSymbol)
                        return;

                    if (symbol.ContainingSymbol.Kind == EnumProvider.SymbolKind.TableExtension)
                    {
                        ITableExtensionTypeSymbol tableExtension = (ITableExtensionTypeSymbol)symbol.ContainingSymbol;
                        if (tableExtension.Target is not IObjectTypeSymbol tableExtensionTypeSymbol)
                        {
                            return;
                        }
                        objectTypeSymbol = tableExtensionTypeSymbol;
                    }

                    if (node.Left is IdentifierNameSyntax leftNode)
                    {
                        CompareIdentifier(ctx, leftNode.Identifier, objectTypeSymbol.Name);
                    }

                    if (node.Right is SimpleNameSyntax rightNode)
                    {
                        CompareIdentifier(ctx, rightNode.Identifier, symbol.Name);
                    }
                    break;
                }
                else
                {
                    // with namespace
                    CompareIdentifier(ctx, node.Right.Identifier, symbol.Name);
                }
            }
        }
    }

    private static void AnalyzeTriggerDeclaration(SymbolAnalysisContext ctx, SemanticModel semanticModel, List<SyntaxNode> nodes)
    {
        var syntaxNodes = nodes.OfType<TriggerDeclarationSyntax>();

        foreach (var node in syntaxNodes)
        {
            var symbol = semanticModel.GetDeclaredSymbol(node, ctx.CancellationToken);
            if (symbol is null)
                continue;

            CompareIdentifier(ctx, node.Name.Identifier, symbol.Name);
        }
    }

    #endregion

    #region Comparators

    private static Lazy<ImmutableDictionary<string, string>>? GetOrdinalDictionary(string identifier)
    {
        propertyOrdinalDictionary.Value.TryGetValue(identifier, out var dictionary);
        return dictionary;
    }

    private static Lazy<ImmutableDictionary<string, string>>? GetOrdinalDictionary(AnalyzeKind identifier)
    {
        analyzeKindOrdinalDictionary.Value.TryGetValue(identifier, out var dictionary);
        return dictionary;
    }

    private static readonly Lazy<ImmutableDictionary<string, string>> _labelPropertyDictionary = new(() =>
        LabelPropertyHelper.GetAllLabelProperties()
                            .ToImmutableDictionary(s => s, s => s, StringComparer.OrdinalIgnoreCase));
    private static readonly Lazy<ImmutableDictionary<string, string>> _navTypeKindDictionary = new(GenerateNavTypeKindDictionary);
    private static readonly Lazy<ImmutableDictionary<string, string>> _symbolKindDictionary = new(GenerateSymbolKindDictionary);

    private static readonly Lazy<Dictionary<string, Lazy<ImmutableDictionary<string, string>>>> propertyOrdinalDictionary = new(() =>
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
            { "TableType",              EnumProvider.TableTypeKind.CanonicalNames } ,
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

    private static readonly Lazy<Dictionary<AnalyzeKind, Lazy<ImmutableDictionary<string, string>>>> analyzeKindOrdinalDictionary = new(() =>
        new Dictionary<AnalyzeKind, Lazy<ImmutableDictionary<string, string>>>()
        {
            { AnalyzeKind.Attribute,                EnumProvider.AttributeKind.CanonicalNames },
            { AnalyzeKind.Area,                     EnumProvider.AreaKind.CanonicalNames },
            { AnalyzeKind.ActionArea,               EnumProvider.ActionAreaKind.CanonicalNames },
            { AnalyzeKind.Property,                 EnumProvider.PropertyKind.CanonicalNames },
            { AnalyzeKind.PropertyName,             EnumProvider.PropertyKind.CanonicalNames },
            { AnalyzeKind.IdentifierEqualsLiteral,  _labelPropertyDictionary }
        });

    private static void CompareIdentifier(SymbolAnalysisContext ctx, SyntaxToken identifier, string? canonical)
    {
        string? tokenText = identifier.ValueText?.UnquoteIdentifier();
        if (string.IsNullOrEmpty(tokenText))
            return;

        if (string.IsNullOrEmpty(canonical))
            return;

        CompareIdentifier(ctx, identifier, tokenText, canonical);
    }

    private static void CompareIdentifier(
        SymbolAnalysisContext ctx,
        string identifierValueText,
        IEnumerable<SyntaxToken> identifiers,
        string canonical)
    {
        // Use spans to increase performance to compare the token text with the canonical value.
        ReadOnlySpan<char> tokenSpan = identifierValueText.AsSpan();
        ReadOnlySpan<char> canonicalSpan = canonical.AsSpan();

        if (!tokenSpan.Equals(canonicalSpan, StringComparison.Ordinal))
        {
            foreach (var identifier in identifiers)
            {
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
                        identifierValueText));
            }
        }
    }

    private void CompareAgainstDictionary(
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
            var message = $"Missing ordinals for '{tokenText}'.";
            RaiseImproveRuleDiagnostic(ctx, identifier, message);
            return;
        }

        if (!lookupDict.TryGetValue(tokenText, out string? canonical))
        {
            var message = $"Redundant analysis of '{tokenText}'.";
            RaiseImproveRuleDiagnostic(ctx, identifier, message);
            return;
        }

        CompareIdentifier(ctx, identifier, tokenText, canonical);
    }

    private static void CompareIdentifier(SymbolAnalysisContext ctx, SyntaxToken identifier, string token, string canonical)
    {
        // Use spans to increase performance to compare the token text with the canonical value.
        ReadOnlySpan<char> tokenSpan = token.AsSpan();
        ReadOnlySpan<char> canonicalSpan = canonical.AsSpan();

        if (!tokenSpan.Equals(canonicalSpan, StringComparison.Ordinal))
        {
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
                    token));
        }
    }

    #endregion

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

    private static void RaiseImproveRuleDiagnostic(SymbolAnalysisContext ctx, SyntaxNode node, string message)
    {
        RaiseImproveRuleDiagnostic(ctx, node.GetLocation(), message);
    }

    private static void RaiseImproveRuleDiagnostic(SymbolAnalysisContext ctx, SyntaxToken token, string message)
    {
        RaiseImproveRuleDiagnostic(ctx, token.GetLocation(), message);
    }

    private static void RaiseImproveRuleDiagnostic(SymbolAnalysisContext ctx, Location location, string message)
    {
        ctx.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.CasingMismatchImproveDiagnostic,
                location,
                message));
    }

    private enum AnalyzeKind
    {
        DataType,
        IdentifierEqualsLiteral,
        IdentifierName,
        Attribute,
        OptionAccessExpression,
        Area,
        ActionArea,
        Property,
        PropertyName,
        QualifiedName,
        TriggerDeclaration
    }

    #region Dictionary Builders

    private static ImmutableDictionary<string, string> GenerateNavTypeKindDictionary()
    {
        var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kind in Enum.GetNames(typeof(NavTypeKind)))
        {
            builder[kind] = kind;
        }

        // Add additional entry for Database::"G/L Entry" (there is no NavTypeKind for this)
        builder["Database"] = "Database";

        return builder.ToImmutable();
    }

    private static ImmutableDictionary<string, string> GenerateSymbolKindDictionary()
    {
        var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kind in Enum.GetNames(typeof(SymbolKind)))
        {
            // Change "XmlPort" to "Xmlport"
            var key = kind == "XmlPort" ? "Xmlport" : kind;
            builder[key] = key;
        }

        // Add additional entry for Database::"G/L Entry" (there is no NavTypeKind for this)
        builder["Database"] = "Database";

        // Add additional entry for ObjectType::Table (there is no NavTypeKind for this)
        builder["ObjectType"] = "ObjectType";

        return builder.ToImmutable();
    }

    #endregion
}