codeunit 50100 MyCodeunit
{
    var
        MyTable: Record "My Customer";
}

table 50100 "My Customer"
{
    fields
    {
        field(1; "Primary Key"; Integer) { }
    }
}
