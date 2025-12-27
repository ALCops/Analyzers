permissionset 50100 MyPermissionSet
{
    [|Caption = 'My Caption', Locked = false;|] // Locked property is set to false, which should trigger a diagnostic
}