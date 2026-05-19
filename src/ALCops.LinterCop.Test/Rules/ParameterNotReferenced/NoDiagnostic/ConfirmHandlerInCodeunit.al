codeunit 50100 MyCodeunit
{
    Subtype = Test;

    [ConfirmHandler]
    procedure ConfirmHandler([|Question: Text[1024]|]; var Reply: Boolean)
    begin
        Reply := true;
    end;
}
