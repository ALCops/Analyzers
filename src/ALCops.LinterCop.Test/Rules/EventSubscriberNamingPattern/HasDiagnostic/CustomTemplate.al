// Correct per default template, but violates custom template Handle{EventSource}{EventName}.
// Expected with custom template: HandleMyPublisherOnSomething
codeunit 50100 MySubscriber
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::"My Publisher", OnSomething, '', false, false)]
    local procedure [|OnMyPublisherOnSomething|]()
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
