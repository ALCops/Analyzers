codeunit 50100 MyFirstCodeunit
{
    internal procedure [|MyProcedure|]()
    begin
    end;
}

codeunit 50101 MySecondCodeunit
{
    internal procedure MyProcedure()
    begin
    end;

    procedure Caller()
    begin
        MyProcedure();
    end;
}