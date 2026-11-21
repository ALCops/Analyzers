codeunit 50100 MyCodeunit
{
    procedure MyProcedure(No: Code[20]): Text
    var
        MyTable: Record MyTable;
        i: Integer;
    begin
        for i := 1 to 3 do begin
            MyTable.SetLoadFields(MyField);
            MyTable.Get(No);
        end;
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