codeunit 50100 MyCodeunit
{
    procedure MyProcedure(Counter: Integer; No: Code[20]): Text
    var
        MyTable: Record MyTable;
    begin
        repeat
            Counter -= 1;
            MyTable.SetLoadFields(MyField);
            MyTable.Get(No);
        until Counter <= 0;
        exit(MyTable.MyField);
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