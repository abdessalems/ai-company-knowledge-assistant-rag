namespace AIKnowledgeAssistant.Application.Services;

using System.Text;
using AIKnowledgeAssistant.Application.DTOs.Chat;
using AIKnowledgeAssistant.Application.Interfaces;
using AIKnowledgeAssistant.Domain.Entities;

/// <summary>
/// The full RAG flow for answering a question from the user's documents.
/// </summary>
public class ChatService : IChatService
{
    private readonly IRetrievalService _retrieval;
    private readonly ILlmService _llm;
    private readonly IUnitOfWork _unitOfWork;

    // How many chunks to feed the model as context.
    private const int TopK = 4;

    // Instructions that keep the model grounded and honest — the heart of RAG:
    // answer ONLY from the provided context, otherwise admit it doesn't know.
    // This is what prevents "hallucinations".
    private const string SystemPrompt =
        "You are a company knowledge assistant. Answer the user's question using ONLY the " +
        "information in the provided context from company documents. If the answer is not " +
        "contained in the context, reply that you don't know based on the available documents. " +
        "Be concise and factual, and do not invent information.";

    public ChatService(
        IRetrievalService retrieval,
        ILlmService llm,
        IUnitOfWork unitOfWork)
    {
        _retrieval = retrieval;
        _llm = llm;
        _unitOfWork = unitOfWork;
    }

    public async Task<ChatResponse> AskAsync(
        Guid userId,
        string question,
        CancellationToken cancellationToken = default)
    {
        // 1. RETRIEVE the most relevant chunks from the user's own documents.
        var retrieved = await _retrieval.RetrieveAsync(userId, question, TopK, cancellationToken);

        // 2. GENERATE an answer — grounded in that context, or a safe fallback.
        string answer;
        if (retrieved.Count == 0)
        {
            answer = "I couldn't find any relevant information in your uploaded documents.";
        }
        else
        {
            var userPrompt = $"Context:\n{BuildContext(retrieved)}\nQuestion: {question}";
            answer = await _llm.CompleteAsync(SystemPrompt, userPrompt, cancellationToken);
        }

        // 3. PERSIST the conversation and its citations (chat history + audit trail).
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Question = question,
            Answer = answer,
            CreatedAt = DateTime.UtcNow,
            Sources = retrieved
                .Select(r => new MessageSource
                {
                    Id = Guid.NewGuid(),
                    DocumentChunkId = r.ChunkId,
                    DocumentName = r.DocumentName,
                    PageNumber = r.PageNumber,
                    RelevanceScore = Math.Clamp(r.Score, 0f, 1f),
                    CreatedAt = DateTime.UtcNow,
                })
                .ToList(),
        };

        await _unitOfWork.Conversations.AddAsync(conversation);
        await _unitOfWork.SaveChangesAsync();

        // 4. RETURN the answer with de-duplicated citations (one per document+page).
        return new ChatResponse
        {
            Answer = answer,
            Sources = retrieved
                .GroupBy(r => new { r.DocumentName, r.PageNumber })
                .Select(g => new SourceDto
                {
                    Document = g.Key.DocumentName,
                    Page = g.Key.PageNumber,
                    Relevance = Math.Round(Math.Clamp((double)g.Max(r => r.Score), 0, 1), 3),
                })
                .OrderByDescending(s => s.Relevance)
                .ToList(),
        };
    }

    /// <summary>
    /// Number each chunk and label it with its source so the model can ground
    /// its answer and we can trace citations.
    /// </summary>
    private static string BuildContext(IReadOnlyList<RetrievedChunk> chunks)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < chunks.Count; i++)
        {
            var c = chunks[i];
            sb.AppendLine($"[{i + 1}] (Source: {c.DocumentName}, page {c.PageNumber})");
            sb.AppendLine(c.Content);
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
