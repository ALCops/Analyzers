codeunit 50101 "My Codeunit"
{
    [EventSubscriber(ObjectType::Codeunit, [|50100|], MyIntegrationEvent, '', false, false)]
    local procedure MyEventSubscriberOnMyCodeunit()
    begin
    end;
}

codeunit 50100 MyCodeunit
{
    [IntegrationEvent(false, false)]
    local procedure MyIntegrationEvent()
    begin
    end;
}