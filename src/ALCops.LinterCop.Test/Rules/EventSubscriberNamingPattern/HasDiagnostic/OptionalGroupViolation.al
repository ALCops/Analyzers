// Template On{EventSource}_{EventName}[_{ElementName}] with empty element name.
// Expected: OnMyPublisher_OnSomething
// Actual:   OnMyPublisher_OnSomething_ (orphaned separator)
codeunit 50100 MySubscriber
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::"My Publisher", OnSomething, '', false, false)]
    local procedure [|OnMyPublisher_OnSomething_|]()
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
