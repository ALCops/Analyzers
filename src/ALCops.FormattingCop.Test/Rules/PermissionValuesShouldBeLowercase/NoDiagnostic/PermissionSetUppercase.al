[||]permissionset 50100 "My Permission Set"
{
    Assignable = false;
    Access = Public;

    Permissions = tabledata Alpha = RIMD,
                  codeunit "Target Codeunit" = X;
}

codeunit 50101 "Target Codeunit"
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
