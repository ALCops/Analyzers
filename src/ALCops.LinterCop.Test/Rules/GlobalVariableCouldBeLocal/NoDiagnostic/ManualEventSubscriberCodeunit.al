codeunit 50100 ManualEventSubscriberCodeunit
{
    EventSubscriberInstance = Manual;

    var
        [|MyValue|]: Integer;

    local procedure ShowValue()
    begin
        MyValue := 10;
        Message('%1', MyValue);
    end;
}
