codeunit 50100 MyCodeunit
{
    procedure UpdateRecord()
    var
        MyTable: Record MyTable;
    begin
        [|MyTable.Get()|];
        if MyTable.MyField > 0 then
            MyTable.Modify(true);
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
