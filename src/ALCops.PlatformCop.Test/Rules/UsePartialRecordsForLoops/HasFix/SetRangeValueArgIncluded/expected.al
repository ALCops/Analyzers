codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Integer
    var
        MyTable: Record MyTable;
        Counter: Integer;
    begin
        MyTable.SetRange(MyField1, MyTable.MyField2);
        MyTable.SetLoadFields(MyField2);
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
        field(1; MyField1; Text[100]) { }
        field(2; MyField2; Text[100]) { }
    }

    keys
    {
        key(PK; MyField1) { }
    }
}
