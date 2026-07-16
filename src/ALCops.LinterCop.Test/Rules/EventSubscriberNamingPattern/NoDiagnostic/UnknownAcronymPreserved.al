// Field "XYZ Report": unknown 3+ letter uppercase word -> preserved variant accepted.
table 50100 MyTable
{
    fields
    {
        field(1; "XYZ Report"; Text[50]) { }
    }
}

codeunit 50100 MySubscriber
{
    [EventSubscriber(ObjectType::Table, Database::MyTable, OnAfterValidateEvent, "XYZ Report", false, false)]
    local procedure [|OnMyTable_OnAfterValidateEvent_XYZReport|](var rec: Record MyTable; var xRec: Record MyTable)
    begin
    end;
}
