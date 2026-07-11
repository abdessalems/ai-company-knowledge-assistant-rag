namespace AIKnowledgeAssistant.Application.Services;

using AIKnowledgeAssistant.Application.Interfaces;

/// <summary>
/// Finds the most relevant chunks for a question using cosine similarity.
///
/// Steps:
/// 1. Turn the question into an embedding (same model as the chunks).
/// 2. Load the user's embedded chunks.
/// 3. Score each chunk by cosine similarity to the question.
/// 4. Return the top K, highest score first.
/// </summary>
public class RetrievalService : IRetrievalService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IUnitOfWork _unitOfWork;

    public RetrievalService(IEmbeddingService embeddingService, IUnitOfWork unitOfWork)
    {
        _embeddingService = embeddingService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(
        Guid userId,
        string question,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var chunks = await _unitOfWork.DocumentChunks.GetEmbeddedChunksForUserAsync(userId);
        if (chunks.Count == 0)
            return Array.Empty<RetrievedChunk>(); // user has no searchable documents

        // Embed the question with the SAME model that produced the chunk vectors.
        var queryEmbedding = await _embeddingService.GenerateAsync(question, cancellationToken);

        return chunks
            .Where(c => c.VectorEmbedding is { Length: > 0 })
            .Select(c => new
            {
                Chunk = c,
                Score = CosineSimilarity(queryEmbedding, c.VectorEmbedding!)
            })
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => new RetrievedChunk(
                x.Chunk.Id,
                x.Chunk.DocumentId,
                x.Chunk.Document?.FileName ?? "(unknown)",
                x.Chunk.PageNumber,
                x.Chunk.Content,
                x.Score))
            .ToList();
    }

    /// <summary>
    /// Cosine similarity = how aligned two vectors are (angle between them).
    /// 1 = same direction (very similar), 0 = unrelated, -1 = opposite.
    /// Formula: dot(a,b) / (|a| * |b|).
    /// </summary>
    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            return 0f; // different embedding models → not comparable

        double dot = 0, magA = 0, magB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        var denominator = Math.Sqrt(magA) * Math.Sqrt(magB);
        return denominator == 0 ? 0f : (float)(dot / denominator);
    }
}
