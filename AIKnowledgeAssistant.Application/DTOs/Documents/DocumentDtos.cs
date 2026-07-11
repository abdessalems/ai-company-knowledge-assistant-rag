namespace AIKnowledgeAssistant.Application.DTOs.Documents;

/// <summary>
/// What the API returns to the client about a document.
///
/// WHY A DTO INSTEAD OF RETURNING THE Document ENTITY?
/// The Document entity carries things the client should never see or need:
///   - FilePath (an internal server path — a security leak if exposed)
///   - UserId, the User navigation property, the Chunks collection
/// A DTO exposes only the safe, relevant fields, and decouples our public
/// API contract from the internal database shape (we can refactor the entity
/// without breaking clients).
/// </summary>
public class DocumentDto
{
    /// <summary>Unique id of the document (used for GET-by-id and DELETE).</summary>
    public Guid Id { get; set; }

    /// <summary>Original filename the user uploaded, e.g. "HR_Policy.pdf".</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>File type as a readable string: "Pdf", "Docx", "Doc", "Txt".</summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>Size of the file in bytes (the client can format as KB/MB).</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>When the document was uploaded (UTC).</summary>
    public DateTime UploadedAt { get; set; }
}

/// <summary>
/// A single processed chunk of a document. Used to inspect the result of the
/// extract + chunk pipeline (and later, whether it has an embedding yet).
/// </summary>
public class DocumentChunkDto
{
    /// <summary>Unique id of the chunk.</summary>
    public Guid Id { get; set; }

    /// <summary>Page number this chunk came from (for citations).</summary>
    public int PageNumber { get; set; }

    /// <summary>Sequential index of this chunk within the document.</summary>
    public int ChunkIndex { get; set; }

    /// <summary>The chunk's text content.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Whether a vector embedding has been generated yet (Step 7).</summary>
    public bool HasEmbedding { get; set; }
}
