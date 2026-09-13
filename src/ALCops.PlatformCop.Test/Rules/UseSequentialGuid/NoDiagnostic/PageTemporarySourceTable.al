page 50100 MyPage
{
    SourceTable = MyTable;
    SourceTableTemporary = true;

    trigger OnOpenPage()
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
