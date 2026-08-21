codeunit 50100 MessageHandlerSignature
{
    Subtype = Test;

    [MessageHandler]
    procedure MessageHandler([|MessageText: Text[1024]|])
    begin
    end;
}
