// Field "vat entry" (all-lowercase source -> registry consulted).
// User overrode default "VAT" -> canonical is now "Vat", so "VATEntry" is rejected.
table 50100 MyTable
{
    fields
    {
        field(1; "vat entry"; Text[50]) { }
    }
}

codeunit 50100 MySubscriber
{
    [EventSubscriber(ObjectType::Table, Database::MyTable, OnAfterValidateEvent, "vat entry", false, false)]
    local procedure [|OnMyTable_OnAfterValidateEvent_VATEntry|](var rec: Record MyTable; var xRec: Record MyTable)
    begin
    end;
}
