codeunit 50100 MyCodeunit
{
    procedure Caller()
    begin
        MyProcedure();
        MyProcedure();
    end;

    internal procedure [|MyProcedure|]()
    begin
    end;
}