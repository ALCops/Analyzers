codeunit 50100 MyTestCodeunit
{
    Subtype = Test;

    [Test]
    procedure [|MyTestProcedure|]()
    begin
    end;

    [MessageHandler]
    procedure [|MyMessageHandler|](Message: Text[1024])
    begin
    end;

    [ConfirmHandler]
    procedure [|MyConfirmHandler|](Question: Text[1024]; var Reply: Boolean)
    begin
    end;
}
