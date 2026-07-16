// Subscriber name is missing the element name segment.
// Expected: OnMyTableOnAfterValidateEventMyField
table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}

codeunit 50100 MySubscriber
{
    [EventSubscriber(ObjectType::Table, Database::MyTable, OnAfterValidateEvent, MyField, false, false)]
    local procedure [|OnMyTableOnAfterValidateEvent|](var rec: Record MyTable; var xRec: Record MyTable)
    begin
    end;
}
