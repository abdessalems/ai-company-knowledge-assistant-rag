namespace AIKnowledgeAssistant.Application.Interfaces;

using AIKnowledgeAssistant.Domain.Enums;

/// <summary>
/// One page of text pulled out of a document.
/// A "record" is a concise immutable data holder — perfect for carrying values
/// between steps of the pipeline without writing boilerplate get/set properties.
/// </summary>
public record DocumentPage(int PageNumber, string Text);

/// <summary>
/// Strategy interface: knows how to extract text from ONE kind of file.
///
/// STRATEGY PATTERN (Open/Closed Principle):
/// We have one implementation per file type (PdfTextExtractor, PlainTextExtractor,
/// and later WordTextExtractor). Adding a new supported type means adding a new
/// class — never editing existing ones. The rest of the app just asks the
/// <see cref="ITextExtractionService"/> facade and never picks a strategy itself.
/// </summary>
public interface ITextExtractor
{
    /// <summary>Can this extractor handle the given file type?</summary>
    bool CanHandle(FileType fileType);

    /// <summary>Extract the document's text, one entry per page.</summary>
    Task<IReadOnlyList<DocumentPage>> ExtractAsync(
        Stream content,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Facade that hides the strategy selection: give it a file + type, it finds the
/// right <see cref="ITextExtractor"/> and runs it. This keeps callers (like
/// DocumentService) from knowing which concrete extractor exists.
/// </summary>
public interface ITextExtractionService
{
    Task<IReadOnlyList<DocumentPage>> ExtractAsync(
        Stream content,
        FileType fileType,
        CancellationToken cancellationToken = default);
}
