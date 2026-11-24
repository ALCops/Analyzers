codeunit 50100 MyCodeunit
{
    procedure MyProcedure(Found: Boolean): Text
    var
        MyTable: Record MyTable;
    begin
        if Found then begin
            MyTable.SetLoadFields(MyField);
            if MyTable.FindSet() then
                repeat
                    Message(MyTable.MyField);
                until MyTable.Next() = 0;
        end;
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