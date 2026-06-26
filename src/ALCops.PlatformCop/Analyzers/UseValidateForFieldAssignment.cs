using System;
using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.PlatformCop.Analyzers;

[DiagnosticAnalyzer]
public class UseValidateForFieldAssignment : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.UseValidateForFieldAssignment
        );

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterOperationAction(
            AnalyzeAssignmentOperation,
            EnumProvider.OperationKind.AssignmentStatement
        );

        if (EnumProvider.OperationKind.CompoundAssignmentStatement != default)
        {
            context.RegisterOperationAction(
                AnalyzeAssignmentOperation,
                EnumProvider.OperationKind.CompoundAssignmentStatement
            );
        }
    }

    private static void AnalyzeAssignmentOperation(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        if (ctx.Operation is not IAssignmentStatement assignment)
            return;

        if (assignment.Target is not IFieldAccess fieldAccess)
            return;

        if (fieldAccess.Instance?.Type is not IRecordTypeSymbol recordType)
            return;

        if (recordType.Temporary)
            return;

        if (IsAssignmentToOwnValidateField(ctx, fieldAccess))
            return;

        var location = fieldAccess.Syntax?.GetIdentifierNameSyntax()?.GetLocation()
                       ?? fieldAccess.Syntax?.GetLocation();

        if (location is null)
            return;

        ctx.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.UseValidateForFieldAssignment,
                location,
                fieldAccess.FieldSymbol.Name));
    }

    private static readonly string[] ValidateTriggerNames =
    {
        "OnValidate",
        "OnBeforeValidate",
        "OnAfterValidate"
    };

    // Assigning a field to the current record (Rec/self) inside that same field's own
    // OnValidate / OnBeforeValidate / OnAfterValidate trigger is by design (default
    // values, value transformation such as rounding) and cannot use Validate().
    private static bool IsAssignmentToOwnValidateField(OperationAnalysisContext ctx, IFieldAccess fieldAccess)
    {
        var syntax = fieldAccess.Syntax;
        if (syntax is null)
            return false;

        var trigger = syntax.FirstAncestorOrSelf<TriggerDeclarationSyntax>();
        if (trigger is null)
            return false;

        if (!IsValidateTrigger(trigger))
            return false;

        if (!IsCurrentRecordInstance(fieldAccess.Instance))
            return false;

        var ownerField = ResolveTriggerOwnerField(ctx);
        if (ownerField is null)
            return false;

        return fieldAccess.FieldSymbol.Name.IsSameName(ownerField.Name);
    }

    private static bool IsValidateTrigger(TriggerDeclarationSyntax trigger)
    {
        var name = trigger.Name?.Identifier.ValueText;
        if (name is null)
            return false;

        foreach (var validateName in ValidateTriggerNames)
        {
            if (string.Equals(name, validateName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    // The current record is referenced either as 'this'/self (an instance reference) or
    // through the synthesized 'Rec' global variable (including a page's implicit-with).
    // 'xRec' and any user-declared record variable are different records and must keep firing.
    private static bool IsCurrentRecordInstance(IOperation? instance)
    {
        if (instance is null)
            return false;

#if !NETSTANDARD2_1
        if (instance is IInstanceReferenceOperation)
            return true;
#endif

        var symbol = instance.GetSymbolSafe();
        return symbol is not null
               && string.Equals(symbol.Name, "Rec", StringComparison.OrdinalIgnoreCase);
    }

    // The trigger symbol's containing symbol is the field (table/table extension) or the
    // control (page/page extension) that owns the trigger. For a control, the owner field
    // is the table field bound to its source expression. For OnBeforeValidate/OnAfterValidate
    // the owner is a change-modify symbol whose modified base field/control is exposed via
    // an internal 'Target' property.
    private static IFieldSymbol? ResolveTriggerOwnerField(OperationAnalysisContext ctx)
        => ResolveOwnerField(ctx.ContainingSymbol?.ContainingSymbol);

    // Name of the internal 'SourceChangeModifySymbol.Target' property. It is not exposed on
    // the public IChangeSymbol interface (which only surfaces ChangeKind and Type), so the
    // modified base field/control can only be reached via reflection.
    private const string ChangeModifyTargetPropertyName = "Target";

    private static IFieldSymbol? ResolveOwnerField(ISymbol? owner)
    {
        switch (owner)
        {
            case null:
                return null;
            case IFieldSymbol field:
                return field;
            case IControlSymbol control:
                return control.RelatedFieldSymbol;
        }

        // modify(field)/modify(control) in extensions: the owner is a change-modify symbol
        // (SymbolKind.Change) whose modified base symbol is exposed via the internal 'Target'.
        if (owner.Kind != EnumProvider.SymbolKind.Change)
            return null;

        var target = owner.GetPropertyIfExists<ISymbol>(ChangeModifyTargetPropertyName);
        if (target is not null && !ReferenceEquals(target, owner))
            return ResolveOwnerField(target);

        return null;
    }
}
