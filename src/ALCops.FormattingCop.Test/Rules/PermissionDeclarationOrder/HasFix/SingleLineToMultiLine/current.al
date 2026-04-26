codeunit 50100 "My Codeunit"
{
    [|Permissions = tabledata Bravo = R, tabledata Alpha = R|];
}

table 50100 Alpha
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}

table 50101 Bravo
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}
