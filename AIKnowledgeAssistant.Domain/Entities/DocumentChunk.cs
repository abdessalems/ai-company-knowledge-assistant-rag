namespace AIKnowledgeAssistant.Domain.Entities;

/// <summary>
/// Represents a processed chunk of a document with vector embedding.
/// 
/// THIS IS THE CORE OF OUR RAG SYSTEM!
/// 
/// DOMAIN RESPONSIBILITY:
/// - Store document text chunks
/// - Store vector embeddings for semantic search
/// - Track where chunk came from (page number)
/// - Enable similarity search
/// 
/// WHY CHUNKS INSTEAD OF WHOLE DOCUMENTS?
/// Problem: Document is 50 pages, question is "What's the vacation policy?"
/// ❌ Bad approach: Send entire document to LLM (too much context, slow, expensive)
/// ✅ Good approach: Find page 3 that discusses vacation, send only that
/// 
/// PROCESS:
/// 1. Upload "HR_Policy.pdf" (50 pages)
/// 2. Extract all text
/// 3. Split into chunks: "Vacation policy: 25 days per year" (chunk 1)
///                       "Sick leave policy: 10 days per year" (chunk 2)
///                       ... more chunks ...
/// 4. Generate embedding for each chunk (vector with ~1536 dimensions)
/// 5. Store embeddings in pgvector (PostgreSQL vector extension)
/// 6. When user asks: Generate question embedding
/// 7. Search: Find most similar chunks using cosine similarity
/// 8. Get top 3-5 chunks as context
/// 9. Send context + question to LLM
/// 10. LLM generates answer citing specific chunks
/// 
/// ARCHITECTURAL NOTE:
/// - This entity bridges document storage and semantic search
/// - Embedding vector is stored here
/// - Infrastructure layer handles pgvector operations
/// - Application layer coordinates chunk creation
/// 
/// TECHNICAL DETAIL - VECTOR EMBEDDINGS:
/// What's an embedding?
/// - Text converted to array of ~1536 numbers
/// - Similar text → similar numbers (mathematically close)
/// - "What's vacation?" and "How many days off?" → similar embeddings
/// - Can compute similarity using dot product or cosine similarity
/// 
/// Why this matters:
/// - Keyword search: "vacation" only matches exact word
/// - Semantic search: "vacation", "days off", "time off" all match
/// - This is why RAG is powerful!
/// </summary>
public class DocumentChunk
{
    /// <summary>
    /// Unique identifier for this chunk.
    /// GUID ensures uniqueness even with duplicate content.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The ID of the document this chunk came from.
    /// Foreign key to Document table.
    /// 
    /// REFERENTIAL INTEGRITY:
    /// - Every chunk must belong to exactly one document
    /// - If document is deleted, delete all its chunks
    /// - Query: "Get all chunks for document X"
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Navigation property to the parent Document.
    /// Set by Entity Framework.
    /// 
    /// USAGE: chunk.Document?.FileName (get the document name)
    /// </summary>
    public Document? Document { get; set; }

    /// <summary>
    /// The actual text content of this chunk.
    /// 
    /// EXAMPLE FOR HR_POLICY.PDF:
    /// "Employees receive 25 vacation days per year. 
    ///  Additional days may be negotiated for executives."
    /// 
    /// CHUNKING STRATEGY (implemented in Infrastructure layer):
    /// - Token-based: Split every 512 tokens (~2000 characters)
    /// - Semantic: Split at sentence/paragraph boundaries
    /// - Sliding window: Overlap between chunks for context
    /// 
    /// SIZE CONSIDERATIONS:
    /// - Too small: Lose context (e.g., "25 days" without "vacation")
    /// - Too large: Include noise (other unrelated info)
    /// - Optimal: 300-500 tokens (~1000-2000 characters)
    /// 
    /// WHY NOT STORE ENTIRE TEXT IN VECTORDB?
    /// - Vector databases optimize for search, not storage
    /// - PostgreSQL + pgvector is better for both
    /// - Keep text in text field, embeddings in vector field
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The page number in the original document where this chunk appears.
    /// 
    /// AUDIT TRAIL FOR CITATIONS:
    /// When user asks "What's the vacation policy?":
    /// AI Answer: "25 days per year"
    /// Citation: "HR_Policy.pdf, page 3"
    /// 
    /// This PageNumber field enables the citation!
    /// 
    /// EDGE CASES:
    /// - Page 0: Metadata or cover page
    /// - For Word docs: Could be section number instead
    /// - For merged PDFs: Could be "document 2, page 5"
    /// 
    /// WHY ESSENTIAL FOR RAG?
    /// Users need to verify information in original source
    /// Otherwise: "How do I know your answer is correct?"
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Sequential index of this chunk within its document.
    /// 
    /// EXAMPLE FOR 10-PAGE DOCUMENT:
    /// Chunk 1: Pages 1-2, ChunkIndex = 1
    /// Chunk 2: Pages 2-3, ChunkIndex = 2 (overlapping for context)
    /// Chunk 3: Pages 3-4, ChunkIndex = 3
    /// ... and so on
    /// 
    /// WHY TRACK THIS?
    /// - Helps with reordering chunks by relevance
    /// - Debugging document processing
    /// - Preventing duplicate chunks
    /// - Efficient deletion (delete all chunks where DocumentId = X)
    /// 
    /// BUSINESS RULE:
    /// ChunkIndex should be sequential (1, 2, 3, 4...)
    /// if chunks are sequential (no gaps)
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Vector embedding of the chunk content.
    /// 
    /// TECHNICAL DETAILS:
    /// - Generated by embedding model (OpenAI, Azure, Ollama)
    /// - ~1536 dimensions (numbers in vector array)
    /// - Stored as pgvector in PostgreSQL
    /// - Used for semantic similarity search
    /// 
    /// EXAMPLE IN PSEUDOCODE:
    /// // User asks: "How many vacation days?"
    /// var questionEmbedding = await _embeddingService.CreateAsync(question);
    /// 
    /// // Find similar chunks
    /// var similarChunks = await _vectorDb.FindSimilar(questionEmbedding, topK: 5);
    /// // Results might include chunks about:
    /// // - Vacation days (very similar)
    /// // - Days off policies (similar)
    /// // - Working hours (not similar)
    /// 
    /// COST CONSIDERATION:
    /// - Creating embeddings costs money (if using OpenAI)
    /// - 1536-dim OpenAI embedding ≈ $0.02 per 1M tokens
    /// - 10,000 chunks ≈ $0.20 one-time cost
    /// - Queries don't cost extra (only retrieval)
    /// 
    /// WHY WE CACHE THIS:
    /// - Never recreate embedding for same content
    /// - Store once, use forever
    /// - If document changes, delete old chunks and create new ones
    /// </summary>
    public float[]? VectorEmbedding { get; set; }

    /// <summary>
    /// Timestamp when this chunk was created.
    /// 
    /// AUDIT PURPOSES:
    /// - When was document processed?
    /// - How old is this indexed content?
    /// - Identify stale chunks (if document updated)
    /// 
    /// AGING OUT EMBEDDINGS:
    /// - If document hasn't been updated in 1 year
    /// - Could re-embed with newer model for quality improvement
    /// - Track: "Last updated", "Last queried"
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Business rule: Validate chunk has meaningful content.
    /// 
    /// WHY CHECK THIS?
    /// - Skip empty chunks
    /// - Skip chunks with only whitespace
    /// - Avoid creating embeddings for garbage data
    /// - Saves on embedding API costs
    /// 
    /// EXAMPLE USAGE IN APPLICATION LAYER:
    /// if (!chunk.HasContent())
    ///     throw new InvalidBusinessRuleException("Chunk is empty");
    /// 
    /// MINIMUM THRESHOLD:
    /// 10 characters ensures some real text
    /// (not just spaces or newlines)
    /// </summary>
    public bool HasContent() => !string.IsNullOrWhiteSpace(Content) && Content.Length > 10;

    /// <summary>
    /// Business rule: Check if chunk has embedding.
    /// 
    /// WHY IMPORTANT FOR RAG?
    /// - Can't search without embedding
    /// - If embedding is null, can't use in retrieval
    /// - Might be pending in async background job
    /// 
    /// WORKFLOW:
    /// 1. Create chunk (Content filled, Embedding null)
    /// 2. Queue background job to generate embedding
    /// 3. Background job completes, updates chunk
    /// 4. Now chunk.HasEmbedding() == true
    /// 5. Can now retrieve in searches
    /// </summary>
    public bool HasEmbedding() => VectorEmbedding != null && VectorEmbedding.Length > 0;
}
