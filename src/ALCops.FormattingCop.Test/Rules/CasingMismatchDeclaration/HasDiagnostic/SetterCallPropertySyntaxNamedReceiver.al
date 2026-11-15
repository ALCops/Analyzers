codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyOtherCodeunit: Codeunit MyOtherCodeunit;
    begin
        MyOtherCodeunit.[|setvalue|] := 42;
    end;
}

codeunit 50101 MyOtherCodeunit
{
    procedure SetValue(NewValue: Integer)
    begin
    end;
}
