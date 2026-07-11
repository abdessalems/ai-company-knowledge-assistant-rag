namespace AIKnowledgeAssistant.Application.Interfaces;

/// <summary>
/// Turns text into vector embeddings (arrays of numbers that capture meaning).
///
/// WHY AN INTERFACE (Dependency Inversion again):
/// The RAG pipeline just needs "text in → vector out". It must not care whether
/// the vectors come from a local Ollama model, OpenAI, or Azure. Swapping the
/// provider = registering a different implementation of this interface.
///
/// Two vectors that are numerically close mean the two texts are semantically
/// similar — that is what powers search-by-meaning.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generate an embedding for each input text, in the same order.
    /// Batching many texts in one call is far faster than one call each.
    /// </summary>
    Task<IReadOnlyList<float[]>> GenerateAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);

    /// <summary>Convenience overload for a single text (e.g. a user's question).</summary>
    Task<float[]> GenerateAsync(
        string text,
        CancellationToken cancellationToken = default);
}
