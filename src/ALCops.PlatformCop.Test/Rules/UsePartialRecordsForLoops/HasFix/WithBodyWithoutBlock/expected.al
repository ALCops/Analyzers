codeunit 50100 MyCodeunit
{
    procedure MyProcedure(Found: Boolean; No: Code[20]): Text
    var
        MyTable: Record MyTable;
        Other: Record MyTable;
    begin
        with Other do begin
            MyTable.SetLoadFields(MyField);
            MyTable.FindSet();
        end;
        repeat
            Message(MyTable.MyField);
        until MyTable.Next() = 0;
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