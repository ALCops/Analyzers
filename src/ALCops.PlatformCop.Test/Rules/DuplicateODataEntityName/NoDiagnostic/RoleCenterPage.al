// RoleCenter pages are excluded from this check
page 50100 MyRoleCenter
{
    PageType = RoleCenter;

    layout
    {
        area(RoleCenter)
        {
            [|part("PTE No."; MyListPart) { }|]
            [|part("PTE No"; MyListPart) { }|]
        }
    }
}

page 50101 MyListPart
{
    PageType = ListPart;
    SourceTable = MyTable;

    layout
    {
        area(Content)
        {
            repeater(Lines)
            {
                field(MyField; Rec.MyField) { ApplicationArea = All; }
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
