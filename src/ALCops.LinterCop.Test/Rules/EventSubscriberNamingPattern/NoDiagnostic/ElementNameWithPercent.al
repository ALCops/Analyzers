// Field "Line Discount %" -> "%" is treated as a word delimiter and dropped, producing
// LineDiscount as the accepted element-name rendering.
table 50100 MyTable
{
    fields
    {
        field(1; "Line Discount %"; Decimal) { }
    }
}

codeunit 50100 MySubscriber
{
    [EventSubscriber(ObjectType::Table, Database::MyTable, OnAfterValidateEvent, "Line Discount %", false, false)]
    local procedure [|OnMyTable_OnAfterValidateEvent_LineDiscount|](var rec: Record MyTable; var xRec: Record MyTable)
    begin
    end;
}
