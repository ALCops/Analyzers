using System.Collections.Immutable;
using ALCops.Common.Permissions;

namespace ALCops.Common;

/// <summary>
/// Centralized classification of AL built-in record methods by their behavioral category.
/// Builds on <see cref="MethodOperationMap"/> (RIMD permission mapping) and adds categories
/// for partial records, triggers, and other non-permission concerns.
/// </summary>
public static class RecordMethodClassification
{
    /// <summary>
    /// Methods that read record data from the database.
    /// Includes Find, FindFirst, FindLast, FindSet, Get, GetBySystemId, IsEmpty, Count.
    /// Derived from <see cref="MethodOperationMap"/> where operation == <see cref="DatabaseOperation.Read"/>.
    /// </summary>
    public static ImmutableHashSet<string> ReadMethods { get; } =
        ImmutableHashSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "Find", "FindFirst", "FindLast", "FindSet",
            "Get", "GetBySystemId",
            "IsEmpty", "Count");

    /// <summary>
    /// Subset of <see cref="ReadMethods"/> that load a single record buffer.
    /// Excludes FindSet (used with repeat..until Next loops) and aggregate reads (IsEmpty, Count).
    /// </summary>
    public static ImmutableHashSet<string> SingleRecordReadMethods { get; } =
        ImmutableHashSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "Find", "FindFirst", "FindLast",
            "Get", "GetBySystemId");

    /// <summary>
    /// Methods that insert records into the database.
    /// </summary>
    public static ImmutableHashSet<string> InsertMethods { get; } =
        ImmutableHashSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "Insert");

    /// <summary>
    /// Methods that modify existing records in the database.
    /// </summary>
    public static ImmutableHashSet<string> ModifyMethods { get; } =
        ImmutableHashSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "Modify", "ModifyAll", "Rename");

    /// <summary>
    /// Methods that delete records from the database.
    /// </summary>
    public static ImmutableHashSet<string> DeleteMethods { get; } =
        ImmutableHashSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "Delete", "DeleteAll");

    /// <summary>
    /// All methods that mutate record data, including non-permission operations
    /// like TransferFields, Init, and Copy that affect the record buffer.
    /// </summary>
    public static ImmutableHashSet<string> WriteMethods { get; } =
        ImmutableHashSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "Insert", "Modify", "ModifyAll", "Delete", "DeleteAll",
            "Rename", "TransferFields", "Init", "Copy");

    /// <summary>
    /// Methods that configure partial record loading (SetLoadFields, AddLoadFields, SetBaseLoadFields).
    /// </summary>
    public static ImmutableHashSet<string> LoadFieldsMethods { get; } =
        ImmutableHashSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "SetLoadFields", "AddLoadFields", "SetBaseLoadFields");

    /// <summary>
    /// Subset of write methods that trigger JIT field loads when partial records are active.
    /// Excludes ModifyAll (set-based, no record load), DeleteAll (same), Init (no SQL).
    /// </summary>
    public static ImmutableHashSet<string> JitLoadWriteMethods { get; } =
        ImmutableHashSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "Insert", "Modify", "Delete", "Rename", "TransferFields", "Copy");

    /// <summary>
    /// Methods that can execute table triggers or field validation.
    /// </summary>
    public static ImmutableHashSet<string> TriggerMethods { get; } =
        ImmutableHashSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "Insert", "Modify", "Delete", "DeleteAll", "Validate", "ModifyAll");

    /// <summary>
    /// Methods that accept a RunTrigger boolean parameter.
    /// </summary>
    public static ImmutableHashSet<string> RunTriggerParameterMethods { get; } =
        ImmutableHashSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "Insert", "Modify", "ModifyAll", "Delete", "DeleteAll");
}
