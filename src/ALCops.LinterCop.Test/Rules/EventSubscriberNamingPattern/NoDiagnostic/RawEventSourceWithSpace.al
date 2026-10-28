// Raw source name preserves its spaces; the source type is included before it.
codeunit 50100 MySubscriber
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::"My Cool Publisher", OnSomething, '', false, false)]
    local procedure [|"OnCodeunit_My Cool Publisher_OnSomething"|]()
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
