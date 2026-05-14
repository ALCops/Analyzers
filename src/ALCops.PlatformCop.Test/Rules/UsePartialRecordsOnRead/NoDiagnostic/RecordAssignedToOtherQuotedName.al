codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        "My Table": Record MyTable;
        TempMyTable: Record MyTable temporary;
    begin
        if [|"My Table".FindSet()|] then
            repeat
                TempMyTable := "My Table";
            until "My Table".Next() < 1;
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
