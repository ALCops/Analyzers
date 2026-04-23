// Obsolete pages are skipped
page 50100 MyPage
{
    PageType = List;
    SourceTable = MyTable;
    ObsoleteState = Pending;
    ObsoleteReason = 'Use MyNewPage instead.';

    layout
    {
        area(Content)
        {
            repeater(Lines)
            {
                [|field("PTE No."; Rec.MyField) { }|]
                [|field("PTE No"; Rec.MyField2) { }|]
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
