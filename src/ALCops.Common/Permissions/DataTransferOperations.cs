using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Permissions;

/// <summary>
/// Maps the AL <c>DataTransfer</c> methods that actually touch the database to the operations
/// they perform on the source and destination table named by <c>SetTables</c>.
/// <para>
/// Deliberately separate from <see cref="MethodOperationMap"/>: that map is keyed on methods
/// called <em>on a record receiver</em> (and is mirrored by <c>RecordMethodClassification</c>),
/// whereas these methods are called on a <c>DataTransfer</c> receiver and take their tables
/// from arguments. Merging them would make every record variable with a user-defined
/// <c>CopyFields</c>/<c>CopyRows</c> method look like a DB operation.
/// </para>
/// </summary>
public static class DataTransferOperations
{
    /// <summary>
    /// The <c>DataTransfer</c> builder method that names the source and destination tables.
    /// </summary>
    public const string SetTablesMethodName = "SetTables";

    private static readonly ImmutableDictionary<string, (DatabaseOperation Source, DatabaseOperation Destination)> Map =
        ImmutableDictionary.CreateRange(
            SemanticFacts.NameEqualityComparer,
            [
                // Reads the source rows, updates existing destination rows.
                new KeyValuePair<string, (DatabaseOperation, DatabaseOperation)>(
                    "CopyFields", (DatabaseOperation.Read, DatabaseOperation.Modify)),

                // Reads the source rows, inserts them as new destination rows.
                new KeyValuePair<string, (DatabaseOperation, DatabaseOperation)>(
                    "CopyRows", (DatabaseOperation.Read, DatabaseOperation.Insert)),
            ]);

    /// <summary>
    /// Returns true when the method name is a <c>DataTransfer</c> method that executes the
    /// transfer. The other <c>DataTransfer</c> methods only build up the transfer definition
    /// and perform no database access on their own.
    /// </summary>
    public static bool IsExecutor(string methodName) => Map.ContainsKey(methodName);

    /// <summary>
    /// Resolves the operations an executor performs on the source and destination table.
    /// </summary>
    public static bool TryGetOperations(
        string methodName,
        out DatabaseOperation sourceOperation,
        out DatabaseOperation destinationOperation)
    {
        if (Map.TryGetValue(methodName, out var operations))
        {
            sourceOperation = operations.Source;
            destinationOperation = operations.Destination;
            return true;
        }

        sourceOperation = DatabaseOperation.None;
        destinationOperation = DatabaseOperation.None;
        return false;
    }
}
