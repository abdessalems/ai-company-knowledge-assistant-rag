namespace AIKnowledgeAssistant.Domain.Entities;

/// <summary>
/// Represents a source citation for a conversation's answer.
/// This is the LINK between a Q&A and the document chunks used to answer it.
/// 
/// CRITICAL FOR RAG TRANSPARENCY!
/// 
/// USER STORY:
/// Employee asks: "What's the remote work policy?"
/// AI answers: "3 days in office, 2 days remote per week"
/// Employee questions: "How do I know this is accurate?"
/// Answer: MessageSource points to exact document + page
/// 
/// RELATIONSHIP DIAGRAM:
/// 
/// Conversation (1 Q&A exchange)
///     ├── MessageSource (1st citation)
///     │   └── DocumentChunk "Remote policy: 3/2 split"
///     ├── MessageSource (2nd citation)
///     │   └── DocumentChunk "Exceptions require manager approval"
///     └── MessageSource (3rd citation)
///         └── DocumentChunk "Remote work process policy"
/// 
/// WHY NOT JUST STORE DOCUMENT NAME?
/// ❌ Naive approach:
///    {
///       "question": "Remote policy?",
///       "answer": "3/2 split",
///       "source": "HR_Policy.pdf"
///    }
///    Problem: Which page in 50-page PDF?
/// 
/// ✅ Better approach (MessageSource):
///    {
///       "question": "Remote policy?",
///       "answer": "3/2 split",
///       "sources": [
///         {
///           "document": "HR_Policy.pdf",
///           "page": 7,
///           "chunk": DocumentChunk.Id
///         }
///       ]
///    }
///    Benefit: Precise, verifiable, traceable
/// 
/// COMPLIANCE VALUE:
/// - Financial institutions: Every statement must cite source
/// - Healthcare: Patient info must be traceable
/// - Legal: Document decisions trail
/// - General: Builds user trust ("I can verify this")
/// </summary>
public class MessageSource
{
    /// <summary>
    /// Unique identifier for this source citation.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The conversation this source belongs to.
    /// Foreign key to Conversation table.
    /// 
    /// QUERY EXAMPLE:
    /// var conversation = await _context.Conversations
    ///     .Include(c => c.Sources)
    ///     .FirstOrDefaultAsync(c => c.Id == conversationId);
    /// 
    /// Now conversation.Sources contains all citations
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Navigation property to Conversation.
    /// Set by Entity Framework.
    /// </summary>
    public Conversation? Conversation { get; set; }

    /// <summary>
    /// The document chunk used as source for this answer.
    /// Foreign key to DocumentChunk table.
    /// 
    /// RELATIONSHIP:
    /// DocumentChunk (one specific chunk) 
    ///     ← referenced by many MessageSources
    ///     (same chunk can be cited in multiple conversations)
    /// 
    /// EXAMPLE:
    /// DocumentChunk: "Vacation: 25 days per year"
    /// Cited in:
    /// 1. Conversation A: "How many days?"
    /// 2. Conversation B: "What's the holiday policy?"
    /// 3. Conversation C: "Can I take unpaid leave?"
    /// 
    /// WHY IMPORTANT:
    /// - Can analyze: "Which chunks are most cited?"
    /// - Insight: "This chunk answers multiple questions"
    /// - Quality: "If many cite it, it's likely accurate"
    /// </summary>
    public Guid DocumentChunkId { get; set; }

    /// <summary>
    /// Navigation property to DocumentChunk.
    /// Set by Entity Framework.
    /// 
    /// USAGE: source.DocumentChunk?.Content (get the actual chunk text)
    /// </summary>
    public DocumentChunk? DocumentChunk { get; set; }

    /// <summary>
    /// Document filename for display purposes.
    /// 
    /// EXAMPLE: "HR_Policy.pdf", "Employee_Handbook.docx"
    /// 
    /// WHY DUPLICATE IF WE HAVE DocumentChunk.Document.FileName?
    /// Denormalization for performance!
    /// 
    /// QUERY (with duplication):
    /// var source = await _context.MessageSources.FirstAsync(m => m.Id == sourceId);
    /// string fileName = source.DocumentName; // 1 DB hit
    /// 
    /// QUERY (without duplication):
    /// var source = await _context.MessageSources
    ///     .Include(m => m.DocumentChunk)
    ///         .ThenInclude(d => d.Document)
    ///     .FirstAsync(m => m.Id == sourceId);
    /// string fileName = source.DocumentChunk?.Document?.FileName; // N+1 query problem!
    /// 
    /// By storing DocumentName here:
    /// - No need to join multiple tables
    /// - Faster queries
    /// - Display works even if chunk is deleted
    /// 
    /// DATABASE DESIGN NOTE:
    /// This is called "denormalization" - storing redundant data
    /// Trade-off:
    /// - Pro: Query performance
    /// - Con: Data could get out of sync (need to manage carefully)
    /// 
    /// In Infrastructure layer, we ensure:
    /// - When creating MessageSource, copy DocumentName from Document
    /// - When document is renamed, update all MessageSources (if needed)
    /// </summary>
    public string DocumentName { get; set; } = string.Empty;

    /// <summary>
    /// Page number in the source document.
    /// 
    /// EXAMPLE: Page 4 of HR_Policy.pdf
    /// 
    /// FOR END USER:
    /// "For details, see HR_Policy.pdf page 4"
    /// "You can verify this at HR_Policy.pdf, page 4"
    /// 
    /// WHY ESSENTIAL?
    /// - 50-page PDF: "page 4" is much more useful than whole file
    /// - User can ctrl+F for specific section
    /// - Professional citation format
    /// - Legal requirements (some industries mandate page references)
    /// 
    /// EDGE CASES:
    /// - Multi-page chunk: Could store "pages 4-5"
    ///   (For v1, just store start page, simplify)
    /// - E-book/Web doc: Could be "Chapter 3, Section 2"
    ///   (For v1, stick with page numbers)
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Relevance score of this source to the question.
    /// 
    /// RANGE: 0.0 to 1.0
    /// - 1.0: Perfect match (highly relevant)
    /// - 0.8: Very relevant
    /// - 0.5: Somewhat relevant
    /// - 0.0: Not relevant at all
    /// 
    /// HOW IT'S CALCULATED:
    /// During retrieval, vector database returns similarity score
    /// 0.92 means: "This chunk is 92% similar to question embedding"
    /// 
    /// WHY STORE IT?
    /// - Sort sources by relevance (most relevant first)
    /// - Display confidence to user
    /// - Filter: "Only include sources with 0.8+ confidence"
    /// - Analytics: "Why did AI pick this source?"
    /// 
    /// EXAMPLE RESPONSE:
    /// "Most relevant sources:"
    /// 1. HR_Policy.pdf, page 4 - Relevance: 0.95 ⭐⭐⭐⭐⭐
    /// 2. HR_Policy.pdf, page 5 - Relevance: 0.87 ⭐⭐⭐⭐
    /// 3. FAQ.pdf, page 2 - Relevance: 0.72 ⭐⭐⭐
    /// </summary>
    public float RelevanceScore { get; set; }

    /// <summary>
    /// When this source was cited.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Business rule: Validate relevance score is in valid range.
    /// 
    /// VALIDATION:
    /// - Score must be between 0.0 and 1.0
    /// - Outside this range indicates calculation error
    /// 
    /// EXAMPLE USAGE IN APPLICATION LAYER:
    /// if (!source.IsValidRelevanceScore())
    ///     throw new InvalidBusinessRuleException("Invalid relevance score");
    /// </summary>
    public bool IsValidRelevanceScore() => RelevanceScore >= 0f && RelevanceScore <= 1f;

    /// <summary>
    /// Business rule: Check if source is relevant enough.
    /// 
    /// THRESHOLD: 0.5 (50% similarity)
    /// 
    /// RATIONALE:
    /// - Below 50%: Probably not related to question
    /// - Above 50%: Likely related
    /// - Above 80%: Highly relevant
    /// 
    /// EXAMPLE USAGE:
    /// var relevantSources = conversation.Sources
    ///     .Where(s => s.IsRelevant())
    ///     .OrderByDescending(s => s.RelevanceScore);
    /// 
    /// Context only sent if RELEVANT sources found:
    /// if (relevantSources.Count() == 0)
    ///     answer = "No relevant documents found";
    /// </summary>
    public bool IsRelevant() => RelevanceScore >= 0.5f;

    /// <summary>
    /// Get display text for UI.
    /// 
    /// EXAMPLE OUTPUT:
    /// "HR_Policy.pdf - Page 4 (Relevance: 95%)"
    /// </summary>
    public string GetDisplayText() 
        => $"{DocumentName} - Page {PageNumber} (Relevance: {Math.Round(RelevanceScore * 100)}%)";
}
