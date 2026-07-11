namespace AIKnowledgeAssistant.Application.Services;

using AIKnowledgeAssistant.Application.DTOs.Documents;
using AIKnowledgeAssistant.Application.Interfaces;
using AIKnowledgeAssistant.Domain.Entities;
using AIKnowledgeAssistant.Domain.Enums;
using AIKnowledgeAssistant.Domain.Exceptions;

/// <summary>
/// Coordinates document upload/list/delete across the file store and database.
///
/// DEPENDENCIES (all injected — Dependency Inversion):
/// - IUnitOfWork:         database access (Documents repository + SaveChanges)
/// - IFileStorageService: where the raw bytes live (local disk today)
///
/// It knows nothing about HTTP, IFormFile, PostgreSQL, or the local filesystem
/// directly — only about abstractions. That is what makes it unit-testable.
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;

    /// <summary>
    /// Maps a file extension to our supported FileType enum.
    /// Also acts as the allow-list: anything not in here is rejected.
    /// </summary>
    private static readonly Dictionary<string, FileType> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = FileType.Pdf,
        [".docx"] = FileType.Docx,
        [".doc"] = FileType.Doc,
        [".txt"] = FileType.Txt,
    };

    public DocumentService(IUnitOfWork unitOfWork, IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
    }

    /// <inheritdoc />
    public async Task<DocumentDto> UploadAsync(
        Guid userId,
        Stream content,
        string originalFileName,
        long fileSizeBytes,
        CancellationToken cancellationToken = default)
    {
        // ---- 1. VALIDATE the upload (business rules) ----------------------

        // 1a. File type must be one we support. This is an allow-list, so any
        //     unexpected extension (.exe, .zip, ...) is rejected by default.
        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(extension) ||
            !SupportedExtensions.TryGetValue(extension, out var fileType))
        {
            throw new InvalidBusinessRuleException(
                $"Unsupported file type '{extension}'. Allowed: PDF, DOCX, DOC, TXT.");
        }

        // 1b. Size rules come from the Document entity itself (single source of
        //     truth): not empty/too small, and not larger than 50 MB.
        var probe = new Document { FileSizeBytes = fileSizeBytes };
        if (!probe.HasContent())
            throw new InvalidBusinessRuleException("File is empty or too small (minimum 1 KB).");
        if (!probe.IsValidSize())
            throw new InvalidBusinessRuleException("File exceeds the maximum size of 50 MB.");

        // ---- 2. STORE the raw bytes --------------------------------------
        // Do this before the DB write so we have a real path to persist.
        var storedPath = await _fileStorage.SaveAsync(content, originalFileName, cancellationToken);

        // ---- 3. RECORD metadata in the database --------------------------
        try
        {
            var document = new Document
            {
                Id = Guid.NewGuid(),
                FileName = originalFileName,
                FilePath = storedPath,
                FileType = fileType,
                FileSizeBytes = fileSizeBytes,
                UploadedAt = DateTime.UtcNow,
                UserId = userId,
            };

            await _unitOfWork.Documents.AddAsync(document);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(document);
        }
        catch
        {
            // COMPENSATING ACTION: if the DB write fails, we must not leave an
            // orphaned file on disk with no record pointing to it. Delete it,
            // then rethrow so the caller/middleware reports the real error.
            await _fileStorage.DeleteAsync(storedPath, cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentDto>> GetUserDocumentsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var documents = await _unitOfWork.Documents.GetUserDocumentsAsync(userId);
        return documents.Select(ToDto);
    }

    /// <inheritdoc />
    public async Task<DocumentDto> GetByIdAsync(
        Guid userId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _unitOfWork.Documents.GetByIdAsync(documentId);

        // Treat "not found" and "not yours" identically → 404. Returning 403
        // would confirm the document exists, leaking information to attackers.
        if (document is null || document.UserId != userId)
            throw new EntityNotFoundException("Document", documentId);

        return ToDto(document);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        Guid userId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _unitOfWork.Documents.GetByIdAsync(documentId);

        if (document is null || document.UserId != userId)
            throw new EntityNotFoundException("Document", documentId);

        // Remove the database row first...
        _unitOfWork.Documents.Delete(document);
        await _unitOfWork.SaveChangesAsync();

        // ...then delete the physical file. Order matters: if file deletion
        // fails, the row is already gone and we haven't left a dangling record.
        // (Idempotent delete means a missing file is not an error.)
        await _fileStorage.DeleteAsync(document.FilePath, cancellationToken);
    }

    /// <summary>Map a Document entity to the public DTO (single place, no leaks).</summary>
    private static DocumentDto ToDto(Document document) => new()
    {
        Id = document.Id,
        FileName = document.FileName,
        FileType = document.FileType.ToString(),
        FileSizeBytes = document.FileSizeBytes,
        UploadedAt = document.UploadedAt,
    };
}
