page 50100 MyPage
{
    SourceTable = MyTable;

    trigger OnOpenPage()
    begin
        "Primary Key" := [|CreateGuid()|];
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
