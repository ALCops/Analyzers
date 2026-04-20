codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
        MyGuid: Guid;
    begin
        MyGuid := [|CreateGuid()|];
        MyTable."Primary Key" := MyGuid;
    end;
}

table 50100 MyTable
{
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
