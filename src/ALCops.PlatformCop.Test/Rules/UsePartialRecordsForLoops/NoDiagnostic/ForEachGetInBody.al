codeunit 50100 MyCodeunit
{
    procedure MyProcedure(KeyList: List of [Code[20]]): Text
    var
        MyTable: Record MyTable;
        KeyValue: Code[20];
        Result: Text;
    begin
        foreach KeyValue in KeyList do begin
            [|MyTable.Get(KeyValue)|];
            Result += MyTable.MyField;
        end;
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
