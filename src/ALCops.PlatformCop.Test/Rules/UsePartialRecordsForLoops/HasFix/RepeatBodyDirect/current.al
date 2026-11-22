codeunit 50100 MyCodeunit
{
    procedure MyProcedure(Counter: Integer): Text
    var
        MyTable: Record MyTable;
        Result: Text;
    begin
        repeat
            Counter -= 1;
            [|MyTable.FindSet()|];
            repeat
                Result += MyTable.MyField;
            until MyTable.Next() = 0;
        until Counter <= 0;
        exit(Result);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; MyField; Text[100]) { }
    }

    keys
    {
        key(PK; "No.") { }
    }
}