/// <summary>
/// Shows the role center.
/// </summary>
page 50100 MyRoleCenter
{
    PageType = RoleCenter;
}

/// <summary>
/// Defines the sample profile.
/// </summary>
profile MyProfile
{
    Caption = 'My Profile';
    RoleCenter = MyRoleCenter;
}

/// <summary>
/// Adjusts the sample profile.
/// </summary>
profileextension [|MyProfileExt|] extends MyProfile
{
}