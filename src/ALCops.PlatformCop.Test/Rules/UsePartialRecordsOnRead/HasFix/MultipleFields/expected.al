codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text
    var
        MyTable: Record MyTable;
    begin
        MyTable.SetLoadFields(MyTable.Description, MyTable.MyField);
        MyTable.FindFirst();
        exit(MyTable.MyField + MyTable.Description);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        field(2; MyField; Text[100]) { }
        field(3; Description; Text[250]) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}
