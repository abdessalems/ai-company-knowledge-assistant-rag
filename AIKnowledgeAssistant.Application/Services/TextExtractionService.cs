namespace AIKnowledgeAssistant.Application.Services;

using AIKnowledgeAssistant.Application.Interfaces;
using AIKnowledgeAssistant.Domain.Enums;
using AIKnowledgeAssistant.Domain.Exceptions;

/// <summary>
/// Picks the right <see cref="ITextExtractor"/> for a file type and runs it.
///
/// It receives ALL registered extractors via constructor injection
/// (IEnumerable&lt;ITextExtractor&gt;) — the DI container hands us every class
/// that implements the interface — and simply asks each one "can you handle
/// this type?" until one says yes. Adding a new file type never touches this class.
/// </summary>
public class TextExtractionService : ITextExtractionService
{
    private readonly IEnumerable<ITextExtractor> _extractors;

    public TextExtractionService(IEnumerable<ITextExtractor> extractors)
    {
        _extractors = extractors;
    }

    public Task<IReadOnlyList<DocumentPage>> ExtractAsync(
        Stream content,
        FileType fileType,
        CancellationToken cancellationToken = default)
    {
        var extractor = _extractors.FirstOrDefault(e => e.CanHandle(fileType))
            ?? throw new InvalidBusinessRuleException(
                $"No text extractor is available for file type '{fileType}'.");

        return extractor.ExtractAsync(content, cancellationToken);
    }
}
