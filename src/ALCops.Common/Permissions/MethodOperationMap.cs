using System.Collections.Immutable;

namespace ALCops.Common.Permissions;

/// <summary>
/// Maps AL built-in record method names to the database operation they perform.
/// </summary>
public static class MethodOperationMap
{
    private static readonly ImmutableDictionary<string, DatabaseOperation> Map =
        ImmutableDictionary.CreateRange(
            StringComparer.OrdinalIgnoreCase,
            [
                // Read operations
                new KeyValuePair<string, DatabaseOperation>("Find", DatabaseOperation.Read),
                new KeyValuePair<string, DatabaseOperation>("FindFirst", DatabaseOperation.Read),
                new KeyValuePair<string, DatabaseOperation>("FindLast", DatabaseOperation.Read),
                new KeyValuePair<string, DatabaseOperation>("FindSet", DatabaseOperation.Read),
                new KeyValuePair<string, DatabaseOperation>("Get", DatabaseOperation.Read),
                new KeyValuePair<string, DatabaseOperation>("GetBySystemId", DatabaseOperation.Read),
                new KeyValuePair<string, DatabaseOperation>("IsEmpty", DatabaseOperation.Read),
                new KeyValuePair<string, DatabaseOperation>("Count", DatabaseOperation.Read),

                // Insert operations
                new KeyValuePair<string, DatabaseOperation>("Insert", DatabaseOperation.Insert),

                // Modify operations
                new KeyValuePair<string, DatabaseOperation>("Modify", DatabaseOperation.Modify),
                new KeyValuePair<string, DatabaseOperation>("ModifyAll", DatabaseOperation.Modify),
                new KeyValuePair<string, DatabaseOperation>("Rename", DatabaseOperation.Modify),

                // Delete operations
                new KeyValuePair<string, DatabaseOperation>("Delete", DatabaseOperation.Delete),
                new KeyValuePair<string, DatabaseOperation>("DeleteAll", DatabaseOperation.Delete),
            ]);

    public static DatabaseOperation GetOperation(string methodName) =>
        Map.TryGetValue(methodName, out var op) ? op : DatabaseOperation.None;

    /// <summary>
    /// The canonical ordering of permission characters: read, insert, modify, delete.
    /// </summary>
    public const string CanonicalOrder = "rimd";

    public static bool IsValidPermissionChar(char c) =>
        FromPermissionChar(c) != DatabaseOperation.None;

    public static char ToPermissionChar(DatabaseOperation operation) =>
        operation switch
        {
            DatabaseOperation.Read => 'r',
            DatabaseOperation.Insert => 'i',
            DatabaseOperation.Modify => 'm',
            DatabaseOperation.Delete => 'd',
            _ => ' '
        };

    public static DatabaseOperation FromPermissionChar(char c) =>
        char.ToLowerInvariant(c) switch
        {
            'r' => DatabaseOperation.Read,
            'i' => DatabaseOperation.Insert,
            'm' => DatabaseOperation.Modify,
            'd' => DatabaseOperation.Delete,
            _ => DatabaseOperation.None
        };
}
