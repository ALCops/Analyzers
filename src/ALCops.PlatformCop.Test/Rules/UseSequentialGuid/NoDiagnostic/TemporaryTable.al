codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        MyTable."Primary Key" := [|CreateGuid()|];
    end;
}

table 50100 MyTable
{
    TableType = Temporary;

    fields
    {
        field(1; "Primary Key"; Guid) { }
        field(2; MyField; Guid) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}
