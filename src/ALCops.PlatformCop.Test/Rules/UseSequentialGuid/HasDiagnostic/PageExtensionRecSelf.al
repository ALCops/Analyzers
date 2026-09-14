pageextension 50001 MyPageExtension extends MyPage
{
    trigger OnOpenPage()
    begin
        Rec."Primary Key" := [|CreateGuid()|];
    end;
}

page 50100 MyPage
{
    SourceTable = MyTable;
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
