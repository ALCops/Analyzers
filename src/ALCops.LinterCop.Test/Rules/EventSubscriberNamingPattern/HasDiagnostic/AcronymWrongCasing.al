// Field "LCY Amount": neither LCYAmount nor LcyAmount matches LcYAmount -> diagnostic.
table 50100 MyTable
{
    fields
    {
        field(1; "LCY Amount"; Decimal) { }
    }
}

codeunit 50100 MySubscriber
{
    [EventSubscriber(ObjectType::Table, Database::MyTable, OnAfterValidateEvent, "LCY Amount", false, false)]
    local procedure [|OnMyTable_OnAfterValidateEvent_LcYAmount|](var rec: Record MyTable; var xRec: Record MyTable)
    begin
    end;
}
