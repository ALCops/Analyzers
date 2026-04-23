// "Balance (LCY)" becomes "Balance_LCY" (space, parens → _, consecutive _ deduped, trailing _ trimmed)
// "Balance_LCY" stays "Balance_LCY" — both collide
page 50100 MyPage
{
    PageType = Document;
    SourceTable = MyTable;

    layout
    {
        area(Content)
        {
            group(General)
            {
                [|field("Balance (LCY)"; Rec.MyField) { }|]
                [|field(Balance_LCY; Rec.MyField2) { }|]
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
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}
