codeunit 50100 MyCodeunit
{
    Subtype = Test;

    procedure MyProcedure(Found: Boolean; No: Code[20]): Text
    var
        MyTable: Record MyTable;
        OtherTable: Record MyTable;
    begin
        asserterror if [|MyTable.Get(No)|] then exit('');
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