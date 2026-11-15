codeunit 50100 MyCodeunit
{
    var
        MyIntegerVariable: Integer;

    internal procedure [|MyInteger|](NewIntegerValue: Integer)
    begin
        MyIntegerVariable := NewIntegerValue;
    end;
}

codeunit 50101 MyOtherCodeunit
{
    var
        MyCodeunit: Codeunit MyCodeunit;

    procedure Caller()
    begin
        MyCodeunit.MyInteger := 42;
    end;
}
