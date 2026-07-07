namespace AIKnowledgeAssistant.Infrastructure.Repositories;

using AIKnowledgeAssistant.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using AIKnowledgeAssistant.Infrastructure.Database;

/// <summary>
/// Unit of Work implementation.
/// 
/// RESPONSIBILITIES:
/// 1. Coordinates multiple repositories
/// 2. Manages single DbContext (shared by all repositories)
/// 3. Handles transactions
/// 4. Coordinates SaveChangesAsync
/// 
/// PATTERN BENEFITS:
/// - Ensures all repositories use same DbContext
/// - Enables transactions across multiple operations
/// - All-or-nothing semantics: Save all changes or roll back
/// - Cleaner API: _unitOfWork.SaveChangesAsync() instead of managing context
/// 
/// TRANSACTION EXAMPLE:
/// // Add user AND document atomically
/// var user = new User { ... };
/// var doc = new Document { UserId = user.Id, ... };
/// 
/// await _unitOfWork.Users.AddAsync(user);
/// await _unitOfWork.Documents.AddAsync(doc);
/// 
/// await _unitOfWork.SaveChangesAsync();
/// // If error, BOTH are rolled back!
/// // Without UoW, only one might be saved (inconsistent state)
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    /// <summary>
    /// The main DbContext instance shared by all repositories.
    /// </summary>
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Lazy-initialized repositories.
    /// 
    /// WHY LAZY INITIALIZATION?
    /// - Only create repositories when first accessed
    /// - If you never access Users repository, don't create it
    /// - Saves memory
    /// - Follows "lazy loading" pattern
    /// 
    /// EXAMPLE:
    /// public IUserRepository Users =>
    ///     _users ??= new UserRepository(_context);
    /// 
    /// First access: Create UserRepository
    /// Later accesses: Return existing instance
    /// </summary>
    private IUserRepository? _users;
    private IDocumentRepository? _documents;
    private IDocumentChunkRepository? _documentChunks;
    private IRepository<Domain.Entities.Conversation>? _conversations;
    private IRepository<Domain.Entities.MessageSource>? _messageSources;
    private IRepository<Domain.Entities.RefreshToken>? _refreshTokens;

    /// <summary>
    /// Constructor with DbContext injection.
    /// </summary>
    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Users repository (lazy-initialized).
    /// 
    /// EXAMPLE USAGE:
    /// var user = await _unitOfWork.Users.GetByEmailAsync("user@company.com");
    /// </summary>
    public IUserRepository Users =>
        _users ??= new UserRepository(_context);

    /// <summary>
    /// Documents repository.
    /// </summary>
    public IDocumentRepository Documents =>
        _documents ??= new DocumentRepository(_context);

    /// <summary>
    /// DocumentChunks repository (RAG core).
    /// </summary>
    public IDocumentChunkRepository DocumentChunks =>
        _documentChunks ??= new DocumentChunkRepository(_context);

    /// <summary>
    /// Conversations repository.
    /// </summary>
    public IRepository<Domain.Entities.Conversation> Conversations =>
        _conversations ??= new GenericRepository<Domain.Entities.Conversation>(_context);

    /// <summary>
    /// MessageSources repository.
    /// </summary>
    public IRepository<Domain.Entities.MessageSource> MessageSources =>
        _messageSources ??= new GenericRepository<Domain.Entities.MessageSource>(_context);

    /// <summary>
    /// RefreshTokens repository.
    /// </summary>
    public IRepository<Domain.Entities.RefreshToken> RefreshTokens =>
        _refreshTokens ??= new GenericRepository<Domain.Entities.RefreshToken>(_context);

    /// <summary>
    /// Save all pending changes atomically.
    /// 
    /// WHAT HAPPENS:
    /// 1. EF Core collects all changes (Added, Modified, Deleted entities)
    /// 2. Generates SQL INSERT/UPDATE/DELETE statements
    /// 3. Wraps in transaction
    /// 4. Executes all-or-nothing
    /// 5. Returns number of entities saved
    /// 
    /// ERROR HANDLING:
    /// If any statement fails:
    /// - Transaction rolls back
    /// - NO changes are saved
    /// - Exception is thrown
    /// 
    /// EXAMPLE:
    /// try
    /// {
    ///     var user = new User { ... };
    ///     await _unitOfWork.Users.AddAsync(user);
    ///     int saved = await _unitOfWork.SaveChangesAsync();
    ///     _logger.Information($"Saved {saved} entities");
    /// }
    /// catch (DbUpdateException ex)
    /// {
    ///     _logger.Error(ex, "Database error");
    ///     throw;
    /// }
    /// 
    /// PERFORMANCE NOTE:
    /// - Single round-trip to database
    /// - All changes saved in one transaction
    /// - Faster than multiple SaveChangesAsync calls
    /// </summary>
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Begin explicit transaction.
    /// 
    /// WHY EXPLICIT TRANSACTION?
    /// SaveChangesAsync creates implicit transaction
    /// For complex scenarios, explicit gives more control
    /// 
    /// EXAMPLE: Two-phase save
    /// using var transaction = await _unitOfWork.BeginTransactionAsync();
    /// try
    /// {
    ///     // Step 1: Create user
    ///     var user = new User { ... };
    ///     await _unitOfWork.Users.AddAsync(user);
    ///     await _unitOfWork.SaveChangesAsync();
    ///     
    ///     // Step 2: Create document (depends on user.Id)
    ///     var doc = new Document { UserId = user.Id, ... };
    ///     await _unitOfWork.Documents.AddAsync(doc);
    ///     await _unitOfWork.SaveChangesAsync();
    ///     
    ///     await transaction.CommitAsync();
    ///     _logger.Information("Both steps completed successfully");
    /// }
    /// catch (Exception ex)
    /// {
    ///     await transaction.RollbackAsync();
    ///     _logger.Error(ex, "Transaction failed, rolling back");
    ///     throw;
    /// }
    /// 
    /// BENEFITS:
    /// - Clear when transaction starts/ends
    /// - Explicit rollback on error
    /// - Multiple SaveChangesAsync in one transaction
    /// - If second SaveChangesAsync fails, first is still rolled back
    /// </summary>
    public async Task BeginTransactionAsync()
    {
        await _context.Database.BeginTransactionAsync();
    }

    /// <summary>
    /// Commit current transaction.
    /// </summary>
    public async Task CommitTransactionAsync()
    {
        var transaction = _context.Database.CurrentTransaction;
        if (transaction != null)
        {
            await transaction.CommitAsync();
        }
    }

    /// <summary>
    /// Rollback current transaction.
    /// 
    /// USAGE:
    /// In catch block, rollback to undo all changes
    /// </summary>
    public async Task RollbackTransactionAsync()
    {
        var transaction = _context.Database.CurrentTransaction;
        if (transaction != null)
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// Dispose pattern for resource cleanup.
    /// 
    /// IMPORTANT:
    /// DbContext must be disposed to release database connections
    /// 
    /// DEPENDENCY INJECTION:
    /// When using DI container, this is called automatically:
    /// services.AddScoped<IUnitOfWork, UnitOfWork>();
    /// // Container calls Dispose after request is done
    /// 
    /// EXAMPLE IF MANUAL:
    /// using var unitOfWork = new UnitOfWork(context);
    /// {
    ///     // Use unitOfWork
    /// }
    /// // Dispose called automatically here
    /// </summary>
    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}
