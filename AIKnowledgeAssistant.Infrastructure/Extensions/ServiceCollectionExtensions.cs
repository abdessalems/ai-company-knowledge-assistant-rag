namespace AIKnowledgeAssistant.Infrastructure.Extensions;

using AIKnowledgeAssistant.Application.Interfaces;
using AIKnowledgeAssistant.Infrastructure.Database;
using AIKnowledgeAssistant.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension method for registering Infrastructure services.
/// 
/// WHAT IS AN EXTENSION METHOD?
/// Allows adding methods to existing types without inheritance.
/// 
/// Here: We add AddInfrastructure() method to IServiceCollection
/// IServiceCollection is ASP.NET Core's Dependency Injection container
/// 
/// DEPENDENCY INJECTION PRINCIPLES:
/// 1. Register services in DI container (Program.cs)
/// 2. Container creates instances when needed
/// 3. Resolves dependencies automatically
/// 4. Makes testing easier (can inject fakes)
/// 
/// EXAMPLE USAGE IN PROGRAM.CS:
/// var builder = WebApplication.CreateBuilder(args);
/// 
/// builder.Services.AddInfrastructure(builder.Configuration);
/// // Now DbContext, repositories, UnitOfWork are registered
/// 
/// LIFETIME OPTIONS:
/// - AddTransient: New instance every time
/// - AddScoped: One per HTTP request
/// - AddSingleton: One for entire application
/// 
/// FOR THIS PROJECT:
/// - DbContext: Scoped (one per request - clean isolation)
/// - Repositories: Scoped (use same DbContext as request)
/// - UnitOfWork: Scoped (same DbContext for request)
/// </summary>
public static class InfrastructureExtensions
{
    /// <summary>
    /// Register all Infrastructure layer services.
    /// 
    /// REGISTERS:
    /// 1. ApplicationDbContext - Database connection
    /// 2. UnitOfWork - Repository coordinator
    /// 
    /// Called from: Program.cs
    /// Keeps Program.cs clean (details hidden in extension)
    /// 
    /// PATTERN BENEFITS:
    /// - Program.cs stays small
    /// - Each layer has own extension
    /// - Easy to enable/disable features
    /// - DRY: Don't repeat DI configuration
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // ====================================
        // DATABASE CONTEXT
        // ====================================
        
        /// Register DbContext with PostgreSQL
        /// 
        /// UseNpgsql: Npgsql provider for PostgreSQL
        /// 
        /// OPTIONS CONFIGURED:
        /// 1. Connection string: Where to connect
        /// 2. UseQueryTrackingBehavior: How to track changes
        ///    - NoTracking: Faster for read-only queries
        ///    - Tracking: Track changes for update/delete
        ///    - See below for explanation
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions
                    .MigrationsHistoryTable("__EFMigrationsHistory", "public")
                    // pgvector extension: enables vector operations
                    .UseNetTopologySuite()
            )
            // Track changes by default (needed for SaveChangesAsync)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
        });

        /// ====================================
        /// QUERY TRACKING BEHAVIOR EXPLAINED
        /// ====================================
        /// 
        /// TRACKING = Change Detection
        /// 
        /// ❌ Without tracking (NoTracking):
        /// var user = await _context.Users.AsNoTracking().FirstAsync(u => u.Id == id);
        /// user.Email = "new@company.com";
        /// await _context.SaveChangesAsync();
        /// // Email NOT saved! Entity not tracked
        /// // Useful for: Read-only queries, faster, less memory
        /// 
        /// ✅ With tracking (TrackAll):
        /// var user = await _context.Users.FirstAsync(u => u.Id == id);
        /// user.Email = "new@company.com";
        /// await _context.SaveChangesAsync();
        /// // Email IS saved! Entity is tracked
        /// // Useful for: Modify and save, normal CRUD
        /// 
        /// DEFAULT: TrackAll (safe for normal CRUD)
        /// OPTIMIZATION: Use AsNoTracking() for read-only queries
        /// 
        /// PERFORMANCE:
        /// - Tracking uses memory: Every entity tracked consumes memory
        /// - For queries returning 1000+ items: Use NoTracking
        /// - For normal CRUD: TrackAll is fine
        /// 
        /// In this project: Use TrackAll as default
        /// Optimize specific queries later if needed

        // ====================================
        // REPOSITORIES & UNIT OF WORK
        // ====================================

        /// Register UnitOfWork as scoped
        /// 
        /// SCOPED LIFETIME:
        /// - One UnitOfWork per HTTP request
        /// - Each request gets fresh instance
        /// - After request, Dispose is called
        /// - Clean isolation between requests
        /// 
        /// EXAMPLE:
        /// Request 1: Gets UnitOfWork instance A
        /// Request 2: Gets UnitOfWork instance B (different)
        /// After Request 1: Instance A is disposed
        /// After Request 2: Instance B is disposed
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}

/// <summary>
/// WHY EXTENSION METHODS FOR DI?
/// 
/// Program.cs is the startup file.
/// Without extensions, it becomes huge:
/// 
/// builder.Services.AddDbContext<ApplicationDbContext>(options => ...);
/// builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
/// builder.Services.AddScoped<IUserService, UserService>();
/// builder.Services.AddScoped<IDocumentService, DocumentService>();
/// ... 50 more registrations ...
/// 
/// WITH EXTENSIONS:
/// builder.Services.AddInfrastructure(connectionString);
/// builder.Services.AddApplication();
/// builder.Services.AddApi();
/// 
/// BENEFITS:
/// 1. Clean Program.cs
/// 2. Organized by layer
/// 3. Easy to enable/disable features
/// 4. Reusable across projects
/// 5. Follows Clean Architecture
/// </summary>
