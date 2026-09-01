table 50100 "Item 1"
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}

table 50101 "Item 2"
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}

table 50102 "Item 10"
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}

table 50103 "Item 100"
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}

codeunit 50100 "My Codeunit"
{
    [|Permissions = tabledata "Item 10" = R,
                  tabledata "Item 2" = R,
                  tabledata "Item 1" = R|];
}
