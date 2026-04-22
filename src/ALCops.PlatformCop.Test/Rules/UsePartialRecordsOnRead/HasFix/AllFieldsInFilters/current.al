codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Boolean
    var
        MyTable: Record MyTable;
    begin
        MyTable.SetRange(MyField1, 'A');
        MyTable.SetRange(MyField2, 'B');
        exit([|MyTable.FindFirst()|]);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField1; Text[100]) { }
        field(2; MyField2; Text[100]) { }
    }

    keys
    {
        key(PK; MyField1) { }
    }
}
