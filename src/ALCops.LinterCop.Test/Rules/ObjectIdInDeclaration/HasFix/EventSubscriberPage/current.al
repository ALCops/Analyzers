codeunit 50101 "My Codeunit"
{
    [EventSubscriber(ObjectType::Page, [|50100|], MyIntegrationEvent, '', false, false)]
    local procedure MyEventSubscriberOnMyPage()
    begin
    end;
}

page 50100 MyPage
{
    [IntegrationEvent(false, false)]
    local procedure MyIntegrationEvent()
    begin
    end;
}