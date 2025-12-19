codeunit 50101 "My Codeunit"
{
    [EventSubscriber(ObjectType::Report, [|50100|], MyIntegrationEvent, '', false, false)]
    local procedure MyEventSubscriberOnMyReport()
    begin
    end;
}

report 50100 MyReport
{
    [IntegrationEvent(false, false)]
    local procedure MyIntegrationEvent()
    begin
    end;
}