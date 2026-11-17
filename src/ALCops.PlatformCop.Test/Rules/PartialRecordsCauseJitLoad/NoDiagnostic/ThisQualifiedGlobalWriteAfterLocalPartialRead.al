codeunit 50100 MyCodeunit
{
    var
        MyTable: Record MyTable;

    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        [|MyTable.SetLoadFields(MyField)|];
        MyTable.FindFirst();
        this.MyTable.Insert();
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
