codeunit 50100 MyCodeunit
{
    var
        MyIntegerVariable: Integer;

    internal procedure [|MyInteger|](NewIntegerValue: Integer)
    begin
        MyIntegerVariable := NewIntegerValue;
    end;

    procedure Caller()
    var
        MyInteger: Integer;
    begin
        MyInteger := 42;
    end;
}
