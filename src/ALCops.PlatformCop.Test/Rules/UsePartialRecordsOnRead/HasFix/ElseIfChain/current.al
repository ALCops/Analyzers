codeunit 50100 MyCodeunit
{
    procedure MyProcedure(No: Code[20]) Result: Text
    var
        MyTable: Record MyTable;
    begin
        if No = '' then begin
            Result := 'None';
        end else
            if [|MyTable.Get(No)|] then
                Result := MyTable.MyField;
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