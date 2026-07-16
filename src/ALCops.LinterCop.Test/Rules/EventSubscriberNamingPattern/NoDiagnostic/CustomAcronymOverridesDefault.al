// Field "vat entry" (all-lowercase source -> registry consulted).
// User overrode default "VAT" -> "Vat" is now the accepted canonical casing.
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
    local procedure [|OnMyTable_OnAfterValidateEvent_VatEntry|](var rec: Record MyTable; var xRec: Record MyTable)
    begin
    end;
}
