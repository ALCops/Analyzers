codeunit 50000 MyCodeunit
{
    Permissions = tabledata "ABC Example Header.Line" = rm;

    procedure Test()
    var
        MyTable: Record "ABC Example Header.Line";
    begin
        MyTable.Modify();
    end;
}

table 50000 "ABC Example Header.Line"
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}
