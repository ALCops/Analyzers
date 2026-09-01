// Default template with a table field whose name contains a space:
// "Sales Header" + _ + OnAfterValidateEvent + _ + "Document Type" -> quoted identifier.
table 50100 "Sales Header"
{
    fields
    {
        field(1; "Document Type"; Option) { OptionMembers = Quote,Order; }
    }
}

codeunit 50100 MySubscriber
{
    [EventSubscriber(ObjectType::Table, Database::"Sales Header", OnAfterValidateEvent, "Document Type", false, false)]
    local procedure [|"Sales Header_OnAfterValidateEvent_Document Type"|](var rec: Record "Sales Header"; var xRec: Record "Sales Header")
    begin
    end;
}
