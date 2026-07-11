namespace AIKnowledgeAssistant.Infrastructure.AI;

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AIKnowledgeAssistant.Application.Interfaces;

/// <summary>
/// <see cref="ILlmService"/> backed by a local Ollama chat model (free, offline).
///
/// Sends a system + user message to Ollama's /api/chat endpoint and returns the
/// assistant's reply text.
/// </summary>
public class OllamaChatService : ILlmService
{
    private readonly HttpClient _http;
    private readonly OllamaSettings _settings;

    public OllamaChatService(HttpClient http, OllamaSettings settings)
    {
        _http = http;
        _settings = settings;
    }

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var request = new ChatRequest(
            _settings.ChatModel,
            new[]
            {
                new ChatMessage("system", systemPrompt),
                new ChatMessage("user", userPrompt),
            },
            Stream: false);

        using var response = await _http.PostAsJsonAsync("/api/chat", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChatResponseBody>(cancellationToken)
            ?? throw new InvalidOperationException("Ollama returned an empty chat response.");

        return result.Message?.Content?.Trim() ?? string.Empty;
    }

    // Shapes for Ollama's /api/chat request and response.
    private record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private record ChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("stream")] bool Stream);

    private record ChatResponseBody(
        [property: JsonPropertyName("message")] ChatMessage? Message);
}
