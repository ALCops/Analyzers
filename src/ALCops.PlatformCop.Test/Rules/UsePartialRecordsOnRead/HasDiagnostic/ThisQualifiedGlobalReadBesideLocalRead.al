codeunit 50100 MyCodeunit
{
    var
        MyTable: Record MyTable;

    procedure MyProcedure(): Text
    var
        MyTable: Record MyTable;
    begin
        this.MyTable.Get(1);
        [|MyTable.FindFirst()|];
        exit(MyTable.MyField + this.MyTable.MyField);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Entry No."; Integer) { }
        field(2; MyField; Text[100]) { }
    }

    keys
    {
        key(PK; "Entry No.") { }
    }
}
