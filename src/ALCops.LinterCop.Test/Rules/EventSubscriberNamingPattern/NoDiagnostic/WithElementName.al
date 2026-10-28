// Kebab-case template includes the source type and element name.
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
    local procedure [|"Ontable_my-table_on-after-validate-event_my-field"|](var rec: Record MyTable; var xRec: Record MyTable)
    begin
    end;
}
