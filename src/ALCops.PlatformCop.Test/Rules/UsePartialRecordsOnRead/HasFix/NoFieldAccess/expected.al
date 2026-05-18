codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Boolean
    var
        MyTable: Record MyTable;
    begin
        MyTable.SetLoadFields("Primary Key");
        exit(MyTable.Get());
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        field(2; MyField; Text[100]) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}
