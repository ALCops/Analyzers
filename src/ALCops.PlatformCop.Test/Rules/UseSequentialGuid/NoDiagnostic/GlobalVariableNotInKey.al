codeunit 50100 MyCodeunit
{
    var
        GlobalId: Guid;

    procedure AssignGuid()
    begin
        GlobalId := [|CreateGuid()|];
    end;

    procedure UseGuid()
    begin
        Message('%1', GlobalId);
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
