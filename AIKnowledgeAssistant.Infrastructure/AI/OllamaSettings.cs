namespace AIKnowledgeAssistant.Infrastructure.AI;

/// <summary>
/// Configuration for talking to a local Ollama server.
/// Values are read from the "Ollama" section of appsettings.json (with the
/// defaults below used if that section is missing).
/// </summary>
public class OllamaSettings
{
    /// <summary>Base URL of the Ollama HTTP API. Default local install.</summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>Model used to create embeddings. 768-dimension, small, free.</summary>
    public string EmbeddingModel { get; set; } = "nomic-embed-text";

    /// <summary>Model used to generate chat answers (used in Step 8).</summary>
    public string ChatModel { get; set; } = "llama3.2";
}
