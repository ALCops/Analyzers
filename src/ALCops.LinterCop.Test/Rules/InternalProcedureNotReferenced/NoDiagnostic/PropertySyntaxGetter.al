codeunit 50100 MyCodeunitWithProperty
{
    var
        MyIntegerVariable: Integer;

    internal procedure [|GetValue|](): Integer
    begin
        exit(MyIntegerVariable);
    end;
}

codeunit 50101 SampleCodeunit
{
    var
        MyCodeunitWithProperty: Codeunit MyCodeunitWithProperty;

    trigger OnRun()
    var
        Value: Integer;
    begin
        Value := MyCodeunitWithProperty.GetValue;
    end;
}
