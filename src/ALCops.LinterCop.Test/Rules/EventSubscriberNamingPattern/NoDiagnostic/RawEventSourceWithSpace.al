// Default template with an event source whose object name contains a space:
// "My Cool Publisher" + _ + OnSomething -> quoted identifier "My Cool Publisher_OnSomething".
codeunit 50100 MySubscriber
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::"My Cool Publisher", OnSomething, '', false, false)]
    local procedure [|"My Cool Publisher_OnSomething"|]()
    begin
    end;
}

codeunit 50101 "My Cool Publisher"
{
    [IntegrationEvent(false, false)]
    procedure OnSomething()
    begin
    end;
}
