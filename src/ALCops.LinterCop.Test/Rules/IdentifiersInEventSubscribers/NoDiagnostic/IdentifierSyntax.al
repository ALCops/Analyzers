codeunit 50100 MyCodeunit
{
    [|[EventSubscriber(ObjectType::Table, Database::MyTable, OnAfterDeleteEvent, '', false, false)]|]
    local procedure OnAfterDeleteEvent(var Rec: Record MyTable; RunTrigger: Boolean)
    begin

    end;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }