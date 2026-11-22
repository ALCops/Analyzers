codeunit 50110 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        MyTable.MyGuidField := [|CreateGuid()|];
    end;
}

tableextension 50111 MyTableExtension extends MyTable
{
    keys
    {
        key(GuidKey; MyGuidField) { }
    }
}
