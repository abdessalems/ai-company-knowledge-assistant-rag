namespace AIKnowledgeAssistant.Infrastructure.TextExtraction;

using AIKnowledgeAssistant.Application.Interfaces;
using AIKnowledgeAssistant.Domain.Enums;
using UglyToad.PdfPig;

/// <summary>
/// Extracts text from PDF files using PdfPig (pure C#, MIT licensed).
///
/// PdfPig gives us the pages of a PDF and the text on each one, which is exactly
/// what we need to keep page numbers for citations.
/// </summary>
public class PdfTextExtractor : ITextExtractor
{
    public bool CanHandle(FileType fileType) => fileType == FileType.Pdf;

    public async Task<IReadOnlyList<DocumentPage>> ExtractAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        // PdfPig reads from a byte[]. Copy the stream into memory first.
        // Our 50 MB upload limit keeps this safe to hold in RAM.
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        var pages = new List<DocumentPage>();

        using var pdf = PdfDocument.Open(buffer.ToArray());
        foreach (var page in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            // page.Number is 1-based (page 1, 2, 3...) — perfect for citations.
            pages.Add(new DocumentPage(page.Number, page.Text ?? string.Empty));
        }

        return pages;
    }
}
