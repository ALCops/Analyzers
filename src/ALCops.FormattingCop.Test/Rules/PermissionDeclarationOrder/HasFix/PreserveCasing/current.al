codeunit 50100 "My Codeunit"
{
    [|Permissions = tabledata Charlie = rImd,
                  tabledata Alpha = RIMD,
                  tabledata Bravo = Ri|];
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

table 50102 Charlie
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}
