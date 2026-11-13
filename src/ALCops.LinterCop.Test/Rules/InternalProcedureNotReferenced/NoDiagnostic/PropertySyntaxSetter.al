codeunit 50100 MyCodeunitWithProperty
{
    var
        MyIntegerVariable: Integer;

    internal procedure [|MyInteger|](NewIntegerValue: Integer)
    begin
        MyIntegerVariable := NewIntegerValue;
    end;
}

codeunit 50101 SampleCodeunit
{
    var
        MyCodeunitWithProperty: Codeunit MyCodeunitWithProperty;

    trigger OnRun()
    begin
        MyCodeunitWithProperty.MyInteger := 42;
    end;
}
