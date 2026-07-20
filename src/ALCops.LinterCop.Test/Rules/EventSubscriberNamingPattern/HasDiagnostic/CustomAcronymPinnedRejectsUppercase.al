// Field "acme product" (all-lowercase source -> registry consulted).
// User configured "Acme" as canonical -> only AcmeProduct is accepted, ACMEProduct is not.
table 50100 MyTable
{
    fields
    {
        field(1; "acme product"; Text[50]) { }
    }
}

codeunit 50100 MySubscriber
{
    [EventSubscriber(ObjectType::Table, Database::MyTable, OnAfterValidateEvent, "acme product", false, false)]
    local procedure [|OnMyTable_OnAfterValidateEvent_ACMEProduct|](var rec: Record MyTable; var xRec: Record MyTable)
    begin
    end;
}
