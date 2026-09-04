codeunit 50100 IntegrationEventIncludesSender
{
    var
        [|MyValue|]: Integer;

    local procedure DoWork()
    begin
        MyValue := 42;
        Message('%1', MyValue);
    end;

    [IntegrationEvent(true, false)]
    local procedure OnSomethingHappened()
    begin
    end;
}
