codeunit 50100 MyCodeunit
{
    procedure MyProcedure(MyTable: Record MyTable): Text
    begin
        [|MyTable.Get()|];
        exit(MyTable.MyField);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        field(2; MyField; Text[100]) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}
