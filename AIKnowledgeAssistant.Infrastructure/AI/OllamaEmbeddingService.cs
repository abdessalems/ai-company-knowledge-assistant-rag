namespace AIKnowledgeAssistant.Infrastructure.AI;

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AIKnowledgeAssistant.Application.Interfaces;

/// <summary>
/// <see cref="IEmbeddingService"/> backed by a local Ollama server (free, offline).
///
/// It POSTs to Ollama's /api/embed endpoint with a batch of texts and gets back
/// one vector per text. HttpClient is injected as a "typed client" (configured
/// in DI), which handles connection pooling and lifetime for us.
/// </summary>
public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly OllamaSettings _settings;

    public OllamaEmbeddingService(HttpClient http, OllamaSettings settings)
    {
        _http = http;
        _settings = settings;
    }

    public async Task<IReadOnlyList<float[]>> GenerateAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
            return Array.Empty<float[]>();

        var request = new EmbedRequest(_settings.EmbeddingModel, texts);

        using var response = await _http.PostAsJsonAsync("/api/embed", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbedResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Ollama returned an empty embedding response.");

        if (result.Embeddings is null || result.Embeddings.Length != texts.Count)
            throw new InvalidOperationException(
                $"Ollama returned {result.Embeddings?.Length ?? 0} embeddings for {texts.Count} inputs.");

        return result.Embeddings;
    }

    public async Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default)
    {
        var results = await GenerateAsync(new[] { text }, cancellationToken);
        return results[0];
    }

    // Request/response shapes for Ollama's /api/embed. JsonPropertyName maps our
    // C# names to the exact lowercase keys the Ollama API expects/returns.
    private record EmbedRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] IReadOnlyList<string> Input);

    private record EmbedResponse(
        [property: JsonPropertyName("embeddings")] float[][] Embeddings);
}
