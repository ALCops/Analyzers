codeunit 50112 MyCFInteractionCodeunit
{
    procedure ControlFlowFollowedByExit(Flag: Boolean)
    begin
        if Flag then begin
            Message('Inside');
        end;
        [|exit|];
    end;

    procedure ControlFlowFollowedByError(Flag: Boolean)
    begin
        if Flag then begin
            Message('Inside');
        end;
        [|Error|]('Failed');
    end;

    procedure ControlFlowFollowedByOneLineIf(Flag: Boolean; Other: Boolean)
    begin
        if Flag then begin
            Message('First');
        end;
        [|if|] Other then Message('Second');
    end;

    procedure ControlFlowFollowedByMultiLineIf(Flag: Boolean; Other: Boolean)
    begin
        if Flag then begin
            Message('First');
        end;
        [|if|] Other then
            Message('Second');
    end;
}
