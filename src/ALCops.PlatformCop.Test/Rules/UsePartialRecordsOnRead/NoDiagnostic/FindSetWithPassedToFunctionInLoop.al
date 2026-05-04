codeunit 50100 MyCodeunit
{
    procedure ProcessRecords()
    var
        MyTable: Record MyTable;
    begin
        if [|MyTable.FindSet()|] then
            repeat
                ProcessRecord(MyTable);
            until MyTable.Next() = 0;
    end;

    local procedure ProcessRecord(var Rec: Record MyTable)
    begin
        Rec.MyField := Rec.MyField + 1;
        Rec.Modify();
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }

    keys
    {
        key(PK; MyField) { }
    }
}
