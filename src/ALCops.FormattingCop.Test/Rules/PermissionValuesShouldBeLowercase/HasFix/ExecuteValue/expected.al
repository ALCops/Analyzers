codeunit 50100 "My Codeunit"
{
    Permissions = tabledata Alpha = x;
}

table 50100 Alpha
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}
