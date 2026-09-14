namespace MyPublisher.MyExtension.RoleCenter;

/// <summary>
/// Defines the test role center profile.
/// </summary>
profile [|"Test Profile"|]
{
    Caption = 'Test Profile', Locked = true;
    ProfileDescription = 'Role center for the test profile.';
    RoleCenter = "Test Role Center";
}

page 50101 "Test Role Center"
{
    PageType = RoleCenter;
}