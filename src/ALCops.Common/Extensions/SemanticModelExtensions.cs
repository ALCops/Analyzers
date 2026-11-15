using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.Common.Extensions;

public static class SemanticModelExtensions
{
    /// <summary>
    /// Returns the procedure that a property-style setter call invokes when <paramref name="target"/> is the
    /// target of that call, otherwise <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// A procedure with exactly one parameter can be called with assignment syntax: <c>Proc := Value</c>,
    /// <c>Obj.Proc := Value</c> or <c>this.Proc := Value</c>. The binder rebinds the whole assignment statement
    /// as the invocation, so <see cref="SemanticModel.GetSymbolInfo(SyntaxNode, CancellationToken)"/> on the
    /// procedure name or its member access has no <see cref="SymbolInfo.Symbol"/> (the procedure is only an
    /// overload-resolution-failure candidate), and the call is reachable only as the
    /// <see cref="IInvocationExpression"/> that <see cref="SemanticModel.GetOperation(SyntaxNode, CancellationToken)"/>
    /// returns for the assignment statement. Call this after <c>GetSymbolInfo</c> came back empty: it binds the
    /// enclosing statement only when <paramref name="target"/> is an assignment target.
    /// </remarks>
    /// <param name="semanticModel">The semantic model of the tree that contains <paramref name="target"/>.</param>
    /// <param name="target">
    /// The assignment target: a bare <see cref="IdentifierNameSyntax"/>, a <see cref="MemberAccessExpressionSyntax"/>,
    /// or the identifier that is the <see cref="MemberAccessExpressionSyntax.Name"/> of one.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static IMethodSymbol? GetMethodInvokedByAssignmentSyntax(
        this SemanticModel semanticModel, SyntaxNode target, CancellationToken cancellationToken = default)
    {
        if (target is IdentifierNameSyntax && target.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == target)
            target = memberAccess;

        if (target.Parent is not AssignmentStatementSyntax assignment || assignment.Target != target)
            return null;

        return (semanticModel.GetOperation(assignment, cancellationToken) as IInvocationExpression)?.TargetMethod;
    }
}
