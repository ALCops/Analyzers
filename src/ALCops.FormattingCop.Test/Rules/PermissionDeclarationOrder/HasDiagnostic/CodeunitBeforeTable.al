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
    [|Permissions = codeunit "My Codeunit" = X,
                  tabledata Alpha = R|];
}
