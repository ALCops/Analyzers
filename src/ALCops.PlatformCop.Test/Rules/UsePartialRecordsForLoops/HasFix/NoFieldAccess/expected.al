codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Integer
    var
        MyTable: Record MyTable;
        Counter: Integer;
    begin
        MyTable.SetLoadFields("No.");
        MyTable.FindSet();
        repeat
            Counter += 1;
        until MyTable.Next() = 0;
        exit(Counter);
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
