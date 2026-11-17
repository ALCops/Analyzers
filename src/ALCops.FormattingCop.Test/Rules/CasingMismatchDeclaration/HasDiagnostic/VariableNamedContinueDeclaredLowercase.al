codeunit 50101 MyCodeunit
{
    procedure MyProcedure()
    var
        continue: Boolean;
    begin
        [|Continue|] := true;
        if [|CONTINUE|] then;
    end;
}
