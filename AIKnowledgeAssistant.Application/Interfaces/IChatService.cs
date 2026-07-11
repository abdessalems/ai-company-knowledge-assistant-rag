namespace AIKnowledgeAssistant.Application.Interfaces;

using AIKnowledgeAssistant.Application.DTOs.Chat;

/// <summary>
/// Orchestrates the full RAG question-answering flow:
/// retrieve relevant chunks → build grounded prompt → ask the LLM →
/// return the answer with citations → save the conversation.
/// </summary>
public interface IChatService
{
    Task<ChatResponse> AskAsync(
        Guid userId,
        string question,
        CancellationToken cancellationToken = default);
}
