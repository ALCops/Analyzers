codeunit 50101 "My Codeunit"
{
}

table 50100 Alpha
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}

permissionset 50100 "My Permission Set"
{
    Assignable = true;
    Permissions = table Alpha = X,
                  tabledata Alpha = R,
                  codeunit "My Codeunit" = X;
}
