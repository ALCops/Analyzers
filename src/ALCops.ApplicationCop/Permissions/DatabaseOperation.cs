namespace ALCops.ApplicationCop.Permissions;

/// <summary>
/// Represents a database operation that requires permissions.
/// Mirrors the pattern from Microsoft.Dynamics.Nav.AppSourceCop.Permissions.
/// </summary>
internal enum DatabaseOperation
{
    None,
    Read,
    Insert,
    Modify,
    Delete
}
