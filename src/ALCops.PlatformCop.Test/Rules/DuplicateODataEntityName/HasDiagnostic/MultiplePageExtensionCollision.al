// Two page extensions adding controls that collide after OData transformation
page 50100 MyPage
{
    PageType = Card;
    SourceTable = MyTable;

    layout
    {
        area(Content) { }
    }
}

pageextension 50100 MyExtension extends MyPage
{
    layout
    {
        addlast(Content)
        {
            [|field("PTE No"; Rec.MyField) { ApplicationArea = All; }|]
        }
    }
}

pageextension 50101 MyExtension2 extends MyPage
{
    layout
    {
        addlast(Content)
        {
            [|field("PTE No."; Rec.MyField) { ApplicationArea = All; }|]
        }
    }
}

table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Integer) { }
        field(2; MyField; Integer) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}
