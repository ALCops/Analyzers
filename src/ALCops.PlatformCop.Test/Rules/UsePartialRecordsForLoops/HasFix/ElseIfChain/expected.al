codeunit 50100 MyCodeunit
{
    procedure MyProcedure(No: Code[20]) Result: Text
    var
        MyTable: Record MyTable;
    begin
        if No = '' then begin
            Result := 'None';
        end else begin
            MyTable.SetLoadFields(MyField);
            if MyTable.FindSet() then
                repeat
                    Result += MyTable.MyField;
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