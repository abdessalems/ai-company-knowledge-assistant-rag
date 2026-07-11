namespace AIKnowledgeAssistant.Infrastructure.Extensions;

using AIKnowledgeAssistant.Application.Interfaces;
using AIKnowledgeAssistant.Infrastructure.AI;
using AIKnowledgeAssistant.Infrastructure.Authentication;
using AIKnowledgeAssistant.Infrastructure.Database;
using AIKnowledgeAssistant.Infrastructure.Repositories;
using AIKnowledgeAssistant.Infrastructure.Storage;
using AIKnowledgeAssistant.Infrastructure.TextExtraction;
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
        string connectionString,
        string fileStorageBasePath)
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

        // ====================================
        // AUTHENTICATION SERVICES
        // ====================================

        /// Register authentication services
        /// 
        /// SERVICES REGISTERED:
        /// 1. JwtSettings: Configuration
        /// 2. IPasswordHasher: Bcrypt hashing
        /// 3. IJwtTokenGenerator: JWT token creation
        /// 4. IAuthenticationService: Auth logic
        /// 
        /// LIFETIMES:
        /// - JwtSettings: Singleton (immutable configuration)
        /// - Services: Scoped (use DbContext per request)
        
        // Load JWT settings from configuration
        // Would normally come from appsettings.json
        // For now: Hardcoded, will be replaced with config
        var jwtSettings = new JwtSettings
        {
            SecretKey = "your-super-secret-key-must-be-at-least-32-characters-long",
            Issuer = "https://ai-knowledge-assistant.company.com",
            Audience = "ai-knowledge-assistant-api",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };

        services.AddSingleton(jwtSettings);
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        // ====================================
        // FILE STORAGE
        // ====================================
        // Singleton: the service is stateless and thread-safe (just reads/writes
        // files under a fixed base path), so one shared instance is ideal.
        // Swapping to Azure Blob / S3 later = change this ONE line.
        services.AddSingleton<IFileStorageService>(
            new LocalFileStorageService(fileStorageBasePath));

        // ====================================
        // TEXT EXTRACTORS (strategy per file type)
        // ====================================
        // Both are registered under ITextExtractor, so the extraction facade
        // receives ALL of them (IEnumerable<ITextExtractor>) and picks the one
        // that CanHandle the uploaded file's type. Add a type = add a class here.
        services.AddSingleton<ITextExtractor, PdfTextExtractor>();
        services.AddSingleton<ITextExtractor, PlainTextExtractor>();

        // ====================================
        // AI: EMBEDDINGS (Ollama, local & free)
        // ====================================
        // Typed HttpClient: the DI container creates and manages an HttpClient
        // for OllamaEmbeddingService. BaseAddress comes from OllamaSettings.
        // The long timeout covers embedding a whole document's chunks at once.
        services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>((provider, client) =>
        {
            var settings = provider.GetRequiredService<OllamaSettings>();
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromMinutes(5);
        });

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
