codeunit 50101 "My Codeunit"
{
    [EventSubscriber(ObjectType::XmlPort, Xmlport::MyXmlport, MyIntegrationEvent, '', false, false)]
    local procedure MyEventSubscriberOnMyXmlPort()
    begin
    end;
}

xmlport 50100 MyXmlport
{
    [IntegrationEvent(false, false)]
    local procedure MyIntegrationEvent()
    begin
    end;
}