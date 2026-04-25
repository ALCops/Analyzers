using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.ApplicationCop.Permissions;

/// <summary>
/// Represents a required database permission detected from code analysis.
/// </summary>
#if NETSTANDARD2_1
internal readonly struct RequiredPermission
{
    public ITableTypeSymbol Table { get; }
    public ITypeSymbol VariableType { get; }
    public DatabaseOperation Operation { get; }
    public Microsoft.Dynamics.Nav.CodeAnalysis.Text.Location Location { get; }

    public RequiredPermission(
        ITableTypeSymbol table,
        ITypeSymbol variableType,
        DatabaseOperation operation,
        Microsoft.Dynamics.Nav.CodeAnalysis.Text.Location location)
    {
        Table = table;
        VariableType = variableType;
        Operation = operation;
        Location = location;
    }
}
#else
internal readonly record struct RequiredPermission(
    ITableTypeSymbol Table,
    ITypeSymbol VariableType,
    DatabaseOperation Operation,
    Microsoft.Dynamics.Nav.CodeAnalysis.Text.Location Location);
#endif
