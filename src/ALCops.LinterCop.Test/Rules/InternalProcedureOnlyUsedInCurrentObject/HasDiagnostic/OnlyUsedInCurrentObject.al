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
