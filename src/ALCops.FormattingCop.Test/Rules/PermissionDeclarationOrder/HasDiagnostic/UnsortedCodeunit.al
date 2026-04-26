codeunit 50100 "Alpha Codeunit"
{
}

codeunit 50101 "Bravo Codeunit"
{
}

page 50100 "Alpha Page"
{
}

permissionset 50100 "My Permission Set"
{
    Assignable = false;
    Access = Public;

    [|Permissions = page "Alpha Page" = X,
                  codeunit "Bravo Codeunit" = X,
                  codeunit "Alpha Codeunit" = X|];
}
