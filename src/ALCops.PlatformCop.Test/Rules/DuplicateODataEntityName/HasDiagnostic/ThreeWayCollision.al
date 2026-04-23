// "A.mt", "Am.t", and "Amt." all become "Amt" after dot removal
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
                [|field("A.mt"; Rec.MyField) { }|]
                [|field("Am.t"; Rec.MyField2) { }|]
                [|field("Amt."; Rec.MyField3) { }|]
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
