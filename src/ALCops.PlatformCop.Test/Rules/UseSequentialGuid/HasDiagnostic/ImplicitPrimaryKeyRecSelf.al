table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Guid) { }
    }

    trigger OnInsert()
    begin
        Rec."Primary Key" := [|CreateGuid()|];
    end;
}
