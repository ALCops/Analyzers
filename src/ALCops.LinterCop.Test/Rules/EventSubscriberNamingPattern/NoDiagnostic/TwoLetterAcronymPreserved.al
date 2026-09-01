// Field "IO Log": two-letter all-uppercase abbreviation stays uppercase.
table 50100 MyTable
{
    fields
    {
        field(1; "IO Log"; Text[50]) { }
    }
}

codeunit 50100 MySubscriber
{
    [EventSubscriber(ObjectType::Table, Database::MyTable, OnAfterValidateEvent, "IO Log", false, false)]
    local procedure [|OnMyTable_OnAfterValidateEvent_IOLog|](var rec: Record MyTable; var xRec: Record MyTable)
    begin
    end;
}
