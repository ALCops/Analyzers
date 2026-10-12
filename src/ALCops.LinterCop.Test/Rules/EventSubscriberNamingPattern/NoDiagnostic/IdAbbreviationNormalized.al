// Field "Item ID": "ID" is a C# abbreviation and always normalizes to "Id".
table 50100 MyTable
{
    fields
    {
        field(1; "Item ID"; Integer) { }
    }
}

codeunit 50100 MySubscriber
{
    [EventSubscriber(ObjectType::Table, Database::MyTable, OnAfterValidateEvent, "Item ID", false, false)]
    local procedure [|Ontable_myTable_onAfterValidateEvent_itemId|](var rec: Record MyTable; var xRec: Record MyTable)
    begin
    end;
}
