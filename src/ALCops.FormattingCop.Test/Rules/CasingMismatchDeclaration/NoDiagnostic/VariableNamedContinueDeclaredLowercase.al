codeunit 50101 MyCodeunit
{
    procedure MyProcedure()
    var
        continue: Boolean;
    begin
        [|continue|] := true;
        if [|continue|] then;
    end;
}
