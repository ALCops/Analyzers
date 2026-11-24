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
        until MyTable.Next() = 0;
        exit(Result);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Bus. Posting Group"; Code[20]) { }
        field(2; "Prod. Posting Group"; Code[20]) { }
        field(3; MyField; Text[100]) { }
    }

    keys
    {
        key(PK; "Bus. Posting Group", "Prod. Posting Group") { }
    }
}
