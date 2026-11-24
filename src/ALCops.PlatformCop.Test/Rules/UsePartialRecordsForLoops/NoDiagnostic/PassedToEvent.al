codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        if [|MyTable.FindSet()|] then
            repeat
                OnAfterGetRecord(MyTable);
            until MyTable.Next() = 0;
    end;

    [IntegrationEvent(false, false)]
    local procedure OnAfterGetRecord(MyRecord: Record MyTable)
    begin
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; MyField; Text[100]) { }
    }

    keys
    {
        key(PK; "No.") { }
    }
}
