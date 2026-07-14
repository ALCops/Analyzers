report 50100 "My Report"
{
    requestpage
    {
        [|Permissions = tabledata Alpha = RIMD|];
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
