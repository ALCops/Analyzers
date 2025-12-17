using System.Collections.Immutable;
using System.Reflection;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public class ObjectIdInDeclaration : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.ObjectIdInDeclaration);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(new Action<SyntaxNodeAnalysisContext>(this.AnalyzeSyntaxNode),
            EnumProvider.SyntaxKind.ObjectReference,
            EnumProvider.SyntaxKind.PermissionValue);

    private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        if (ctx.Node is not ObjectNameOrIdSyntax node)
            return;

        if (node.Identifier is not ObjectIdSyntax identifier)
            return;

        if (identifier.Value.Kind != EnumProvider.SyntaxKind.Int32LiteralToken)
            return;

        if (identifier.Value.Value is not int id)
            return;

        SymbolKind symbolKind = GetSymbolKind(ctx.Node.Parent);
        if (symbolKind == EnumProvider.SymbolKind.Undefined)
            return;

        var applicationObjectTypeSymbol = ctx.SemanticModel.Compilation.GetApplicationObjectTypeSymbolsByIdAcrossModulesWithReflection(symbolKind, id).FirstOrDefault();
        if (applicationObjectTypeSymbol == null)
            return;

        var properties = ImmutableDictionary<string, string>.Empty
            .Add("IdentifierName", applicationObjectTypeSymbol.Name.QuoteIdentifierIfNeededWithReflection());

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ObjectIdInDeclaration,
            ctx.Node.GetLocation(),
            properties,
            id,
            applicationObjectTypeSymbol.Name));
    }

    private static SymbolKind GetSymbolKind(SyntaxNode node)
    {
        var syntaxKind = GetSyntaxKind(node);

        return syntaxKind switch
        {
            var s when s == EnumProvider.SyntaxKind.CodeunitKeyword => EnumProvider.SymbolKind.Codeunit,
            var s when s == EnumProvider.SyntaxKind.PageKeyword => EnumProvider.SymbolKind.Page,
            var s when s == EnumProvider.SyntaxKind.QueryKeyword => EnumProvider.SymbolKind.Query,
            var s when s == EnumProvider.SyntaxKind.TableKeyword => EnumProvider.SymbolKind.Table,
            var s when s == EnumProvider.SyntaxKind.ReportKeyword => EnumProvider.SymbolKind.Report,
            var s when s == EnumProvider.SyntaxKind.XmlPortKeyword => EnumProvider.SymbolKind.XmlPort,
            _ => EnumProvider.SymbolKind.Undefined,
        };
    }

    private static SyntaxKind? GetSyntaxKind(SyntaxNode parent)
    {
        return parent switch
        {
            PermissionSyntax ps => ps.ObjectType.Kind,
            SubtypedDataTypeSyntax sdts => sdts.TypeName.Kind,
            _ => null
        };
    }
}