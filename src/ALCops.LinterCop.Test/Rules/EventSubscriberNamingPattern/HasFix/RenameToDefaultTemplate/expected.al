codeunit 50100 MySubscriber
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::"My Publisher", OnSomething, '', false, false)]
    local procedure "My Publisher_OnSomething"()
    begin
    end;
}

codeunit 50101 "My Publisher"
{
    [IntegrationEvent(false, false)]
    procedure OnSomething()
    begin
    end;
}
