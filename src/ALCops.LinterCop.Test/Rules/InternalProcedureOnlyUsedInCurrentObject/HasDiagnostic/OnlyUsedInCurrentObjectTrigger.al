codeunit 50100 MyCodeunit
{
    trigger OnRun()
    begin
        MyProcedure();
    end;

    internal procedure [|MyProcedure|]()
    begin
    end;
}
