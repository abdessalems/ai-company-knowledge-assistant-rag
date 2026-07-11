namespace AIKnowledgeAssistant.Application.Services;

using System.Text.RegularExpressions;
using AIKnowledgeAssistant.Application.Interfaces;

/// <summary>
/// Splits page text into fixed-size chunks with overlap ("sliding window").
///
/// Each chunk belongs to exactly one page, so its page number stays accurate for
/// citations. Chunk indexes are sequential across the whole document.
/// </summary>
public class SlidingWindowTextChunker : ITextChunker
{
    // ~1000 chars ≈ 200-300 tokens: big enough to hold an idea, small enough to
    // stay focused. Overlap carries context across boundaries.
    private const int MaxChunkChars = 1000;
    private const int OverlapChars = 200;

    public IReadOnlyList<TextChunk> Chunk(IReadOnlyList<DocumentPage> pages)
    {
        var chunks = new List<TextChunk>();
        var chunkIndex = 0;

        foreach (var page in pages)
        {
            var text = NormalizeWhitespace(page.Text);
            if (string.IsNullOrWhiteSpace(text))
                continue; // skip blank pages

            foreach (var piece in SplitWithOverlap(text))
            {
                chunks.Add(new TextChunk(page.PageNumber, chunkIndex++, piece));
            }
        }

        return chunks;
    }

    /// <summary>Collapse all runs of whitespace/newlines into single spaces.</summary>
    private static string NormalizeWhitespace(string text) =>
        Regex.Replace(text, @"\s+", " ").Trim();

    /// <summary>
    /// Yield successive substrings of up to MaxChunkChars, each starting
    /// OverlapChars before the previous one ended, breaking on spaces where
    /// possible so we don't cut words in half.
    /// </summary>
    private static IEnumerable<string> SplitWithOverlap(string text)
    {
        var start = 0;
        while (start < text.Length)
        {
            var end = Math.Min(start + MaxChunkChars, text.Length);

            // If we're not at the very end, back up to the last space inside the
            // window so the chunk ends on a whole word.
            if (end < text.Length)
            {
                var lastSpace = text.LastIndexOf(' ', end - 1, end - start);
                if (lastSpace > start)
                    end = lastSpace;
            }

            var piece = text[start..end].Trim();
            if (piece.Length > 0)
                yield return piece;

            if (end >= text.Length)
                yield break;

            // Step forward, but keep an overlap. Math.Max guarantees progress
            // even if a chunk was short, so we can never loop forever.
            start = Math.Max(end - OverlapChars, start + 1);
        }
    }
}
