codeunit 50100 MyCodeunit
{
    Subtype = Test;

    [Test]
    [HandlerFunctions('ConfirmHandler')]
    procedure MyProcedure()
    begin
    end;

    [ConfirmHandler]
    procedure [|ConfirmHandler(Question: Text[1024]; var Response: Boolean)|]
    begin
    end;
}