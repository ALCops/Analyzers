codeunit 50100 MyPublisher
{
    [IntegrationEvent(false, false)]
    procedure Strong()
    begin
    end;
}

codeunit 50101 MySubscriber
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::MyPublisher, [|'Strong'|], '', false, false)]
    local procedure OnStrong()
    begin
    end;
}
