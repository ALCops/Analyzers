codeunit 50109 MyCodeunit
{
    [EventSubscriber(ObjectType::Table, Database::MyTable, 'OnAfterInsertEvent', '', false, false)]
    local procedure OnAfterInsertMyTable(var Rec: Record MyTable)
    begin
        [|Rec|].TestField("No.");
    end;

    procedure CheckRecord(var rec: Record MyTable)
    begin
        [|rec|].TestField("No.");
    end;
}

table 50109 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
    }
}
