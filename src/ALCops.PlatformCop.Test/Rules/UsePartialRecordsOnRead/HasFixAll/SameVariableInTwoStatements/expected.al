codeunit 50100 MyCodeunit
{
    procedure MyProcedure(No: Code[20]; OtherNo: Code[20]) Result: Text
    var
        MyTable: Record MyTable;
    begin
        MyTable.SetLoadFields(MyField);
        MyTable.Get(No);
        Result := MyTable.MyField;
        MyTable.SetLoadFields(MyField);
        MyTable.Get(OtherNo);
        Result += MyTable.MyField;
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