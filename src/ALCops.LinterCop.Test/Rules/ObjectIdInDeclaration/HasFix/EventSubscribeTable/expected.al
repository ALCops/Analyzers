codeunit 50101 "My Codeunit"
{
    [EventSubscriber(ObjectType::Table, Database::MyTable, MyIntegrationEvent, '', false, false)]
    local procedure MyEventSubscriberOnMyTable()
    begin
    end;
}

table 50100 MyTable
{
    fields { field(1; MyField; Integer) { } }

    [IntegrationEvent(false, false)]
    local procedure MyIntegrationEvent()
    begin
    end;
}