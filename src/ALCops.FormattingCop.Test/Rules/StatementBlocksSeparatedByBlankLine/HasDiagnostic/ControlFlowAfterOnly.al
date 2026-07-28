codeunit 50128 MyCFAfterOnlyCodeunit
{
    // With ControlFlowBefore=false and ControlFlowAfter=true, only the "after" marker (on the
    // Message following the block) fires. The 'if' keyword is intentionally not marked because
    // "before" is off.
    procedure AfterOnly(Flag: Boolean)
    begin
        Message('Prep');
        if Flag then begin
            Message('Inside if');
        end;
        [|Message|]('After if without blank line — flagged because ControlFlowAfter=true');
    end;
}
