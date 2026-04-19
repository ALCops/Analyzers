codeunit 50100 MyCodeunit
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::"My Publisher", OnSomething, '', false, false)]
    local [|procedure OnSomething()|]
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
