// New default template {Event Source}_{EventName}[_{Element Name}] matches the identifier
// the AL Language extension's "Find Event" feature generates verbatim (quoted when needed).
codeunit 50100 MySubscriber
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::"My Publisher", OnSomething, '', false, false)]
    local procedure [|"My Publisher_OnSomething"|]()
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
