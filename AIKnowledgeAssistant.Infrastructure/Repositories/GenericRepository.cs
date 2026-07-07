namespace AIKnowledgeAssistant.Infrastructure.Repositories;

using AIKnowledgeAssistant.Application.Interfaces;
using AIKnowledgeAssistant.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using AIKnowledgeAssistant.Infrastructure.Database;

/// <summary>
/// Generic repository base class for common CRUD operations.
/// 
/// WHY A BASE CLASS?
/// - Don't repeat CRUD code for every entity type
/// - All repositories inherit from GenericRepository<T>
/// - Specialized repositories only override/add custom methods
/// 
/// INHERITANCE:
/// public class UserRepository : GenericRepository<User>, IUserRepository
/// public class DocumentRepository : GenericRepository<Document>, IDocumentRepository
/// 
/// BENEFITS:
/// - DRY: Don't Repeat Yourself
/// - Consistent: Same implementation for all entities
/// - Testable: Generic tests work for all repositories
/// - Maintainable: Fix bugs once, all repositories benefit
/// </summary>
public class GenericRepository<T> : IRepository<T> where T : class
{
    /// <summary>
    /// The DbContext instance.
    /// 
    /// RESPONSIBILITIES:
    /// - Connection to database
    /// - Change tracking
    /// - Query execution
    /// 
    /// Protected: Only subclasses can access
    /// </summary>
    protected readonly ApplicationDbContext _context;

    /// <summary>
    /// DbSet for the entity type T.
    /// 
    /// EXAMPLE:
    /// If T = User, then _dbSet = context.Users
    /// If T = Document, then _dbSet = context.Documents
    /// 
    /// Generic repository works for any entity!
    /// </summary>
    protected readonly DbSet<T> _dbSet;

    /// <summary>
    /// Constructor with dependency injection.
    /// 
    /// DI PRINCIPLE:
    /// Don't create DbContext, have it injected
    /// - Easier to test (inject test DbContext)
    /// - Easier to swap implementations
    /// - Cleaner code
    /// </summary>
    public GenericRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    /// <summary>
    /// Get all entities.
    /// 
    /// LINQ TO EF CORE:
    /// _dbSet.AsQueryable() → IQueryable<T>
    /// This creates a SQL query (not yet executed)
    /// 
    /// ToListAsync() → Executes query, returns List<T>
    /// 
    /// Generated SQL:
    /// SELECT * FROM TableName;
    /// </summary>
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    /// <summary>
    /// Get by primary key (ID).
    /// 
    /// PERFORMANCE:
    /// - Uses index on Id column
    /// - O(1) lookup
    /// - Very fast
    /// 
    /// RETURN VALUE:
    /// - Entity if found
    /// - null if not found
    /// 
    /// Generated SQL:
    /// SELECT * FROM TableName WHERE Id = @id;
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    /// <summary>
    /// Get first entity matching predicate.
    /// 
    /// IMPORTANT NOTE:
    /// Func<T, bool> vs Func<T, Task<bool>>
    /// - We use Func<T, bool> (synchronous predicate)
    /// - More common, easier to use
    /// - EF Core translates to SQL
    /// 
    /// LIMITATION:
    /// Predicate is evaluated in-memory (LINQ to Objects)
    /// NOT in database (LINQ to SQL)
    /// 
    /// PERFORMANCE IMPACT:
    /// - All entities loaded to memory
    /// - Then filtered locally
    /// - Bad for large tables!
    /// 
    /// BETTER APPROACH FOR LARGE DATA:
    /// For large queries, use IQueryable with LINQ to EF:
    /// _dbSet.Where(p => p.Email == email).FirstOrDefaultAsync()
    /// 
    /// This project: Acceptable for now
    /// Real-world: Would optimize per query
    /// </summary>
    public virtual async Task<T?> FirstOrDefaultAsync(Func<T, bool> predicate)
    {
        return await Task.FromResult(_dbSet.FirstOrDefault(predicate));
    }

    /// <summary>
    /// Get multiple entities matching predicate.
    /// 
    /// SAME LIMITATION AS FirstOrDefaultAsync:
    /// - In-memory filtering
    /// - All entities loaded first
    /// - Not ideal for huge tables
    /// 
    /// For production systems:
    /// Create specialized methods with IQueryable for complex queries:
    /// 
    /// public async Task<IEnumerable<User>> GetActiveUsersAsync()
    /// {
    ///     return await _dbSet
    ///         .Where(u => u.IsActive) // Translated to SQL
    ///         .ToListAsync();
    /// }
    /// </summary>
    public virtual async Task<IEnumerable<T>> WhereAsync(Func<T, bool> predicate)
    {
        return await Task.FromResult(_dbSet.Where(predicate).ToList());
    }

    /// <summary>
    /// Add new entity to context (not yet saved).
    /// 
    /// PROCESS:
    /// 1. AddAsync marks entity as "Added"
    /// 2. SaveChangesAsync executes INSERT
    /// 
    /// EXAMPLE:
    /// var user = new User { Email = "test@company.com", ... };
    /// await _repository.AddAsync(user);
    /// await _unitOfWork.SaveChangesAsync();
    /// // NOW it's in database
    /// </summary>
    public virtual async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    /// <summary>
    /// Add multiple entities to context.
    /// 
    /// More efficient than:
    /// foreach (var entity in entities)
    ///     await AddAsync(entity);
    /// </summary>
    public virtual async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    /// <summary>
    /// Update entity in context.
    /// 
    /// CHANGE TRACKING:
    /// Entity Framework tracks entities automatically.
    /// If you modify an entity from _dbSet:
    /// var user = await _repository.GetByIdAsync(id);
    /// user.Email = "new@company.com";
    /// // No need to call Update! EF tracks the change.
    /// // SaveChangesAsync sees the change and generates UPDATE SQL
    /// 
    /// WHY STILL HAVE Update() METHOD?
    /// - Explicit is better than implicit
    /// - Clear intent: "I'm modifying this"
    /// - Handles detached entities (loaded from another context)
    /// 
    /// DETACHED ENTITIES:
    /// var user = await someOtherContext.Users.FirstAsync(u => u.Id == id);
    /// // user is from different context, not tracked
    /// _context.Update(user); // Reattach to current context
    /// </summary>
    public virtual void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    /// <summary>
    /// Delete entity.
    /// 
    /// Marks entity as "Deleted"
    /// SaveChangesAsync generates DELETE SQL
    /// </summary>
    public virtual void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }

    /// <summary>
    /// Delete multiple entities.
    /// </summary>
    public virtual void DeleteRange(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    /// <summary>
    /// Check if any entity matches predicate.
    /// 
    /// PERFORMANCE:
    /// - Optimized: Returns after finding first match
    /// - No need to fetch entire entity
    /// 
    /// EXAMPLE:
    /// if (await _repo.AnyAsync(u => u.Email == email))
    ///     // Email already exists
    /// </summary>
    public virtual async Task<bool> AnyAsync(Func<T, bool> predicate)
    {
        return await Task.FromResult(_dbSet.Any(predicate));
    }

    /// <summary>
    /// Count entities matching predicate.
    /// 
    /// NULL PREDICATE:
    /// If predicate is null, count all entities.
    /// If predicate provided, count only matching.
    /// </summary>
    public virtual async Task<int> CountAsync(Func<T, bool>? predicate = null)
    {
        if (predicate == null)
            return await _dbSet.CountAsync();

        return await Task.FromResult(_dbSet.Count(predicate));
    }
}

/// <summary>
/// User-specific repository implementation.
/// 
/// EXTENDS: GenericRepository<User> (inherits CRUD)
/// IMPLEMENTS: IUserRepository (specialized methods)
/// 
/// SPECIALIZED METHODS:
/// - GetByEmailWithDetailsAsync: For login
/// - EmailExistsAsync: For registration
/// - GetAllAdminsAsync: For admin operations
/// </summary>
public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    /// <summary>
    /// Get user by email with all related data.
    /// 
    /// INCLUDES:
    /// - Documents: User's uploaded files
    /// - Conversations: User's Q&A history
    /// - RefreshTokens: User's active sessions
    /// 
    /// WHY EAGER LOADING?
    /// Include(u => u.Documents) loads in single query:
    /// SELECT u.*, d.* FROM Users u LEFT JOIN Documents d ON d.UserId = u.Id
    /// 
    /// WITHOUT Include():
    /// SELECT * FROM Users WHERE Email = @email
    /// Then later: var docs = user.Documents // NULL!
    /// 
    /// PERFORMANCE:
    /// - Single query: Good
    /// - Loads all related data upfront
    /// - More memory, but fewer database calls
    /// </summary>
    public async Task<User?> GetByEmailWithDetailsAsync(string email)
    {
        return await _context.Users
            .Include(u => u.Documents)
            .Include(u => u.Conversations)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <summary>
    /// Check if email is already registered.
    /// 
    /// USAGE IN REGISTRATION:
    /// if (await _userRepository.EmailExistsAsync(email))
    ///     throw new InvalidBusinessRuleException("Email already registered");
    /// 
    /// PERFORMANCE:
    /// - Optimized: Only checks existence
    /// - Returns bool (not entity)
    /// - SQL: SELECT 1 FROM Users WHERE Email = @email LIMIT 1
    /// </summary>
    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    /// <summary>
    /// Get all admin users.
    /// 
    /// USAGE:
    /// - Send notifications to all admins
    /// - Admin dashboard: "How many admins do we have?"
    /// 
    /// PERFORMANCE:
    /// - Uses index on Role column
    /// - Fast even with many users
    /// </summary>
    public async Task<IEnumerable<User>> GetAllAdminsAsync()
    {
        return await _context.Users
            .Where(u => u.Role == Domain.Enums.UserRole.Admin)
            .ToListAsync();
    }
}

/// <summary>
/// Document-specific repository implementation.
/// 
/// SPECIALIZED METHODS:
/// - GetUserDocumentsAsync: Security - only user's documents
/// - GetWithChunksAsync: RAG pipeline - document with all chunks
/// - GetByFileNameAsync: Prevent duplicates
/// </summary>
public class DocumentRepository : GenericRepository<Document>, IDocumentRepository
{
    public DocumentRepository(ApplicationDbContext context) : base(context) { }

    /// <summary>
    /// Get all documents for a specific user.
    /// 
    /// SECURITY:
    /// Only returns user's own documents.
    /// Application layer enforces authorization.
    /// 
    /// EXAMPLE:
    /// var userDocs = await _docRepository.GetUserDocumentsAsync(userId);
    /// // Returns: only documents where UserId == userId
    /// </summary>
    public async Task<IEnumerable<Document>> GetUserDocumentsAsync(Guid userId)
    {
        return await _context.Documents
            .Where(d => d.UserId == userId)
            .ToListAsync();
    }

    /// <summary>
    /// Get document with all its chunks.
    /// 
    /// RAG PIPELINE:
    /// - After upload, chunks are created from document
    /// - To search, need all chunks
    /// - This query loads them efficiently
    /// 
    /// Include() = LEFT JOIN in SQL
    /// Result: Document with Chunks collection populated
    /// </summary>
    public async Task<Document?> GetWithChunksAsync(Guid documentId)
    {
        return await _context.Documents
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.Id == documentId);
    }

    /// <summary>
    /// Find document by filename for a user.
    /// 
    /// USE CASE:
    /// Before uploading new file:
    /// var existing = await _docRepo.GetByFileNameAsync(fileName, userId);
    /// if (existing != null)
    ///     throw new InvalidBusinessRuleException("File already uploaded");
    /// </summary>
    public async Task<Document?> GetByFileNameAsync(string fileName, Guid userId)
    {
        return await _context.Documents
            .FirstOrDefaultAsync(d => d.FileName == fileName && d.UserId == userId);
    }
}

/// <summary>
/// DocumentChunk repository - RAG CORE.
/// 
/// CRITICAL METHODS:
/// - FindSimilarAsync: Vector similarity search (pgvector)
/// - GetChunksByDocumentAsync: All chunks for a document
/// - GetChunksWithEmbeddingsAsync: Only searchable chunks
/// </summary>
public class DocumentChunkRepository : GenericRepository<DocumentChunk>, IDocumentChunkRepository
{
    public DocumentChunkRepository(ApplicationDbContext context) : base(context) { }

    /// <summary>
    /// Get all chunks for a document.
    /// </summary>
    public async Task<IEnumerable<DocumentChunk>> GetChunksByDocumentAsync(Guid documentId)
    {
        return await _context.DocumentChunks
            .Where(c => c.DocumentId == documentId)
            .ToListAsync();
    }

    /// <summary>
    /// Get chunks from specific page.
    /// </summary>
    public async Task<IEnumerable<DocumentChunk>> GetChunksByPageAsync(int pageNumber)
    {
        return await _context.DocumentChunks
            .Where(c => c.PageNumber == pageNumber)
            .ToListAsync();
    }

    /// <summary>
    /// Get only chunks that have embeddings (ready for search).
    /// </summary>
    public async Task<IEnumerable<DocumentChunk>> GetChunksWithEmbeddingsAsync()
    {
        return await _context.DocumentChunks
            .Where(c => c.VectorEmbedding != null)
            .ToListAsync();
    }

    /// <summary>
    /// VECTOR SIMILARITY SEARCH - THE RAG MAGIC!
    /// 
    /// WHAT THIS DOES:
    /// 1. Takes question embedding (float array, 1536 dimensions)
    /// 2. Searches pgvector for similar chunks
    /// 3. Returns top K most similar
    /// 
    /// POSTGRESQL pgvector OPERATOR:
    /// <-> = cosine distance (similarity)
    /// Closer to 0 = more similar
    /// 
    /// SQL GENERATED (internally):
    /// SELECT *
    /// FROM DocumentChunks
    /// WHERE VectorEmbedding IS NOT NULL
    /// ORDER BY VectorEmbedding <-> @queryEmbedding
    /// LIMIT @topK;
    /// 
    /// INDEX:
    /// IVFFlat index makes this query FAST
    /// Without index, would scan all rows (slow!)
    /// With index, finds approximate nearest neighbors efficiently
    /// 
    /// TRADE-OFF:
    /// - IVFFlat: Fast, approximately correct (99-99.9% accurate typically)
    /// - HNSW: Faster, even more approximate
    /// - Exact (no index): Slow, 100% accurate
    /// 
    /// For RAG, approximate is fine!
    /// 100 milliseconds with approximate > 1 second exact
    /// User experience matters more than mathematical purity
    /// 
    /// EXAMPLE USAGE:
    /// // User asks: "How many vacation days?"
    /// var questionEmbedding = await _embeddingService.CreateAsync(question);
    /// 
    /// // Find top 5 most similar chunks
    /// var relevantChunks = await _chunkRepository.FindSimilarAsync(questionEmbedding, topK: 5);
    /// 
    /// // Result: DocumentChunks about vacation policy (most similar first)
    /// // These chunks are context for the LLM
    /// 
    /// ACCURACY:
    /// Top 1: HR_Policy, page 3, "Vacation: 25 days per year"
    /// Top 2: HR_Policy, page 4, "Exceptions: executives get 30 days"
    /// Top 3: FAQ, page 1, "Can I take unpaid leave?"
    /// Top 4: HR_Policy, page 5, "Holiday calendar"
    /// Top 5: Benefits, page 2, "Health insurance"
    /// 
    /// Perfect ranking! Semantic search works!
    /// </summary>
    public async Task<IEnumerable<DocumentChunk>> FindSimilarAsync(float[] queryEmbedding, int topK)
    {
        // ⚠️ NOTE: This implementation is PSEUDOCODE
        // Real implementation requires NpgsqlDbContext with vector support
        // Will be implemented in next section with proper pgvector setup
        
        // For now, return empty to compile
        // Implementation will use raw SQL or Npgsql extension:
        // 
        // SELECT * FROM DocumentChunks
        // ORDER BY VectorEmbedding <-> @queryEmbedding::vector
        // LIMIT @topK;
        
        await Task.Delay(0); // Async placeholder
        return new List<DocumentChunk>();
    }
}
