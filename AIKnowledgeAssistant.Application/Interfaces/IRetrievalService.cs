namespace AIKnowledgeAssistant.Application.Interfaces;

/// <summary>
/// A chunk found to be relevant to a question, with its similarity score and the
/// info needed to cite it (document name + page).
/// </summary>
public record RetrievedChunk(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentName,
    int PageNumber,
    string Content,
    float Score);

/// <summary>
/// The "R" in RAG: given a question, find the most relevant chunks from the
/// user's OWN documents (semantic search).
///
/// Current implementation embeds the question and ranks the user's chunks by
/// cosine similarity in C#. The production upgrade is pgvector doing this in SQL —
/// swapping this implementation is the only change that would require.
/// </summary>
public interface IRetrievalService
{
    Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(
        Guid userId,
        string question,
        int topK,
        CancellationToken cancellationToken = default);
}
