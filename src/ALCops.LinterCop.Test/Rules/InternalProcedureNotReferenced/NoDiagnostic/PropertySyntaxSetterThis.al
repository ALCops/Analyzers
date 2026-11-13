codeunit 50100 MyCodeunitWithProperty
{
    var
        MyIntegerVariable: Integer;

    trigger OnRun()
    begin
        this.MyInteger := 42;
    end;

    internal procedure [|MyInteger|](NewIntegerValue: Integer)
    begin
        MyIntegerVariable := NewIntegerValue;
    end;
}
