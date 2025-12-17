// This is a polyfill for the IsExternalInit class that is required for init-only setters
// and records in C# 9+ when targeting frameworks older than .NET 5 (e.g., netstandard2.1).

#if NETSTANDARD2_1
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif