table 50100 AA
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}

table 50101 "A B"
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}

codeunit 50100 "My Codeunit"
{
    [|Permissions = tabledata AA = R,
                  tabledata "A B" = R|];
}
