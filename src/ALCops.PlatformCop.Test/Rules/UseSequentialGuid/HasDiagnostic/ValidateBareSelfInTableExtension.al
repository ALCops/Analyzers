tableextension 50001 MyTableExtension extends MyTable
{
    procedure DoSomething()
    begin
        Validate("Primary Key", [|CreateGuid()|]);
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
