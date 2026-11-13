codeunit 50100 MyCodeunit
{
    Subtype = Test;

    [MessageHandler]
    internal procedure [|MyProcedure|](Message: Text[1024])
    begin
    end;
}