codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyGuid: Guid;
    begin
        MyGuid := [|CreateGuid()|];
        Message('%1', MyGuid);
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
