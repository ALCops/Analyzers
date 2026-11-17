report 50100 MyReport
{
    ProcessingOnly = true;

    dataset
    {
        dataitem(MyTable; MyTable)
        {
        }
    }

    procedure MyProcedure(): Text
    var
        MyTable: Record MyTable;
    begin
        MyTable.SetRange("Entry No.", 1);
        [|this.MyTable.FindFirst()|];
        exit(this.MyTable.MyField);
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
