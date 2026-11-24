codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text
    var
        MyTable: Record MyTable;
        Result: Text;
    begin
        MyTable.SetLoadFields(Description, MyField);
        MyTable.FindSet();
        repeat
            Result += MyTable.MyField + MyTable.Description;
        until MyTable.Next() = 0;
        exit(Result);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; MyField; Text[100]) { }
        field(3; Description; Text[250]) { }
    }

    keys
    {
        key(PK; "No.") { }
    }
}
