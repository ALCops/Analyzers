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

    trigger OnInsert()
    begin
        Rec."Primary Key" := [|CreateGuid()|];
    end;
}
