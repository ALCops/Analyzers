codeunit 50000 MyCodeunit
{
    Permissions = tabledata MyTable = d;

    procedure Test()
    var
        MyTable: Record MyTable;
    begin
        [|MyTable.DeleteAll();|]
    end;
}

table 50000 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}
