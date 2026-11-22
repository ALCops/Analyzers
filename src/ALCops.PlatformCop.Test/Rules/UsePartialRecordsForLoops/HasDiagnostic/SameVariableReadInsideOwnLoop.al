codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text
    var
        MyTable: Record MyTable;
        Result: Text;
    begin
        [|MyTable.FindSet()|];
        repeat
            Result += MyTable.MyField;
            MyTable.Get(MyTable.Parent);
        until MyTable.Next() = 0;
        exit(Result);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Entry No."; Integer) { }
        field(2; Parent; Integer) { }
        field(3; MyField; Text[100]) { }
    }

    keys
    {
        key(PK; "Entry No.") { }
    }
}
