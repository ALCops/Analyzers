// Template On{EventSource}_{EventName}[_{ElementName}] with element name set.
// On + MyTable + _ + OnAfterValidateEvent + _ + MyField = OnMyTable_OnAfterValidateEvent_MyField
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
    local procedure [|OnMyTable_OnAfterValidateEvent_MyField|](var rec: Record MyTable; var xRec: Record MyTable)
    begin
    end;
}
