using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Test;

/// <summary>
/// Test double simulating an alcops.json that exists but cannot be read:
/// <see cref="Exists"/> returns true and <see cref="OpenRead"/> throws.
/// Deterministic and cross-platform, unlike physical file locks (advisory-only on Linux).
/// <see cref="GetDirectoryPath"/> returns an empty string so the settings cache is bypassed.
/// </summary>
internal sealed class ThrowingFileSystem : IFileSystem
{
    public bool Exists(string path) => true;

    public Stream OpenRead(string path) => throw new IOException("Simulated read failure.");

    public string GetDirectoryPath() => string.Empty;

    public byte[] ReadBytes(string path) => throw new NotSupportedException();

    public byte[] ReadBytes(string path, int count) => throw new NotSupportedException();

    public void WriteBytes(string path, byte[] content) => throw new NotSupportedException();

    public bool DirectoryExistsForFile(string path) => throw new NotSupportedException();

    public IEnumerable<string> GetFiles(string searchPattern) => throw new NotSupportedException();

    public IEnumerable<string> GetFiles(string directory, string searchPattern) => throw new NotSupportedException();

    public IEnumerable<string> GetFilesRecursively(string directory) => throw new NotSupportedException();

    public void CreateDirectoryForFile(string path) => throw new NotSupportedException();

    public Stream CreateFile(string path) => throw new NotSupportedException();

    public Stream OpenWrite(string path) => throw new NotSupportedException();

    public Stream OpenFile(string filePath, FileMode mode, FileAccess access, FileShare share = FileShare.None, int bufferSize = 4096, FileOptions options = FileOptions.None) => throw new NotSupportedException();

    public void DeleteFile(string path) => throw new NotSupportedException();

    public long GetFileSize(string path) => throw new NotSupportedException();

    public string GetAbsolutePath(string relativePath) => throw new NotSupportedException();

    public bool DirectoryExists(string directory) => throw new NotSupportedException();
}
