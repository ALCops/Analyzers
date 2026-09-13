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

    trigger OnInsert()
    begin
        Validate("Primary Key", [|CreateGuid()|]);
    end;
}
