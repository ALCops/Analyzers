using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class ClearCodeunitSingleInstance : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    ImmutableArray.Create(
        DiagnosticDescriptors.ClearCodeunitSingleInstance);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeInvocation,
            EnumProvider.OperationKind.InvocationExpression);

    private void AnalyzeInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression operation)
            return;

        if (operation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod)
            return;

        switch (operation.TargetMethod.Name)
        {
            case "Clear":
                if (operation.Arguments.Length > 0)
                    AnalyzeClearInvocation(operation, ctx);
                break;

            case "ClearAll":
                AnalyzeClearAllInvocation(ctx);
                break;
        }
    }

    private static void AnalyzeClearInvocation(IInvocationExpression operation, OperationAnalysisContext ctx)
    {
        if (operation.Arguments[0].Value is not IConversionExpression boundConversion ||
            boundConversion.Operand is not IOperation operand ||
            operand.GetSymbol()?.GetTypeSymbol() is not ICodeunitTypeSymbol codeunit)
            return;

        if (IsSingleInstanceCodeunitWithGlobalVars(codeunit))
        {
            var variableName = operand.GetSymbol()?.Name ?? string.Empty;
            var objectName = codeunit.Name;

            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ClearCodeunitSingleInstance,
                ctx.Operation.Syntax.GetLocation(),
                variableName,
                objectName));
        }
    }

    private static void AnalyzeClearAllInvocation(OperationAnalysisContext ctx)
    {
        if (ctx.ContainingSymbol.GetContainingObjectTypeSymbol().GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Codeunit)
            return;

        IEnumerable<ISymbol> localVariables = ((IMethodSymbol)ctx.ContainingSymbol.OriginalDefinition).LocalVariables
                                                        .Where(var => var.OriginalDefinition.GetTypeSymbol().GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Codeunit &&
                                                                      var.OriginalDefinition.GetTypeSymbol().OriginalDefinition != ctx.ContainingSymbol.GetContainingObjectTypeSymbol().OriginalDefinition);

        if (HasSingleInstanceCodeunitWithGlobalVars(localVariables, out ISymbol? localCodeunitVariable) && localCodeunitVariable is not null)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ClearCodeunitSingleInstance,
                ctx.Operation.Syntax.GetLocation(),
                localCodeunitVariable.Name,
                localCodeunitVariable.GetTypeSymbol().Name));

            return;
        }

        IEnumerable<ISymbol> globalVariables = ctx.ContainingSymbol.GetContainingObjectTypeSymbol()
                                                        .GetMembers()
                                                        .Where(var => var.Kind == EnumProvider.SymbolKind.GlobalVariable &&
                                                                        var.OriginalDefinition.GetTypeSymbol().GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Codeunit &&
                                                                        var.OriginalDefinition.GetTypeSymbol().OriginalDefinition != ctx.ContainingSymbol.GetContainingObjectTypeSymbol().OriginalDefinition);

        if (HasSingleInstanceCodeunitWithGlobalVars(globalVariables, out ISymbol? globalCodeunitVariables) && globalCodeunitVariables is not null)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ClearCodeunitSingleInstance,
                ctx.Operation.Syntax.GetLocation(),
                globalCodeunitVariables.Name,
                globalCodeunitVariables.GetTypeSymbol().Name));

            return;
        }
    }

    private static bool HasSingleInstanceCodeunitWithGlobalVars(IEnumerable<ISymbol> variables, out ISymbol? codeunit)
    {
        foreach (ISymbol variable in variables.Where(var => var.OriginalDefinition?.ContainingType?.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Codeunit))
            if (IsSingleInstanceCodeunitWithGlobalVars((ICodeunitTypeSymbol)variable.OriginalDefinition.GetTypeSymbol()))
            {
                codeunit = variable;
                return true;
            }

        codeunit = null;
        return false;
    }

    private static bool IsSingleInstanceCodeunitWithGlobalVars(ICodeunitTypeSymbol codeunitTypeSymbol)
    {
        if (!IsSingleInstanceCodeunit(codeunitTypeSymbol))
        {
            return false;
        }

        var globalVariables = codeunitTypeSymbol.GetMembers().Where(members => members.Kind == EnumProvider.SymbolKind.GlobalVariable);
        var globalVariablesNonRecordTypes = globalVariables.Where(vars => vars.GetTypeSymbol().GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Record);

        bool globalVariablesExists = globalVariablesNonRecordTypes.Count() != 0;
        return globalVariablesExists;
    }

    private static bool IsSingleInstanceCodeunit(ICodeunitTypeSymbol codeunitTypeSymbol)
    {
        IPropertySymbol? singleInstanceProperty = codeunitTypeSymbol.GetProperty(EnumProvider.PropertyKind.SingleInstance);
        if (singleInstanceProperty is null)
        {
            return false;
        }

        // codeunits without source code could return "1" for the SingleInstance value
        if (singleInstanceProperty.Value is not bool booleanValue)
        {
            return false;
        }

        return booleanValue;
    }
}