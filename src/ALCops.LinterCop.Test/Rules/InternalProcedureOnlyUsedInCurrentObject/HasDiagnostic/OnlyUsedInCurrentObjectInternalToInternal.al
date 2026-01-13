codeunit 50100 MyCodeunit
{
    procedure Caller()
    begin
        MyWrapper();
    end;

    internal procedure MyWrapper()
    begin
        MyProcedure();
    end;

    internal procedure [|MyProcedure|]()
    begin
    end;
}