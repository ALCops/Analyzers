using System.Collections.Immutable;
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
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ObjectIdInDeclarationWithoutCodeFix,
                ctx.Node.GetLocation(),
                id));
            return;
        }

        var applicationObjectTypeSymbol = ctx.SemanticModel.Compilation.GetApplicationObjectTypeSymbolsByIdAcrossModulesWithReflection(symbolKind, id).FirstOrDefault();
        if (applicationObjectTypeSymbol == null)
            return;

        string? namespaceName = null;
        var containingNamespace = ctx.ContainingSymbol.GetContainingNamespaceQualifiedNameWithReflection();
        var appObjectNamespace = applicationObjectTypeSymbol.GetContainingNamespaceQualifiedNameWithReflection();
        if (containingNamespace != appObjectNamespace)
        {
            namespaceName = appObjectNamespace;
        }

        var properties = ImmutableDictionary<string, string>.Empty
            .Add("NamespaceName", namespaceName ?? string.Empty)
            .Add("IdentifierName", applicationObjectTypeSymbol.Name);

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
            PagePartSyntax pps => EnumProvider.SyntaxKind.PageKeyword,
            PermissionSyntax ps => ps.ObjectType.Kind,
            ReportDataItemSyntax => EnumProvider.SyntaxKind.TableKeyword,
            SubtypedDataTypeSyntax sdts => GetSyntaxKindFromSubtypedDataType(sdts),
            _ => null
        };
    }

    private static SyntaxKind? GetSyntaxKindFromSubtypedDataType(SubtypedDataTypeSyntax sdts)
    {
        var kind = sdts.TypeName.Kind;

        if (kind == EnumProvider.SyntaxKind.IdentifierToken)
            return GetSyntaxKindFromIdentifier(sdts.TypeName.Value as string);

        return kind;
    }

    private static SyntaxKind? GetSyntaxKindFromIdentifier(string? identifier)
    {
        return identifier switch
        {
            "Record" => EnumProvider.SyntaxKind.TableKeyword,
            _ => null
        };
    }

    private static string GetQualifiedIdentifierName(ISymbol containingSymbol, IApplicationObjectTypeSymbol applicationObjectTypeSymbol)
    {
        var containingNamespaceQualifiedName = containingSymbol.GetContainingNamespaceQualifiedNameWithReflection();
        var applicationObjectNamespaceQualifiedName = applicationObjectTypeSymbol.GetContainingNamespaceQualifiedNameWithReflection();
        if (
            (string.IsNullOrEmpty(containingNamespaceQualifiedName) &&
            string.IsNullOrEmpty(applicationObjectNamespaceQualifiedName)) ||
            containingNamespaceQualifiedName == applicationObjectNamespaceQualifiedName)
        {
            return applicationObjectTypeSymbol.Name;
        }

        return applicationObjectNamespaceQualifiedName + "." + applicationObjectTypeSymbol.Name;
    }
}