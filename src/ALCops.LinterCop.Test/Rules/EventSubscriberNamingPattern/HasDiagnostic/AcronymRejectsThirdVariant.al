// Field "LCY Amount" + user config "KnownAcronyms": ["Lcy"].
// Only "LCY" (original, preferred) and "Lcy" (registered variant) are accepted;
// any third spelling such as "LcY" must still produce a diagnostic.
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
