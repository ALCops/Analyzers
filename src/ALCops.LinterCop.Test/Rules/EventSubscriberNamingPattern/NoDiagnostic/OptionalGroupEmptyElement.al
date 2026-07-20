// Template On{EventSource}_{EventName}[_{ElementName}] with empty element name.
// Optional group [_{ElementName}] is skipped -> OnMyPublisher_OnSomething
codeunit 50100 MySubscriber
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::"My Publisher", OnSomething, '', false, false)]
    local procedure [|OnMyPublisher_OnSomething|]()
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
