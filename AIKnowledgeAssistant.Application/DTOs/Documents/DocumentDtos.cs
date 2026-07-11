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
