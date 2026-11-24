codeunit 50100 MyCodeunit
{
    var
        MyTable: Record MyTable;

    procedure MyProcedure(): Text
    var
        MyTable: Record MyTable;
        Result: Text;
    begin
        MyTable.SetRange("Entry No.", 1);
        [|this.MyTable.FindSet()|];
        repeat
            Result += this.MyTable.MyField;
        until this.MyTable.Next() = 0;
        exit(Result);
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
