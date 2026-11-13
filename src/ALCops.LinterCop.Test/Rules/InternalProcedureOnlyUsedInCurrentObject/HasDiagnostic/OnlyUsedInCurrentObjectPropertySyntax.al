codeunit 50100 MyCodeunit
{
    var
        MyIntegerVariable: Integer;

    trigger OnRun()
    begin
        MyInteger := 42;
    end;

    internal procedure [|MyInteger|](NewIntegerValue: Integer)
    begin
        MyIntegerVariable := NewIntegerValue;
    end;
}
