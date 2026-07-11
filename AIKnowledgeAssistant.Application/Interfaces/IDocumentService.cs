namespace AIKnowledgeAssistant.Application.Interfaces;

using AIKnowledgeAssistant.Application.DTOs.Documents;

/// <summary>
/// Application-layer orchestration for document management.
///
/// This is the "Service Layer" pattern: the controller stays thin and only
/// deals with HTTP; all the real work (validation, saving bytes, creating the
/// database row, enforcing ownership) lives here and is reusable/testable.
///
/// EVERY METHOD TAKES userId:
/// Documents are private to their owner. Passing the authenticated user's id
/// into every call lets the service enforce "you can only see/delete YOUR
/// documents" — a security rule that must live in the business layer, not be
/// left to the client to respect.
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Validate, store, and record a newly uploaded file.
    /// </summary>
    /// <param name="userId">The authenticated user uploading the file.</param>
    /// <param name="content">The raw file bytes.</param>
    /// <param name="originalFileName">The user's filename (for type + display).</param>
    /// <param name="fileSizeBytes">Size of the upload, for validation.</param>
    Task<DocumentDto> UploadAsync(
        Guid userId,
        Stream content,
        string originalFileName,
        long fileSizeBytes,
        CancellationToken cancellationToken = default);

    /// <summary>List all documents owned by the given user.</summary>
    Task<IEnumerable<DocumentDto>> GetUserDocumentsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a single document by id, but only if it belongs to the user.
    /// Throws EntityNotFoundException otherwise (we do NOT reveal that a
    /// document exists but belongs to someone else).
    /// </summary>
    Task<DocumentDto> GetByIdAsync(
        Guid userId,
        Guid documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a document (its database row AND its stored file), but only if
    /// it belongs to the user.
    /// </summary>
    Task DeleteAsync(
        Guid userId,
        Guid documentId,
        CancellationToken cancellationToken = default);
}
