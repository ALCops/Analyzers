codeunit 50100 "My Codeunit"
{
}

page 50100 "My Page"
{
}

report 50100 "My Report"
{
}

table 50100 "My Table"
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}

permissionset 50100 "My Permission Set"
{
    Assignable = false;
    Access = Public;

    [|Permissions = tabledata "My Table" = R,
                  codeunit "My Codeunit" = X,
                  page "My Page" = X,
                  report "My Report" = X|];
}
