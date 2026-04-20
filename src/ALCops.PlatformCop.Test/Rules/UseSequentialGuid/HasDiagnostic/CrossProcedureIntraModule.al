codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    begin
        SetPrimaryKey([|CreateGuid()|]);
    end;

    local procedure SetPrimaryKey(MyGuid: Guid)
    var
        MyTable: Record MyTable;
    begin
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
