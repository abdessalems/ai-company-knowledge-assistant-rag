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
    private readonly ITextExtractionService _textExtraction;
    private readonly ITextChunker _textChunker;

    /// <summary>
    /// Maps a file extension to our supported FileType enum.
    /// Also acts as the allow-list: anything not in here is rejected.
    ///
    /// NOTE: currently only PDF and TXT, because those are the file types we can
    /// fully extract text from (Step 6). Word (.docx/.doc) will be re-added once
    /// its extractor exists — we do not accept files we cannot yet process.
    /// </summary>
    private static readonly Dictionary<string, FileType> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = FileType.Pdf,
        [".txt"] = FileType.Txt,
    };

    public DocumentService(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorage,
        ITextExtractionService textExtraction,
        ITextChunker textChunker)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _textExtraction = textExtraction;
        _textChunker = textChunker;
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
                $"Unsupported file type '{extension}'. Allowed: PDF, TXT.");
        }

        // 1b. Size rules come from the Document entity itself (single source of
        //     truth): not empty/too small, and not larger than 50 MB.
        var probe = new Document { FileSizeBytes = fileSizeBytes };
        if (!probe.HasContent())
            throw new InvalidBusinessRuleException("File is empty or too small (minimum 1 KB).");
        if (!probe.IsValidSize())
            throw new InvalidBusinessRuleException("File exceeds the maximum size of 50 MB.");

        // ---- 2. STORE the raw bytes --------------------------------------
        // Do this before processing so we have a real path to persist and can
        // re-read the file cleanly for extraction.
        var storedPath = await _fileStorage.SaveAsync(content, originalFileName, cancellationToken);

        try
        {
            // ---- 3. EXTRACT + CHUNK (the RAG pipeline) -------------------
            // Re-open the stored file and pull its text out page by page,
            // then split those pages into overlapping chunks.
            List<DocumentChunk> chunks;
            await using (var readStream = await _fileStorage.OpenReadAsync(storedPath, cancellationToken))
            {
                var pages = await _textExtraction.ExtractAsync(readStream, fileType, cancellationToken);
                var textChunks = _textChunker.Chunk(pages);

                if (textChunks.Count == 0)
                    throw new InvalidBusinessRuleException(
                        "No readable text was found in the document (it may be a scanned image).");

                chunks = textChunks
                    .Select(c => new DocumentChunk
                    {
                        Id = Guid.NewGuid(),
                        Content = c.Content,
                        PageNumber = c.PageNumber,
                        ChunkIndex = c.ChunkIndex,
                        VectorEmbedding = null, // filled in Step 7 (embeddings)
                        CreatedAt = DateTime.UtcNow,
                    })
                    .ToList();
            }

            // ---- 4. RECORD document + its chunks in ONE transaction ------
            var document = new Document
            {
                Id = Guid.NewGuid(),
                FileName = originalFileName,
                FilePath = storedPath,
                FileType = fileType,
                FileSizeBytes = fileSizeBytes,
                UploadedAt = DateTime.UtcNow,
                UserId = userId,
                Chunks = chunks, // EF inserts the chunks together with the document
            };

            await _unitOfWork.Documents.AddAsync(document);
            await _unitOfWork.SaveChangesAsync();

            return ToDto(document);
        }
        catch
        {
            // COMPENSATING ACTION: if extraction or the DB write fails, we must
            // not leave an orphaned file on disk. Delete it, then rethrow so the
            // middleware reports the real error.
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

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentChunkDto>> GetChunksAsync(
        Guid userId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        // GetWithChunksAsync eager-loads the Chunks collection in one query.
        var document = await _unitOfWork.Documents.GetWithChunksAsync(documentId);

        if (document is null || document.UserId != userId)
            throw new EntityNotFoundException("Document", documentId);

        return document.Chunks
            .OrderBy(c => c.ChunkIndex)
            .Select(c => new DocumentChunkDto
            {
                Id = c.Id,
                PageNumber = c.PageNumber,
                ChunkIndex = c.ChunkIndex,
                Content = c.Content,
                HasEmbedding = c.HasEmbedding(),
            });
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
