// Page control directly references the PK field - no auto-add collision
page 50100 MyPage
{
    PageType = List;
    SourceTable = MyTable;

    layout
    {
        area(Content)
        {
            repeater(Lines)
            {
                [|field("Primary Key"; Rec."Primary Key") { }|]
            }
        }
    }
}

table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        field(2; MyField; Integer) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}
