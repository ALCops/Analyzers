codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text
    var
        MyTable: Record MyTable;
        MyItem: Record MyItem;
        Result: Text;
    begin
        MyTable.SetLoadFields(MyTable.MyField);
        if MyTable.FindSet() then
            repeat
                [|MyItem.Get(MyTable.MyField)|];
                Result += MyItem.Description;
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
    }

    keys
    {
        key(PK; "No.") { }
    }
}

table 50101 MyItem
{
    fields
    {
        field(1; "No."; Text[100]) { }
        field(2; Description; Text[100]) { }
    }

    keys
    {
        key(PK; "No.") { }
    }
}
