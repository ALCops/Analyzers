codeunit 50127 MyCFBeforeOnlyCodeunit
{
    // With ControlFlowBefore=true and ControlFlowAfter=false, only the "before" marker on 'if' fires.
    // The Message() call after the if block is intentionally not marked because "after" is off.
    procedure BeforeOnly(Flag: Boolean)
    begin
        Message('Prep');
        [|if|] Flag then begin
            Message('Inside if');
        end;
        Message('After if without blank line — not flagged because ControlFlowAfter=false');
    end;
}
