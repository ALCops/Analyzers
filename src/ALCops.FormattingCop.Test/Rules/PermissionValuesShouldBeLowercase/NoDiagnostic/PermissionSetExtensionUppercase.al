[||]permissionset 50100 "Base Permission Set"
{
    Assignable = false;
    Access = Public;

    Permissions = tabledata Alpha = r;
}

permissionsetextension 50100 "My Extension" extends "Base Permission Set"
{
    Permissions = tabledata Alpha = RIMD;
}

table 50100 Alpha
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}
