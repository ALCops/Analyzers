query 50100 "My Query"
{
    [|Permissions = tabledata Alpha = RIMD|];

    elements
    {
        dataitem(Alpha; Alpha)
        {
            column(MyField; MyField) { }
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
