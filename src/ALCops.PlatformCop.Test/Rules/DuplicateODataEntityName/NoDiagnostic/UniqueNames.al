// All controls have unique OData names
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
                [|field("Customer No."; Rec.MyField) { }|]
                [|field("Customer Name"; Rec.MyField2) { }|]
                [|field(Amount; Rec.MyField3) { }|]
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
