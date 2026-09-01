codeunit 50113 MyControlFlowAfterCodeunit
{
    procedure ControlFlowBeforeDisabled(Flag: Boolean; Other: Boolean)
    begin
        if Flag then begin
            Message('First');
        end;
        [|if|] Other then begin
            Message('Second');
        end;
    end;
}
