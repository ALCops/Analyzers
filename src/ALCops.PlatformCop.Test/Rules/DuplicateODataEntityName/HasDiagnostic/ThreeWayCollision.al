// "A B", "A.B", and "A-B" all become "A_B" after OData transformation (space, dot, hyphen → _)
page 50100 MyPage
{
    PageType = Card;
    SourceTable = MyTable;

    layout
    {
        area(Content)
        {
            group(General)
            {
                [|field("A B"; Rec.MyField) { }|]
                [|field("A.B"; Rec.MyField2) { }|]
                [|field("A-B"; Rec.MyField3) { }|]
            }
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
        field(4; MyField3; Integer) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}
