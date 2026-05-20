using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Text;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class ObjectIdInDeclaration : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.ObjectIdInDeclaration);

    private static readonly Lazy<ImmutableDictionary<string, string>> SymbolKindDictionary = new(GenerateSymbolKindDictionary);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(
            this.AnalyzeSyntaxNode,
            EnumProvider.SyntaxKind.ObjectReference,
            EnumProvider.SyntaxKind.PermissionValue);

        context.RegisterOperationAction(
            this.AnalyzeBuiltInInvocation,
            EnumProvider.OperationKind.InvocationExpression);

        context.RegisterSymbolAction(
            this.AnalyzeEventSubscriber,
            EnumProvider.SymbolKind.Method);
    }

    private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        if (ctx.Node is not ObjectNameOrIdSyntax { Identifier: ObjectIdSyntax identifier })
            return;

        if (identifier.Value.Kind != EnumProvider.SyntaxKind.Int32LiteralToken)
            return;

        if (identifier.Value.Value is not int objectId)
            return;

        SymbolKind symbolKind = GetSymbolKind(ctx.Node.Parent);
        if (symbolKind == EnumProvider.SymbolKind.Undefined)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ObjectIdInDeclarationWithoutCodeFix,
                ctx.Node.GetLocation(),
                objectId));
            return;
        }

        var referencedApplicationObject = ctx.SemanticModel.Compilation.GetApplicationObjectTypeSymbolsByIdAcrossModulesWithReflection(symbolKind, objectId).FirstOrDefault();
        if (referencedApplicationObject == null)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ObjectIdInDeclarationWithoutCodeFix,
                ctx.Node.GetLocation(),
                objectId));
            return;
        }

        ctx.ReportDiagnostic(
            CreateDiagnostic(
                ctx.ContainingSymbol,
                referencedApplicationObject,
                objectId,
                ctx.Node.GetLocation()));
    }

    private void AnalyzeBuiltInInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
            return;

        var targetMethod = invocation.TargetMethod;
        if (targetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
            return;

        // Exclude methods that don't accept any parameters
        if (targetMethod.Parameters.Length == 0)
            return;

        // Skip if the method does not have a first parameter set
        if (invocation.Arguments.Length == 0)
            return;

        if (targetMethod.Parameters[0].ParameterType.NavTypeKind != EnumProvider.NavTypeKind.Integer)
            return;

        // Special treatment for RecordRef where we only allow variables of that type
        if (invocation.Instance is IOperation instance &&
            instance.Type.NavTypeKind != EnumProvider.NavTypeKind.RecordRef)
            return;

        if (invocation.Syntax is not InvocationExpressionSyntax invocationSyntax)
            return;

        if (invocationSyntax.ArgumentList.Arguments[0] is not LiteralExpressionSyntax literalExpr)
            return;

        if (literalExpr.Literal is not Int32SignedLiteralValueSyntax intLiteral)
            return;

        if (!int.TryParse(intLiteral.Number.ValueText, out var objectId))
            return;

        if (invocationSyntax.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var symbolKindAsText = memberAccess.Expression.GetIdentifierOrLiteralValue();
        if (symbolKindAsText is null)
            return;

        var symbolKindText = symbolKindAsText.ToString();
        // Special treatment for RecordRef where we only want to analyze Open method calls
        if (SemanticFacts.IsSameName(symbolKindText, "RecordRef") &&
            !SemanticFacts.IsSameName(targetMethod.Name, "Open"))
            return;

        // Map RecordRef to Table Data Type
        if (SemanticFacts.IsSameName(symbolKindText, "RecordRef"))
            symbolKindText = "Table";

        if (!Enum.TryParse(symbolKindText, ignoreCase: true, out SymbolKind symbolKind))
            return;

        // Page.Run(0) is a valid use case
        if (symbolKind == EnumProvider.SymbolKind.Page && objectId == 0)
            return;

        var referencedApplicationObject = ctx.Compilation
            .GetApplicationObjectTypeSymbolsByIdAcrossModulesWithReflection(symbolKind, objectId)
            .FirstOrDefault();

        if (referencedApplicationObject == null)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ObjectIdInDeclarationWithoutCodeFix,
                literalExpr.GetLocation(),
                objectId));
            return;
        }

        ctx.ReportDiagnostic(
            CreateDiagnostic(
                ctx.ContainingSymbol,
                referencedApplicationObject,
                objectId,
                literalExpr.GetLocation(),
                referencedApplicationObject.Kind
                ));
    }

    private void AnalyzeEventSubscriber(SymbolAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not IMethodSymbol method)
            return;

        if (method.Attributes.Length == 0)
            return;

        var eventSubscriberAttribute = method.Attributes.FirstOrDefault(attr => attr.AttributeKind == EnumProvider.AttributeKind.EventSubscriber);
        if (eventSubscriberAttribute == null)
            return;

        if (eventSubscriberAttribute.Arguments[1].DeclaringSyntaxReference?.GetSyntax().Kind != EnumProvider.SyntaxKind.LiteralAttributeArgument ||
            !int.TryParse(
                eventSubscriberAttribute.Arguments[1].ValueText,
                out int objectId))
        {
            return;
        }

        var referencedApplicationObject = eventSubscriberAttribute.GetReferencedApplicationObject();
        if (referencedApplicationObject == null)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ObjectIdInDeclarationWithoutCodeFix,
                eventSubscriberAttribute.Arguments[1].GetLocation(),
                objectId));
            return;
        }

        ctx.ReportDiagnostic(
            CreateDiagnostic(
                ctx.Symbol,
                referencedApplicationObject,
                objectId,
                eventSubscriberAttribute.Arguments[1].GetLocation(),
                referencedApplicationObject.Kind
                ));
    }

    private static Diagnostic CreateDiagnostic(ISymbol ContainingSymbol, IApplicationObjectTypeSymbol ReferencedApplicationObject, int ObjectId, Location Location) =>
        CreateDiagnostic(ContainingSymbol, ReferencedApplicationObject, ObjectId, Location, null);

    private static Diagnostic CreateDiagnostic(ISymbol ContainingSymbol, IApplicationObjectTypeSymbol ReferencedApplicationObject, int ObjectId, Location Location, SymbolKind? symbolKind)
    {
        string? namespaceName = null;
        var containingNamespace = ContainingSymbol.GetContainingNamespaceQualifiedNameWithReflection();
        var RefAppNamespace = ReferencedApplicationObject.GetContainingNamespaceQualifiedNameWithReflection();
        if (containingNamespace != RefAppNamespace)
        {
            namespaceName = RefAppNamespace;
        }

        var symbolKindText = symbolKind.ToString() ?? string.Empty;
        if (!string.IsNullOrEmpty(symbolKindText) && SymbolKindDictionary.Value.TryGetValue(symbolKindText, out var mapped))
            symbolKindText = mapped;

        var properties = ImmutableDictionary<string, string>.Empty
            .Add("SymbolKind", symbolKindText)
            .Add("NamespaceName", namespaceName ?? string.Empty)
            .Add("IdentifierName", ReferencedApplicationObject.Name);

        return Diagnostic.Create(
            DiagnosticDescriptors.ObjectIdInDeclaration,
            Location,
            properties,
            ObjectId,
            ReferencedApplicationObject.Name);
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
            ObjectReferencePropertyValueSyntax orpvs => GetSyntaxKindFromObjectReferencePropertyValue(orpvs),
            PagePartSyntax pps => EnumProvider.SyntaxKind.PageKeyword,
            PermissionSyntax ps => ps.ObjectType.Kind,
            ReportDataItemSyntax => EnumProvider.SyntaxKind.TableKeyword,
            SubtypedDataTypeSyntax sdts => GetSyntaxKindFromSubtypedDataType(sdts),
            _ => null
        };
    }

    private static SyntaxKind? GetSyntaxKindFromObjectReferencePropertyValue(ObjectReferencePropertyValueSyntax orpvs)
    {
        return orpvs.GetContainingObjectSyntax() switch
        {
            ProfileSyntax => EnumProvider.SyntaxKind.PageKeyword,
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

    private static ImmutableDictionary<string, string> GenerateSymbolKindDictionary()
    {
        var builder = ImmutableDictionary.CreateBuilder<string, string>(SemanticFacts.NameEqualityComparer);

        foreach (var kind in Enum.GetNames(typeof(SymbolKind)))
        {
            // Map enum names to expected string value
            // - "Table" => "Database"
            // - "XmlPort" => "Xmlport"
            var value = kind switch
            {
                "Table" => "Database",
                "XmlPort" => "Xmlport",
                _ => kind
            };

            builder[kind] = value;
        }

        // Add additional entry for handling RecordRef.methodName (map to Database)
        builder["RecordRef"] = "Database";

        return builder.ToImmutable();
    }
}