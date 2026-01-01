codeunit 50100 EventSubscriberCodeunit
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::MyCodeunit, 'MyProcedure', '', false, false)]
    local procedure MyProcedure([|MyTable|]: Record MyTable)
    begin
    end;
}

codeunit 50101 MyCodeunit
{
    [IntegrationEvent(false, false)]
    local procedure MyProcedure(var MyTable: Record MyTable)
    begin
    end;
}

table 50000 MyTable { fields { field(1; MyField; Integer) { } } }