codeunit 50100 MyCodeunit
{
    var
        GlobalId: Guid;

    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        GlobalId := [|CreateGuid()|];
        MyTable."Primary Key" := GlobalId;
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Guid) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}
