codeunit 50000 MyCodeunit
{
    Permissions = tabledata MyTable = rm;

    procedure Test()
    var
        MyTable: Record MyTable;
    begin
        MyTable.Modify();
    end;
}

table 50000 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}
