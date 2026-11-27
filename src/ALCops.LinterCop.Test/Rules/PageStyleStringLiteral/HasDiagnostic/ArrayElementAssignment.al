codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyStyles: array[2] of Text;
    begin
        MyStyles[1] := [|'Strong'|];
    end;
}
