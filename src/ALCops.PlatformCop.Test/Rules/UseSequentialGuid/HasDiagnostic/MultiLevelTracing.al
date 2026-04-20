codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    begin
        ProcA([|CreateGuid()|]);
    end;

    local procedure ProcA(MyGuid: Guid)
    begin
        ProcB(MyGuid);
    end;

    local procedure ProcB(MyGuid: Guid)
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
