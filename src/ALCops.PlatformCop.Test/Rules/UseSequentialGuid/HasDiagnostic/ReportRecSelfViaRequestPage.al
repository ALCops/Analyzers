report 50100 MyReport
{
    requestpage
    {
        SourceTable = MyTable;
    }

    trigger OnPreReport()
    begin
        Rec."Primary Key" := [|CreateGuid()|];
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
