codeunit 50000 MyCodeunit
{
    Permissions = tabledata MyTable = r;

    procedure Test()
    var
        MyTable: Record MyTable;
    begin
        [|MyTable.Modify();|]
    end;
}

table 50000 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}
