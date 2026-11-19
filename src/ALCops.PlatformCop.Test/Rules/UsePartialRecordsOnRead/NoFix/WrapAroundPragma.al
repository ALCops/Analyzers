codeunit 50100 MyCodeunit
{
    procedure MyProcedure(Found: Boolean; No: Code[20]): Text
    var
        MyTable: Record MyTable;
    begin
        if Found then
#pragma warning disable AA0001
            [|MyTable.Get(No)|];
#pragma warning restore AA0001
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