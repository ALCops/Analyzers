// Field "LCY Amount" + user config "KnownAcronyms": ["Lcy"].
// The registered variant "Lcy" is additionally accepted alongside the original "LCY".
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
    local procedure [|OnMyTable_OnAfterValidateEvent_LcyAmount|](var rec: Record MyTable; var xRec: Record MyTable)
    begin
    end;
}
