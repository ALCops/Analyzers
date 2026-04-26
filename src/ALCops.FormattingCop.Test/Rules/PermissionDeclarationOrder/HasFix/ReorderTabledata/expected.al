codeunit 50100 "My Codeunit"
{
    Permissions = tabledata "Dimension Set Entry" = r,
                  tabledata "My Other Table" = R,
                  tabledata "My Table" = R,
                  tabledata "Purchase Document" = R;
}

table 50100 "My Table"
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}

table 50101 "Dimension Set Entry"
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}

table 50102 "My Other Table"
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}

table 50103 "Purchase Document"
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}
