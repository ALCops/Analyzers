namespace ALCops.Common.Permissions;

/// <summary>
/// Represents a database operation that requires permissions.
/// Mirrors the pattern from Microsoft.Dynamics.Nav.AppSourceCop.Permissions.
/// </summary>
public enum DatabaseOperation
{
    None,
    Read,
    Insert,
    Modify,
    Delete
}
