codeunit 50100 MyCodeunit
{
    var
        Continue: Boolean;

    procedure MyProcedure()
    begin
        [|continue|] := true;
    end;
}
