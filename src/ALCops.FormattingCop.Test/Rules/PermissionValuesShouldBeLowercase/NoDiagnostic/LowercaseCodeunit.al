[||]codeunit 50100 "My Codeunit"
{
    Permissions = tabledata Alpha = rimd,
                  tabledata Bravo = r;
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
