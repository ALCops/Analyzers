codeunit 50100 MyCodeunit
{
    procedure Caller()
    begin
        MyProcedure();
    end;

    internal procedure [|MyProcedure|]()
    begin
    end;
}

codeunit 50101 MyOtherCodeunit
{
    var
        MyCodeunit: Codeunit MyCodeunit;

    procedure Caller()
    begin
        MyCodeunit.MyProcedure();
    end;
}