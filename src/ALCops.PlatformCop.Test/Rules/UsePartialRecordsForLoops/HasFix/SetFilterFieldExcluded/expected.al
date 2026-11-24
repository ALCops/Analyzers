codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text
    var
        MyTable: Record MyTable;
        Result: Text;
    begin
        MyTable.SetFilter(MyField1, '%1', 'A');
        MyTable.SetLoadFields(MyField2);
        MyTable.FindSet();
        repeat
            Result += MyTable.MyField2;
        until MyTable.Next() = 0;
        exit(Result);
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
