codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyOtherCodeunit: Codeunit MyOtherCodeunit;
    begin
        [|SetValue|] := 42;
        MyOtherCodeunit.[|SetValue|] := 42;
    end;

    procedure SetValue(NewValue: Integer)
    begin
    end;
}

codeunit 50101 MyOtherCodeunit
{
    procedure SetValue(NewValue: Integer)
    begin
    end;
}
