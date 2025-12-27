[|codeunit 50100 MyCodeunit|]
{
    procedure MyProcedure()
    begin
    end;
}

permissionset 50100 MyPermissionSet
{
    Permissions = Codeunit MyCodeunit = X;
}