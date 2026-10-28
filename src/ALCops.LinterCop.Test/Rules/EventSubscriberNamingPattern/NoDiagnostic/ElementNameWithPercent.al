// Snake-case field "Line Discount %" drops "%" and renders line_discount.
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
    local procedure [|Ontable_my_table_on_after_validate_event_line_discount|](var rec: Record MyTable; var xRec: Record MyTable)
    begin
    end;
}
