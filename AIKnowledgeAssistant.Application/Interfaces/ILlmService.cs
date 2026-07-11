namespace AIKnowledgeAssistant.Application.Interfaces;

/// <summary>
/// Generates text with a Large Language Model (LLM).
///
/// Kept deliberately generic (a system prompt + a user prompt → text) so the
/// provider is swappable: local Ollama today, OpenAI/Azure later. The RAG-specific
/// logic (building the context, the citation rules) lives in the chat service,
/// NOT here — this interface only knows "prompt in → completion out".
/// </summary>
public interface ILlmService
{
    /// <param name="systemPrompt">Instructions that set the assistant's behaviour.</param>
    /// <param name="userPrompt">The user's message (here: context + question).</param>
    Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
