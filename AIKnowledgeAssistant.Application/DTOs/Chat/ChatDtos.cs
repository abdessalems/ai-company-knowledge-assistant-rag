namespace AIKnowledgeAssistant.Application.DTOs.Chat;

/// <summary>What the client sends to ask a question.</summary>
public class AskRequest
{
    /// <summary>The natural-language question, e.g. "How many vacation days do I get?"</summary>
    public string Question { get; set; } = string.Empty;
}

/// <summary>A single citation returned with an answer.</summary>
public class SourceDto
{
    /// <summary>Document filename, e.g. "HR_Policy.pdf".</summary>
    public string Document { get; set; } = string.Empty;

    /// <summary>Page number within that document.</summary>
    public int Page { get; set; }

    /// <summary>How relevant this source was (0.0–1.0), highest first.</summary>
    public double Relevance { get; set; }
}

/// <summary>The answer plus the sources it was based on.</summary>
public class ChatResponse
{
    /// <summary>The AI-generated answer, grounded in the retrieved sources.</summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>Citations (document + page) the answer drew from.</summary>
    public List<SourceDto> Sources { get; set; } = new();
}
