namespace AIKnowledgeAssistant.Domain.Enums;

/// <summary>
/// Supported file types for document uploads.
/// 
/// WHY WE DEFINE THIS:
/// - Only these file types can be uploaded
/// - We validate file type before processing
/// - Different extraction logic per file type
/// 
/// FUTURE EXPANSION:
/// When we add new file types, we:
/// 1. Add enum value here
/// 2. Add extraction logic in Infrastructure/AI layer
/// 3. Application layer automatically supports it
/// 
/// This is the power of Clean Architecture!
/// </summary>
public enum FileType
{
    /// <summary>
    /// PDF documents - extracted using iTextSharp or similar
    /// </summary>
    Pdf = 0,

    /// <summary>
    /// Microsoft Word documents - extracted using Open XML SDK
    /// </summary>
    Docx = 1,

    /// <summary>
    /// Microsoft Word legacy format - extracted using Open XML SDK
    /// </summary>
    Doc = 2,

    /// <summary>
    /// Plain text files - direct reading
    /// </summary>
    Txt = 3
}
