page 50100 "My Page"
{
    [|Permissions = tabledata Alpha = RIMD|];

    actions
    {
        area(Processing)
        {
            action("BOM Level")
            {
                ApplicationArea = All;
                AccessByPermission = tabledata Alpha = R;
            }
        }
    }
}

table 50100 Alpha
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}
