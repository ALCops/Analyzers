namespace ALCops.Common.Permissions;

/// <summary>
/// Tracks which database operations are permitted for a specific table.
/// </summary>
public sealed class DeclaredPermissionSet
{
    private bool _read;
    private bool _insert;
    private bool _modify;
    private bool _delete;

    public bool HasPermission(DatabaseOperation operation) =>
        operation switch
        {
            DatabaseOperation.Read => _read,
            DatabaseOperation.Insert => _insert,
            DatabaseOperation.Modify => _modify,
            DatabaseOperation.Delete => _delete,
            _ => false
        };

    public void Grant(DatabaseOperation operation)
    {
        switch (operation)
        {
            case DatabaseOperation.Read: _read = true; break;
            case DatabaseOperation.Insert: _insert = true; break;
            case DatabaseOperation.Modify: _modify = true; break;
            case DatabaseOperation.Delete: _delete = true; break;
        }
    }

    /// <summary>
    /// Parses a permission string like "rimd" and grants the corresponding operations.
    /// </summary>
    public void GrantFromPermissionString(string? permissions)
    {
        if (string.IsNullOrEmpty(permissions))
            return;

        foreach (var c in permissions)
        {
            var op = MethodOperationMap.FromPermissionChar(c);
            if (op != DatabaseOperation.None)
                Grant(op);
        }
    }

    public static DeclaredPermissionSet All()
    {
        var set = new DeclaredPermissionSet();
        set.Grant(DatabaseOperation.Read);
        set.Grant(DatabaseOperation.Insert);
        set.Grant(DatabaseOperation.Modify);
        set.Grant(DatabaseOperation.Delete);
        return set;
    }
}
