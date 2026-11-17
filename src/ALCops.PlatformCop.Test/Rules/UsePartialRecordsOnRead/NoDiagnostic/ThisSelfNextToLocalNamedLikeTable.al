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

    procedure MyProcedure(): Text
    var
        MyTable: Record MyTable;
    begin
        MyTable.SetRange("Entry No.", 1);
        [|this.FindFirst()|];
        exit(this.MyField);
    end;
}
