codeunit 50000 MyCodeunit
{
    Permissions = tabledata MyTable = im;

    procedure Test()
    var
        MyTable: Record MyTable;
    begin
        [|MyTable.FindFirst();|]
    end;
}

table 50000 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}
