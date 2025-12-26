using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.FormattingCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class CasingMismatchBuiltInMethod : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.CasingMismatch);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterOperationAction(
            this.AnalyzeBuiltInMethod,
                EnumProvider.OperationKind.InvocationExpression,
                EnumProvider.OperationKind.FieldAccess,
                EnumProvider.OperationKind.GlobalReferenceExpression,
                EnumProvider.OperationKind.LocalReferenceExpression,
                EnumProvider.OperationKind.ParameterReferenceExpression,
                EnumProvider.OperationKind.ReturnValueReferenceExpression,
                EnumProvider.OperationKind.XmlPortDataItemAccess);
    }

    private void AnalyzeBuiltInMethod(OperationAnalysisContext ctx)
    {
        var operation = ctx.Operation;
        string targetName = operation.Kind switch
        {
            var k when k == EnumProvider.OperationKind.InvocationExpression && operation is IInvocationExpression invocation
                => invocation.TargetMethod.Name,
            var k when k == EnumProvider.OperationKind.FieldAccess && operation is IFieldAccess fieldAccess
                => fieldAccess.FieldSymbol.Name,
            var k when k == EnumProvider.OperationKind.GlobalReferenceExpression
                => ((IGlobalReferenceExpression)operation).GlobalVariable.Name,
            var k when k == EnumProvider.OperationKind.LocalReferenceExpression
                => ((ILocalReferenceExpression)operation).LocalVariable.Name,
            var k when k == EnumProvider.OperationKind.ParameterReferenceExpression
                => ((IParameterReferenceExpression)operation).Parameter.Name,
            var k when k == EnumProvider.OperationKind.ReturnValueReferenceExpression
                => ((IReturnValueReferenceExpression)operation).ReturnValue.Name,
            var k when k == EnumProvider.OperationKind.XmlPortDataItemAccess
                => ((IXmlPortNodeAccess)operation).XmlPortNodeSymbol.Name,
            _ => string.Empty
        };
        if (string.IsNullOrEmpty(targetName))
            return;

        ReadOnlySpan<char> targetSpan = targetName.AsSpan();
        SyntaxNode opSyntax = operation.Syntax;
        var opSyntaxUnquoted = opSyntax.ToString().UnquoteIdentifier();

        if (OnlyDiffersInCasing(opSyntaxUnquoted.AsSpan(), targetSpan))
        {
            var properties = ImmutableDictionary<string, string>.Empty
                .Add("CanonicalText", targetName);

            ctx.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.CasingMismatch,
                    opSyntax.GetLocation(),
                    properties,
                    targetName,
                    opSyntax.ToString()));
            return;
        }

        foreach (var descendant in opSyntax.DescendantNodes())
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            var descendantUnquoted = descendant.ToString().UnquoteIdentifier();
            if (OnlyDiffersInCasing(descendantUnquoted.AsSpan(), targetSpan))
            {
                var properties = ImmutableDictionary<string, string>.Empty
                    .Add("CanonicalText", targetName);

                ctx.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.CasingMismatch,
                        opSyntax.GetLocation(),
                        properties,
                        targetName,
                        descendantUnquoted));
                return;
            }
        }
    }

    private static bool OnlyDiffersInCasing(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        return left.Equals(right, StringComparison.OrdinalIgnoreCase) &&
               !left.Equals(right, StringComparison.Ordinal);
    }
}