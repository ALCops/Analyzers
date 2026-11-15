page 50100 MyRoleCenter
{
    PageType = RoleCenter;
}

profile MyProfile
{
    Caption = 'My Profile';
    RoleCenter = MyRoleCenter;
}

profileextension MyProfileExt extends MyProfile
{
    [|Caption|] = '';
}