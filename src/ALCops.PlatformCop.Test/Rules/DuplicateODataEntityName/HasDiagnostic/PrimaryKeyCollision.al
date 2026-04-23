// Control "Primary_Key" collides with the auto-included PK field "Primary Key" (both become "Primary_Key")
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
                [|field(Primary_Key; Rec.MyField) { }|]
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
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}
