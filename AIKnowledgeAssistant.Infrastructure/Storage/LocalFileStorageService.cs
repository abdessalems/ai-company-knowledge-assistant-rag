namespace AIKnowledgeAssistant.Infrastructure.Storage;

using AIKnowledgeAssistant.Application.Interfaces;

/// <summary>
/// Stores uploaded files on the local filesystem.
///
/// This is the DEVELOPMENT implementation of <see cref="IFileStorageService"/>.
/// In production you would add e.g. AzureBlobStorageService that implements the
/// same interface, and swap it in the DI container — no other code changes.
///
/// SECURITY PRINCIPLES APPLIED HERE:
/// 1. The stored filename is a fresh GUID, never the user's filename.
///    → prevents two users overwriting each other's "cv.pdf"
///    → prevents "../../etc/passwd" style path-traversal attacks, because we
///      never build a path from user-supplied text (only from the extension).
/// 2. Only the file extension is taken from the original name, and it is
///    sanitised to strip any directory separators.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    /// <summary>
    /// </summary>
    /// <param name="basePath">
    /// The root folder under which all uploaded files are stored,
    /// e.g. "C:\app\uploads". Supplied from configuration via DI.
    /// </param>
    public LocalFileStorageService(string basePath)
    {
        _basePath = basePath;

        // Ensure the storage folder exists. Creating it once at construction
        // avoids checking on every single upload. Safe to call repeatedly:
        // CreateDirectory does nothing if the folder already exists.
        Directory.CreateDirectory(_basePath);
    }

    /// <inheritdoc />
    public async Task<string> SaveAsync(
        Stream content,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        // Take ONLY the extension from the user's filename (e.g. ".pdf"),
        // and strip any path characters so it can't escape our folder.
        var extension = Path.GetExtension(originalFileName);
        var safeExtension = string.IsNullOrWhiteSpace(extension)
            ? string.Empty
            : Path.GetFileName(extension); // removes any directory parts

        // The actual stored name is a random GUID + the safe extension.
        var storedFileName = $"{Guid.NewGuid():N}{safeExtension}";
        var fullPath = Path.Combine(_basePath, storedFileName);

        // Open a file on disk and copy the uploaded bytes into it.
        // FileMode.CreateNew guarantees we never overwrite an existing file
        // (a GUID collision is astronomically unlikely, but we fail safe).
        await using var fileStream = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);

        await content.CopyToAsync(fileStream, cancellationToken);

        // Return the full path — this is what gets saved in Document.FilePath.
        return fullPath;
    }

    /// <inheritdoc />
    public Task DeleteAsync(
        string storedPath,
        CancellationToken cancellationToken = default)
    {
        // Idempotent: only delete if it still exists. Deleting an already-gone
        // file is treated as success, not an error (the desired end state —
        // "file no longer exists" — is already true).
        if (File.Exists(storedPath))
        {
            File.Delete(storedPath);
        }

        // No async work is needed for local disk delete, but we return a Task
        // to satisfy the interface (a cloud implementation WOULD be async).
        return Task.CompletedTask;
    }
}
