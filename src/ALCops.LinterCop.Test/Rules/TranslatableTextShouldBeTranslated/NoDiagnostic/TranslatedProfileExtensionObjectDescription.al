profileextension MyProfileExt extends MyProfile
{
    [|ProfileDescription = 'Describes my extended profile.'|];
}

profile MyProfile
{
    Caption = 'My Profile';
    RoleCenter = MyRoleCenter;
}

page 50100 MyRoleCenter
{
    PageType = RoleCenter;
}
