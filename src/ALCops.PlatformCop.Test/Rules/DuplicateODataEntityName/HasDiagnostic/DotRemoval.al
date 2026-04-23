// "PTE No." and "PTE No" both become "PTE_No" after OData transformation (dot and space become _, trailing _ trimmed)
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
