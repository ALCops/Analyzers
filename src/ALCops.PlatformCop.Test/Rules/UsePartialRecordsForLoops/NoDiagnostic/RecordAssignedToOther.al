codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
        TempMyTable: Record MyTable temporary;
    begin
        if [|MyTable.FindSet()|] then
            repeat
                TempMyTable := MyTable;
            until MyTable.Next() < 1;
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }

    keys
    {
        key(PK; MyField) { }
    }
}
