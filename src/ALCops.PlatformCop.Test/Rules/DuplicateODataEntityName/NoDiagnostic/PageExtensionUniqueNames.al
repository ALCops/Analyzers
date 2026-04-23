// Extension controls have unique OData names relative to base page
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
                field("Item No."; Rec.MyField) { ApplicationArea = All; }
            }
        }
    }
}

pageextension 50100 MyPageExt extends MyPage
{
    layout
    {
        addlast(Lines)
        {
            [|field("Item Description"; Rec.MyField2) { }|]
        }
    }
}

table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Integer) { }
        field(2; MyField; Integer) { }
        field(3; MyField2; Integer) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}
