codeunit 50100 MyCodeunit
{
    var
        GlobalId: Guid;

    procedure AssignGuid()
    begin
        GlobalId := [|CreateGuid()|];
    end;

    procedure UseGuid()
    var
        MyTable: Record MyTable;
    begin
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
