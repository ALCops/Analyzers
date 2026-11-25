profile MyProfile
{
    Caption = 'My Profile';
    [|ProfileDescription = 'Describes my profile.', Locked = true|];
    RoleCenter = MyRoleCenter;
}

page 50100 MyRoleCenter
{
    PageType = RoleCenter;
}
