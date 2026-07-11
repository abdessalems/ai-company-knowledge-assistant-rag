namespace AIKnowledgeAssistant.Infrastructure.TextExtraction;

using AIKnowledgeAssistant.Application.Interfaces;
using AIKnowledgeAssistant.Domain.Enums;

/// <summary>
/// Extracts text from plain .txt files.
///
/// A text file has no pages, so we treat the whole file as a single "page 1".
/// The chunker will still split it into multiple chunks by size.
/// </summary>
public class PlainTextExtractor : ITextExtractor
{
    public bool CanHandle(FileType fileType) => fileType == FileType.Txt;

    public async Task<IReadOnlyList<DocumentPage>> ExtractAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        // leaveOpen: true so the caller stays in control of disposing the stream.
        using var reader = new StreamReader(content, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);

        return new List<DocumentPage> { new DocumentPage(1, text) };
    }
}
