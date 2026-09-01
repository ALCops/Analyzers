codeunit 50100 "My Codeunit"
{
    [|Permissions = tabledata Alpha = Rimd|];
}

table 50100 Alpha
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}
