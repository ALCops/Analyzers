using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class RecordGetProcedureArguments : DiagnosticAnalyzer
{
    private const string GetMethodName = "Get";
    private const string PrimaryKeyFieldName = "Primary Key";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.RecordGetProcedureArguments
        );

    private static readonly Dictionary<NavTypeKind, HashSet<NavTypeKind>> ImplicitConversions = new()
    {
        // Integer can be converted to Decimal, Option and/or BigInteger
        { EnumProvider.NavTypeKind.Integer, new HashSet<NavTypeKind> { EnumProvider.NavTypeKind.Decimal, EnumProvider.NavTypeKind.Option, EnumProvider.NavTypeKind.BigInteger } },

        // BigInteger can be converted to Duration
        { EnumProvider.NavTypeKind.BigInteger, new HashSet<NavTypeKind> { EnumProvider.NavTypeKind.Duration } },

        // Code can be converted to Text
        { EnumProvider.NavTypeKind.Code, new HashSet<NavTypeKind> { EnumProvider.NavTypeKind.Text } },

        // Text can be converted to Code
        { EnumProvider.NavTypeKind.Text, new HashSet<NavTypeKind> { EnumProvider.NavTypeKind.Code } },

        // String(literal) can be converted to Text and/or Code
        { NavTypeKind.String, new HashSet<NavTypeKind> { NavTypeKind.Text, NavTypeKind.Code } },

        // Explicity set Enum can not be converted
        { EnumProvider.NavTypeKind.Enum, new HashSet<NavTypeKind>() }
    };

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeInvocationStatement,
            EnumProvider.OperationKind.InvocationExpression
        );

    private void AnalyzeInvocationStatement(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete() || ctx.Operation is not IInvocationExpression invocation)
            return;

        if (invocation.TargetMethod.MethodKind != EnumProvider.MethodKind.BuiltInMethod ||
            !string.Equals(invocation.TargetMethod.Name, GetMethodName, StringComparison.Ordinal))
            return;

        // Skip unsupported single argument scenarios, like Record.Get(RecordId)
        if (invocation.Arguments.Length == 1 &&
            invocation.Arguments[0].Value is IConversionExpression { Operand.Type: { } type } &&
            type.GetTypeSymbol().GetNavTypeKindSafe() == EnumProvider.NavTypeKind.RecordId)
        {
            return;
        }

        if (invocation.Instance?.Type.GetTypeSymbol()?.OriginalDefinition is not ITableTypeSymbol table)
            return;

        if (IsSingletonTable(table))
        {
            if (invocation.Arguments.Length == 0)
                return;

            if (invocation.Arguments.Length == 1 &&
                 invocation.Arguments[0].Value is IConversionExpression { Operand.ConstantValue: { HasValue: true } constant } &&
                constant.Value?.ToString() == "")
            {
                return;
            }
        }

        if (invocation.Arguments.Length != table.PrimaryKey.Fields.Length)
        {
            string expectedArgs = invocation.Arguments.Length < table.PrimaryKey.Fields.Length
                ? $"Insufficient arguments provided; expected {table.PrimaryKey.Fields.Length} arguments"
                : $"Too many arguments provided; expected {table.PrimaryKey.Fields.Length} arguments";

            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.RecordGetProcedureArguments,
                ctx.Operation.Syntax.GetLocation(),
                table.Name,
                expectedArgs));

            return;
        }

        for (int i = 0; i < invocation.Arguments.Length; i++)
        {
            if (!AreFieldCompatible(invocation.Arguments[i], table.PrimaryKey.Fields[i]))
            {
                if (IsOptionMemberAccessOnMatchingPrimaryKeyField(invocation.Arguments[i], table.PrimaryKey.Fields[i], table))
                    continue;

                var argumentType = invocation.Arguments[i].GetTypeSymbol();
#if NETSTANDARD2_1
                var fieldType = table.PrimaryKey.Fields[i].OriginalDefinition.GetTypeSymbol();
#else
                var fieldType = table.PrimaryKey.Fields[i].Type;
#endif

                string expectedArgs = $"Argument at position {i + 1} has an invalid type; expected '{fieldType}', found '{argumentType}'";

                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.RecordGetProcedureArguments,
                    ctx.Operation.Syntax.GetLocation(),
                    table.Name,
                    expectedArgs));
                return;
            }
        }
    }

    private static bool AreFieldCompatible(IArgument argument, IFieldSymbol field)
    {
        var argumentType = argument.GetTypeSymbol();
#if NETSTANDARD2_1
        var fieldType = field.OriginalDefinition.GetTypeSymbol();
#else
        var fieldType = field.Type;
#endif
        if (argumentType is null || fieldType is null)
            return true;

        var argumentNavType = argumentType.GetNavTypeKindSafe();
        var fieldNavType = fieldType.GetNavTypeKindSafe();

        if (argumentNavType == NavTypeKind.Enum && fieldNavType == NavTypeKind.Enum)
            return argumentType.OriginalDefinition.Equals(fieldType.OriginalDefinition);

        if ((argumentNavType == fieldNavType && argumentType.Length <= fieldType.Length) ||
           argumentNavType == NavTypeKind.None ||
           argumentNavType == NavTypeKind.Joker)
            return true;

        if (ImplicitConversions.TryGetValue(argumentNavType, out var compatibleTypes) && !compatibleTypes.Contains(fieldNavType))
            return false;

        if (argumentType.HasLength && fieldType.HasLength &&
            argumentType.Length > fieldType.Length)
            return false;

        return true;
    }

    private static bool IsOptionMemberAccessOnMatchingPrimaryKeyField(IArgument argument, IFieldSymbol primaryKeyField, ITableTypeSymbol table)
    {
        IOperation current = argument.Value;
        while (current is IConversionExpression conversion)
            current = conversion.Operand;

        if (current is not IOptionAccess optionAccess)
            return false;

        if (optionAccess.Instance is not IFieldAccess fieldAccess)
            return false;

        // Verify the field access is on the same table as the .Get() invocation
        if (fieldAccess.FieldSymbol.GetContainingObjectTypeSymbol() is not ITableTypeSymbol fieldTable ||
            !fieldTable.Equals(table))
            return false;

        return SemanticFacts.IsSameName(fieldAccess.FieldSymbol.Name, primaryKeyField.Name);
    }

    private static bool IsSingletonTable(ITableTypeSymbol table)
    {
        if (table.PrimaryKey.Fields.Length != 1)
            return false;

        var pkField = table.PrimaryKey.Fields[0];

#if NETSTANDARD2_1
        var fieldType = pkField.OriginalDefinition.GetTypeSymbol();
#else
        var fieldType = pkField.Type;
#endif

        if (fieldType is null)
            return false;

        return fieldType.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Code
            || SemanticFacts.IsSameName(pkField.Name, PrimaryKeyFieldName);
    }
}