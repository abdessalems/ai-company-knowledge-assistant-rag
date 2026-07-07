namespace AIKnowledgeAssistant.Application.Interfaces;

using AIKnowledgeAssistant.Domain.Entities;

/// <summary>
/// Generic repository interface for basic CRUD operations.
/// 
/// REPOSITORY PATTERN EXPLANATION:
/// 
/// What problem does Repository solve?
/// ❌ Bad: Service directly uses DbContext everywhere
///    - Hard to test (need real database)
///    - Hard to swap database implementations
///    - Data access logic scattered throughout code
/// 
/// ✅ Good: Service uses Repository abstraction
///    - Easy to mock in tests
///    - Can swap database implementations
///    - Centralized data access logic
///    - Clear separation: Application uses Repository,
///      Infrastructure implements Repository
/// 
/// DEPENDENCY FLOW:
/// Application Layer (uses interface)
///     ↓
/// IRepository<T> (interface defined here)
///     ↑
/// Infrastructure Layer (implements interface)
///     ↓
/// EF Core DbContext
///     ↓
/// PostgreSQL Database
/// 
/// GENERIC PATTERN:
/// Using <T> means one repository works for any entity type
/// IRepository<User>, IRepository<Document>, IRepository<Conversation>
/// All use the same base methods!
/// 
/// EXAMPLE USAGE IN APPLICATION SERVICE:
/// public class UserService
/// {
///     private readonly IRepository<User> _repository;
///     
///     public async Task<User> GetByEmailAsync(string email)
///     {
///         return await _repository.FirstOrDefaultAsync(u => u.Email == email);
///         // Doesn't know if database is PostgreSQL, MySQL, or In-Memory!
///     }
/// }
/// </summary>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Get all entities of type T.
    /// 
    /// ⚠️ WARNING: Can return huge result sets!
    /// Use with caution on large tables.
    /// Prefer filtered queries or pagination.
    /// 
    /// EXAMPLE:
    /// var allUsers = await _userRepository.GetAllAsync();
    /// 
    /// SQL GENERATED:
    /// SELECT * FROM Users;
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Get entity by primary key (ID).
    /// 
    /// MOST COMMON QUERY:
    /// - Get user by ID
    /// - Get document by ID
    /// - Get chunk by ID
    /// 
    /// EXAMPLE:
    /// var user = await _userRepository.GetByIdAsync(userId);
    /// 
    /// PERFORMANCE:
    /// - O(1) lookup (indexed by primary key)
    /// - Very fast
    /// 
    /// NULL HANDLING:
    /// - Returns null if ID not found
    /// - Application layer should check for null
    /// </summary>
    Task<T?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get single entity matching a predicate (condition).
    /// 
    /// PREDICATES: Lambda expressions that filter entities
    /// 
    /// EXAMPLES:
    /// var user = await _userRepository.FirstOrDefaultAsync(u => u.Email == "test@example.com");
    /// var admin = await _userRepository.FirstOrDefaultAsync(u => u.IsAdmin());
    /// var document = await _docRepository.FirstOrDefaultAsync(d => d.FileName == "HR_Policy.pdf");
    /// 
    /// SQL GENERATED (example):
    /// SELECT * FROM Users WHERE Email = 'test@example.com' LIMIT 1;
    /// 
    /// RETURNS:
    /// - First matching entity
    /// - null if no match found
    /// 
    /// WHY LAMBDA EXPRESSIONS?
    /// - Type-safe: Compile-time checking
    /// - IntelliSense: IDE shows available properties
    /// - No SQL injection: EF Core handles parameterization
    /// - Readable: Looks like C# code, not SQL strings
    /// </summary>
    Task<T?> FirstOrDefaultAsync(Func<T, bool> predicate);

    /// <summary>
    /// Get multiple entities matching a predicate.
    /// 
    /// EXAMPLES:
    /// var userDocs = await _docRepository.WhereAsync(d => d.UserId == userId);
    /// var adminUsers = await _userRepository.WhereAsync(u => u.IsAdmin());
    /// var chunks = await _chunkRepository.WhereAsync(c => c.DocumentId == docId);
    /// 
    /// RETURNS:
    /// - IEnumerable<T> of all matching entities
    /// - Empty collection if no matches
    /// 
    /// PERFORMANCE CONSIDERATION:
    /// Can return large result sets!
    /// Application layer should:
    /// - Paginate results
    /// - Limit result count
    /// - Use OrderBy for consistent ordering
    /// 
    /// Example: Get page 1, 20 items per page
    /// var page1 = (await _repo.WhereAsync(predicate))
    ///     .Skip(0)
    ///     .Take(20)
    ///     .ToList();
    /// </summary>
    Task<IEnumerable<T>> WhereAsync(Func<T, bool> predicate);

    /// <summary>
    /// Add new entity to repository (not yet saved).
    /// 
    /// TWO-STEP PROCESS:
    /// 1. Add entity:     _repository.AddAsync(newUser);
    /// 2. Save changes:   await _unitOfWork.SaveChangesAsync();
    /// 
    /// WHY NOT SAVE IMMEDIATELY?
    /// - Transactions: Can add multiple entities, save once
    /// - Rollback: If error, nothing is saved
    /// - Unit of Work pattern: Coordinate multiple operations
    /// 
    /// EXAMPLE:
    /// var newUser = new User 
    /// { 
    ///     Id = Guid.NewGuid(),
    ///     Email = "newuser@company.com",
    ///     FirstName = "John",
    ///     LastName = "Doe"
    /// };
    /// await _userRepository.AddAsync(newUser);
    /// await _unitOfWork.SaveChangesAsync(); // Actual save
    /// </summary>
    Task AddAsync(T entity);

    /// <summary>
    /// Add multiple entities to repository (not yet saved).
    /// 
    /// BULK OPERATIONS:
    /// Instead of: await AddAsync(user1); await AddAsync(user2); await AddAsync(user3);
    /// Do:         await AddRangeAsync(new[] { user1, user2, user3 });
    /// 
    /// More efficient than adding one-by-one.
    /// Still requires SaveChangesAsync() to persist.
    /// </summary>
    Task AddRangeAsync(IEnumerable<T> entities);

    /// <summary>
    /// Update existing entity.
    /// 
    /// PROCESS:
    /// 1. Fetch entity:    var user = await _repo.GetByIdAsync(id);
    /// 2. Modify:          user.Email = "newemail@company.com";
    /// 3. Update:          _repo.Update(user);
    /// 4. Save:            await _unitOfWork.SaveChangesAsync();
    /// 
    /// OR (change tracking):
    /// 1. Get entity:      var user = await _repo.GetByIdAsync(id);
    /// 2. Modify:          user.Email = "new@company.com";
    /// 3. Save (auto):     await _unitOfWork.SaveChangesAsync();
    /// 
    /// Entity Framework tracks changes automatically!
    /// If you modify an entity fetched from DbContext,
    /// you don't always need to call Update().
    /// But calling it explicitly is safer and clearer.
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Delete entity by ID.
    /// 
    /// WARNING: Two approaches:
    /// 
    /// Approach 1: Delete without fetching
    /// var entity = new T { Id = id };
    /// _repo.Delete(entity);
    /// // Entity is "stub" (only ID set)
    /// // EF Core knows to delete it
    /// 
    /// Approach 2: Fetch and delete
    /// var entity = await _repo.GetByIdAsync(id);
    /// _repo.Delete(entity);
    /// // Safer: confirms entity exists before deleting
    /// 
    /// Use Approach 2 in application code!
    /// </summary>
    void Delete(T entity);

    /// <summary>
    /// Delete multiple entities.
    /// 
    /// More efficient than deleting one-by-one.
    /// </summary>
    void DeleteRange(IEnumerable<T> entities);

    /// <summary>
    /// Check if any entity matches predicate.
    /// 
    /// EXAMPLES:
    /// if (await _userRepo.AnyAsync(u => u.Email == email))
    ///     throw new InvalidBusinessRuleException("Email already exists");
    /// 
    /// if (!await _docRepo.AnyAsync(d => d.Id == docId))
    ///     throw new EntityNotFoundException("Document", docId);
    /// 
    /// PERFORMANCE:
    /// - Optimized: Stops after finding first match
    /// - Returns boolean (true/false)
    /// - No need to fetch entire entity
    /// 
    /// SQL GENERATED:
    /// SELECT 1 FROM Documents WHERE Id = @id LIMIT 1;
    /// </summary>
    Task<bool> AnyAsync(Func<T, bool> predicate);

    /// <summary>
    /// Count entities matching predicate.
    /// 
    /// EXAMPLES:
    /// var userCount = await _userRepo.CountAsync();
    /// var adminCount = await _userRepo.CountAsync(u => u.IsAdmin());
    /// var userDocs = await _docRepo.CountAsync(d => d.UserId == userId);
    /// 
    /// RETURNS:
    /// - int: Number of matching entities
    /// 
    /// PERFORMANCE:
    /// - Uses COUNT SQL function (fast)
    /// - Doesn't fetch entities, just count
    /// </summary>
    Task<int> CountAsync(Func<T, bool>? predicate = null);
}

/// <summary>
/// Specialized repository for User entity.
/// 
/// WHY SPECIALIZED REPOSITORY?
/// Generic IRepository<User> handles basic CRUD.
/// But User queries need special logic:
/// - Find by email (used in login)
/// - Check if email exists (used in registration)
/// - Get user with related data (documents, conversations)
/// 
/// IUserRepository extends IRepository<User>
/// with User-specific methods.
/// 
/// PRINCIPLE: Repository per aggregate root
/// - User is an aggregate root (owns Documents, Conversations)
/// - Document is an aggregate root (owns Chunks)
/// - Need specialized repositories for aggregate roots
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Get user by email (for login).
    /// 
    /// BUSINESS REQUIREMENT:
    /// - Emails must be unique
    /// - Login by email not ID
    /// - Must include related data: Documents, Conversations, RefreshTokens
    /// 
    /// EXAMPLE:
    /// var user = await _userRepository.GetByEmailWithDetailsAsync("user@company.com");
    /// 
    /// WHY "WithDetails"?
    /// - Tells caller: "This query includes relationships"
    /// - Alternative: GetByEmailAsync returns user without relationships
    /// - Important for query planning (what data is loaded?)
    /// </summary>
    Task<User?> GetByEmailWithDetailsAsync(string email);

    /// <summary>
    /// Check if email already exists (for registration).
    /// 
    /// BUSINESS REQUIREMENT:
    /// - When user tries to register with email
    /// - Check if email is already taken
    /// 
    /// EXAMPLE:
    /// if (await _userRepository.EmailExistsAsync("newuser@company.com"))
    ///     throw new InvalidBusinessRuleException("Email already registered");
    /// 
    /// WHY NOT USE AnyAsync?
    /// - AnyAsync: Generic, from IRepository<User>
    /// - EmailExistsAsync: Named method, clear intent
    /// - More readable: "Is this email taken?" vs "Any user with this email?"
    /// </summary>
    Task<bool> EmailExistsAsync(string email);

    /// <summary>
    /// Get all admins (for admin-only operations).
    /// 
    /// USE CASES:
    /// - Send notifications to admins
    /// - Grant admin privileges
    /// - Audit log access
    /// </summary>
    Task<IEnumerable<User>> GetAllAdminsAsync();
}

/// <summary>
/// Specialized repository for Document entity.
/// 
/// Document-specific queries:
/// - Get user's documents
/// - Get document with chunks
/// - Find by filename
/// </summary>
public interface IDocumentRepository : IRepository<Document>
{
    /// <summary>
    /// Get all documents for a specific user.
    /// 
    /// BUSINESS REQUIREMENT:
    /// - Users can only see their own documents
    /// - Security: Enforce in repository
    /// 
    /// EXAMPLE:
    /// var userDocs = await _docRepository.GetUserDocumentsAsync(userId);
    /// </summary>
    Task<IEnumerable<Document>> GetUserDocumentsAsync(Guid userId);

    /// <summary>
    /// Get document with all its chunks.
    /// 
    /// WHY "WithChunks"?
    /// - Eager loading: Include related Chunks
    /// - Alternative: GetByIdAsync returns document without chunks
    /// - Important for RAG: Need all chunks for a document
    /// </summary>
    Task<Document?> GetWithChunksAsync(Guid documentId);

    /// <summary>
    /// Find document by filename for a user.
    /// 
    /// USE CASE:
    /// - Check if file already uploaded
    /// - Prevent duplicate uploads
    /// </summary>
    Task<Document?> GetByFileNameAsync(string fileName, Guid userId);
}

/// <summary>
/// Specialized repository for DocumentChunk entity.
/// 
/// RAG CORE: DocumentChunk-specific queries
/// - Find chunks by document
/// - Find by page number
/// - Vector similarity search (pgvector)
/// </summary>
public interface IDocumentChunkRepository : IRepository<DocumentChunk>
{
    /// <summary>
    /// Get all chunks for a specific document.
    /// 
    /// USAGE:
    /// - Fetch all chunks to regenerate embeddings
    /// - Delete all chunks when document is updated
    /// - Statistics: "How many chunks in this document?"
    /// </summary>
    Task<IEnumerable<DocumentChunk>> GetChunksByDocumentAsync(Guid documentId);

    /// <summary>
    /// Get chunks by page number (for pagination).
    /// 
    /// EXAMPLE:
    /// Get all chunks from page 5:
    /// var page5Chunks = await _chunkRepository.GetChunksByPageAsync(5);
    /// </summary>
    Task<IEnumerable<DocumentChunk>> GetChunksByPageAsync(int pageNumber);

    /// <summary>
    /// Get chunks with embeddings (ready for RAG search).
    /// 
    /// IMPORTANT FILTER:
    /// - Skip chunks without embeddings (processing incomplete)
    /// - Only return searchable chunks
    /// </summary>
    Task<IEnumerable<DocumentChunk>> GetChunksWithEmbeddingsAsync();

    /// <summary>
    /// Vector similarity search (THE RAG MAGIC).
    /// 
    /// WHAT DOES THIS DO?
    /// 1. Takes question embedding (vector)
    /// 2. Searches pgvector for similar chunks
    /// 3. Returns top K most similar chunks
    /// 4. Sorted by relevance (cosine similarity)
    /// 
    /// POSTGRESQL QUERY (internal):
    /// SELECT *
    /// FROM DocumentChunks
    /// ORDER BY VectorEmbedding <-> @queryEmbedding
    /// LIMIT @topK;
    /// 
    /// The <-> operator = cosine distance (similarity)
    /// IVFFlat index makes this FAST
    /// 
    /// EXAMPLE USAGE IN RAG SERVICE:
    /// var questionEmbedding = await _embeddingService.CreateAsync("How many vacation days?");
    /// var relevantChunks = await _chunkRepository.FindSimilarAsync(questionEmbedding, topK: 5);
    /// // Now have top 5 most relevant chunks
    /// // Send to LLM as context
    /// 
    /// This is the core of RAG - semantic search!
    /// </summary>
    Task<IEnumerable<DocumentChunk>> FindSimilarAsync(float[] queryEmbedding, int topK);
}

/// <summary>
/// Unit of Work pattern interface.
/// 
/// WHAT IS UNIT OF WORK?
/// Coordinates multiple repositories in a single transaction.
/// 
/// PROBLEM WITHOUT UNIT OF WORK:
/// - Multiple repositories, each with own DbContext = multiple connections
/// - Not a transaction: Can't rollback if one fails
/// - Inconsistent: Some changes saved, others not
/// 
/// SOLUTION WITH UNIT OF WORK:
/// - Single DbContext shared by all repositories
/// - SaveChangesAsync() saves everything atomically
/// - If error: Nothing is saved (rollback)
/// - Consistent: All-or-nothing semantics
/// 
/// EXAMPLE USAGE IN APPLICATION SERVICE:
/// var userRepository = _unitOfWork.Users;
/// var docRepository = _unitOfWork.Documents;
/// 
/// var newUser = new User { ... };
/// var newDoc = new Document { UserId = newUser.Id, ... };
/// 
/// await userRepository.AddAsync(newUser);
/// await docRepository.AddAsync(newDoc);
/// 
/// await _unitOfWork.SaveChangesAsync(); // Save both atomically!
/// 
/// ROLLBACK:
/// If SaveChangesAsync throws exception, neither is saved!
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Repository for User operations.
    /// </summary>
    IUserRepository Users { get; }

    /// <summary>
    /// Repository for Document operations.
    /// </summary>
    IDocumentRepository Documents { get; }

    /// <summary>
    /// Repository for DocumentChunk operations.
    /// </summary>
    IDocumentChunkRepository DocumentChunks { get; }

    /// <summary>
    /// Repository for Conversation operations.
    /// </summary>
    IRepository<Conversation> Conversations { get; }

    /// <summary>
    /// Repository for MessageSource operations.
    /// </summary>
    IRepository<MessageSource> MessageSources { get; }

    /// <summary>
    /// Repository for RefreshToken operations.
    /// </summary>
    IRepository<RefreshToken> RefreshTokens { get; }

    /// <summary>
    /// Save all changes atomically.
    /// 
    /// TRANSACTION SEMANTICS:
    /// - All or nothing
    /// - If error, nothing is saved
    /// - Returns number of entities saved
    /// 
    /// ASYNC/AWAIT:
    /// - Non-blocking
    /// - Doesn't block thread
    /// - Scales to many concurrent requests
    /// 
    /// EXAMPLE:
    /// int entriesSaved = await _unitOfWork.SaveChangesAsync();
    /// _logger.Information($"Saved {entriesSaved} entities");
    /// </summary>
    Task<int> SaveChangesAsync();

    /// <summary>
    /// Begin a database transaction.
    /// 
    /// USE CASES:
    /// - Need multiple SaveChangesAsync in one transaction
    /// - Complex multi-step operations
    /// - Ensure consistency
    /// 
    /// EXAMPLE:
    /// using var transaction = await _unitOfWork.BeginTransactionAsync();
    /// try
    /// {
    ///     await _unitOfWork.Users.AddAsync(user);
    ///     await _unitOfWork.SaveChangesAsync();
    ///     
    ///     await _unitOfWork.Documents.AddAsync(doc);
    ///     await _unitOfWork.SaveChangesAsync();
    ///     
    ///     await transaction.CommitAsync();
    /// }
    /// catch
    /// {
    ///     await transaction.RollbackAsync();
    ///     throw;
    /// }
    /// </summary>
    Task BeginTransactionAsync();

    /// <summary>
    /// Commit current transaction.
    /// </summary>
    Task CommitTransactionAsync();

    /// <summary>
    /// Rollback current transaction.
    /// </summary>
    Task RollbackTransactionAsync();
}
