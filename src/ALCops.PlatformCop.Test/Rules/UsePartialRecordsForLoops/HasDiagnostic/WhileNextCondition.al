codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text
    var
        MyTable: Record MyTable;
        Result: Text;
    begin
        if [|MyTable.FindSet()|] then begin
            Result := MyTable.MyField;
            while MyTable.Next() <> 0 do
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
