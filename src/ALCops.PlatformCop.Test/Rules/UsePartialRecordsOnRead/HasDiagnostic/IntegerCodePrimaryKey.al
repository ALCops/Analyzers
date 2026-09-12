codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text
    var
        MyTable: Record MyTable;
    begin
        [|MyTable.Get(1)|];
        exit(MyTable.MyField);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Code"; Integer) { }
        field(2; MyField; Text[100]) { }
    }

    keys
    {
        key(PK; "Code") { }
    }
}
