codeunit 50100 MyCodeunit
{
    procedure MyProcedure(Found: Boolean; No: Code[20]; OtherNo: Code[20]): Text
    var
        MyTable: Record MyTable;
    begin
        if Found then
            if [|MyTable.Get(No)|] or [|MyTable.Get(OtherNo)|] then
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