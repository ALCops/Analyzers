codeunit 50100 IntegrationEventExposesGlobals
{
    var
        [|MyPublishedValue|]: Integer;

    local procedure DoWork()
    begin
        MyPublishedValue := 1;
        Message('%1', MyPublishedValue);
        OnAfterDoWork();
    end;

    [IntegrationEvent(false, true)]
    local procedure OnAfterDoWork()
    begin
    end;
}
