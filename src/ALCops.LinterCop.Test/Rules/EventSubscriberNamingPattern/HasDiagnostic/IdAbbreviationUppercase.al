// Field "Item ID": "ItemID" is not accepted; only "ItemId" per C# guideline.
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
    local procedure [|OnMyTable_OnAfterValidateEvent_ItemID|](var rec: Record MyTable; var xRec: Record MyTable)
    begin
    end;
}
