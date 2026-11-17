codeunit 50100 MyCodeunit
{
    var
        MyTable: Record MyTable;

    procedure MyProcedure(): Text
    var
        MyTable: Record MyTable;
    begin
        MyTable.SetLoadFields(MyField);
        MyTable.FindFirst();
        exit(MyTable.MyField + this.MyTable.OtherField);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Entry No."; Integer) { }
        field(2; MyField; Text[100]) { }
        field(3; OtherField; Text[100]) { }
    }

    keys
    {
        key(PK; "Entry No.") { }
    }
}
