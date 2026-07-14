report 50100 "My Report"
{
    [|Permissions = tabledata Alpha = R|];
}

table 50100 Alpha
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}
