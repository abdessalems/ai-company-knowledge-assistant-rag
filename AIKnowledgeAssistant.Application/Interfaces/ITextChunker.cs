namespace AIKnowledgeAssistant.Application.Interfaces;

/// <summary>
/// One chunk of text ready to become a DocumentChunk row.
/// Carries which page it came from (for citations) and its sequence number.
/// </summary>
public record TextChunk(int PageNumber, int ChunkIndex, string Content);

/// <summary>
/// Splits extracted page text into overlapping chunks suitable for embedding.
///
/// WHY CHUNK AT ALL?
/// Embedding models and LLMs have size limits, and a whole 50-page PDF is far
/// too much context. We split into ~1000-character pieces so that later we can
/// find and send only the few most relevant pieces to the AI.
///
/// WHY OVERLAP BETWEEN CHUNKS?
/// A key sentence might fall exactly on a chunk boundary. By repeating the last
/// ~200 characters of one chunk at the start of the next, we make sure no idea
/// is cut in half and lost to search.
/// </summary>
public interface ITextChunker
{
    IReadOnlyList<TextChunk> Chunk(IReadOnlyList<DocumentPage> pages);
}
