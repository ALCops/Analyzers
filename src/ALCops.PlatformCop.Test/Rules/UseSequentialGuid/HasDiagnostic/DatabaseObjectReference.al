codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyGuid: Guid;
        MyTable: Record MyTable;
    begin
        MyGuid := [|CreateGuid()|];
        MyTable."Source Type" := DATABASE::MyTable;
        MyTable."Primary Key" := MyGuid;
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Guid) { }
        field(2; "Source Type"; Integer) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}
