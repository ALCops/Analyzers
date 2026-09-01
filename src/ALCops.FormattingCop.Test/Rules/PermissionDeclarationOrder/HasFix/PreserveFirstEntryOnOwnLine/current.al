codeunit 50100 "Post. Appr. Setup Wiz Hlp"
{
}

codeunit 50101 "Post Inv. Appr. Setup Hlp"
{
}

permissionset 50100 GeneratedPermission
{
    Assignable = true;
    [|Permissions =
        codeunit "Post Inv. Appr. Setup Hlp" = X,
        codeunit "Post. Appr. Setup Wiz Hlp" = X|];
}
