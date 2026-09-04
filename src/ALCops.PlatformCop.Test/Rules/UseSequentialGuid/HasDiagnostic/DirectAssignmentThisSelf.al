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

    procedure MyProcedure()
    begin
        this."Primary Key" := [|CreateGuid()|];
    end;
}
