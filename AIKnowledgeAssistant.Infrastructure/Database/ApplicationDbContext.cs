namespace AIKnowledgeAssistant.Infrastructure.Database;

using System.Text.Json;
using AIKnowledgeAssistant.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// Main database context for the AI Knowledge Assistant application.
/// 
/// WHAT IS DbContext?
/// DbContext is Entity Framework's connection to the database.
/// It acts as a TRANSLATOR between C# objects and SQL queries.
/// 
/// RESPONSIBILITIES:
/// 1. Define DbSets - Collections of entities (mapped to database tables)
/// 2. Configure entity relationships (one-to-many, many-to-many)
/// 3. Configure validation rules and constraints
/// 4. Manage database connections and transactions
/// 5. Track changes to entities (for SaveChangesAsync)
/// 
/// DATABASE SCHEMA:
/// - Users: User accounts with authentication
/// - Documents: Uploaded files metadata
/// - DocumentChunks: Text chunks with embeddings
/// - Conversations: Q&A history
/// - MessageSources: Citations for answers
/// - RefreshTokens: JWT token management
/// 
/// TECHNOLOGY:
/// - Entity Framework Core 10 (latest .NET)
/// - PostgreSQL 17 (database)
/// - pgvector extension (vector similarity search)
/// 
/// ASYNC/AWAIT NOTE:
/// All database operations use async/await:
/// - SaveChangesAsync() - Don't block thread
/// - ToListAsync(), FirstOrDefaultAsync(), etc.
/// - Better performance, especially under load
/// - Required for scalable applications
/// 
/// EXAMPLE USAGE IN APPLICATION LAYER:
/// var user = await _context.Users
///     .Include(u => u.Documents)
///     .Include(u => u.Conversations)
///     .FirstOrDefaultAsync(u => u.Email == email);
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Constructor that accepts DbContextOptions.
    /// 
    /// WHY CONSTRUCTOR WITH OPTIONS?
    /// - Dependency Injection: Services can configure options
    /// - Testability: Can inject test database configuration
    /// - Flexibility: Connection string in appsettings.json, not hardcoded
    /// 
    /// EXAMPLE IN PROGRAM.CS:
    /// services.AddDbContext<ApplicationDbContext>(options =>
    ///     options.UseNpgsql(connectionString)
    ///         .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
    /// </summary>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options) { }

    /// <summary>
    /// Collection of Users in the database.
    /// 
    /// DbSet represents a table in the database.
    /// Each User object = one row in Users table
    /// 
    /// QUERIES:
    /// var users = await _context.Users.ToListAsync(); // SELECT * FROM users
    /// var admin = await _context.Users
    ///     .FirstOrDefaultAsync(u => u.Email == "admin@company.com");
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Collection of Documents.
    /// 
    /// TYPICAL QUERIES:
    /// - Get all documents for a user: _context.Documents.Where(d => d.UserId == userId)
    /// - Get document with chunks: _context.Documents
    ///     .Include(d => d.Chunks).FirstOrDefaultAsync()
    /// </summary>
    public DbSet<Document> Documents { get; set; } = null!;

    /// <summary>
    /// Collection of DocumentChunks (core of RAG system).
    /// 
    /// CRITICAL QUERIES:
    /// - Find by page: _context.DocumentChunks.Where(c => c.PageNumber == 3)
    /// - Get all chunks for document: _context.DocumentChunks.Where(c => c.DocumentId == docId)
    /// - Get chunks with embeddings: _context.DocumentChunks.Where(c => c.VectorEmbedding != null)
    /// 
    /// pgvector QUERIES (explained below):
    /// Vector similarity search uses <-> operator (cosine distance)
    /// </summary>
    public DbSet<DocumentChunk> DocumentChunks { get; set; } = null!;

    /// <summary>
    /// Collection of Conversations (chat history).
    /// 
    /// QUERIES:
    /// - Get user's conversations: _context.Conversations
    ///     .Where(c => c.UserId == userId)
    ///     .Include(c => c.Sources)
    /// </summary>
    public DbSet<Conversation> Conversations { get; set; } = null!;

    /// <summary>
    /// Collection of MessageSources (answer citations).
    /// 
    /// QUERIES:
    /// - Get sources for conversation: _context.MessageSources
    ///     .Where(s => s.ConversationId == convId)
    ///     .OrderByDescending(s => s.RelevanceScore)
    /// </summary>
    public DbSet<MessageSource> MessageSources { get; set; } = null!;

    /// <summary>
    /// Collection of RefreshTokens (security/authentication).
    /// 
    /// QUERIES:
    /// - Validate token: _context.RefreshTokens
    ///     .Where(t => t.Token == tokenHash && !t.RevokedAt.HasValue)
    /// - Get user's active sessions: _context.RefreshTokens
    ///     .Where(t => t.UserId == userId && !t.RevokedAt.HasValue)
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    /// <summary>
    /// Configure entity-to-database mappings.
    /// 
    /// THIS IS WHERE THE MAGIC HAPPENS!
    /// 
    /// Two approaches:
    /// 1. Data Annotations: [Table], [Column] attributes on entities (not used here)
    /// 2. Fluent API: builder.Entity<User>() in this method (cleaner, more powerful)
    /// 
    /// We use Fluent API because:
    /// - Keeps entities pure (no attributes)
    /// - More readable for complex configurations
    /// - Follows Domain-Driven Design principle
    /// - Infrastructure configures the mapping, not entities
    /// 
    /// CONFIGURATIONS DONE HERE:
    /// - Table names
    /// - Primary keys
    /// - Foreign keys and relationships
    /// - Column types and constraints
    /// - Indexes for performance
    /// - Shadow properties
    /// - Conversions (value objects, enums)
    /// 
    /// NOTE: For large projects, split into separate files using IEntityTypeConfiguration
    /// For this project, keeping in one method for simplicity
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ====================================
        // VECTOR EMBEDDING CONVERSION
        // ====================================
        // The embedding is a float[] in C#, but the column is jsonb in Postgres.
        // A ValueConverter tells EF how to translate between the two:
        //   - saving:  float[]  -> JSON string (stored in the jsonb column)
        //   - loading: JSON     -> float[]
        var embeddingConverter = new ValueConverter<float[]?, string?>(
            v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => v == null ? null : JsonSerializer.Deserialize<float[]>(v, (JsonSerializerOptions?)null));

        // A ValueComparer teaches EF how to compare/clone arrays so it can detect
        // changes correctly (arrays are reference types, so it can't just use ==).
        var embeddingComparer = new ValueComparer<float[]?>(
            (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
            v => v == null ? 0 : v.Aggregate(0, (hash, f) => HashCode.Combine(hash, f.GetHashCode())),
            v => v == null ? null : v.ToArray());

        // ====================================
        // USER CONFIGURATION
        // ====================================

        modelBuilder.Entity<User>(entity =>
        {
            // Table name
            entity.ToTable("Users");

            // Primary Key
            entity.HasKey(e => e.Id);

            // Required fields
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255)
                .HasComment("User's email address - used for login");

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(255)
                .HasComment("Bcrypt hashed password - never plaintext");

            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(100)
                .HasComment("User's first name");

            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100)
                .HasComment("User's last name");

            entity.Property(e => e.Role)
                .IsRequired()
                .HasDefaultValue(AIKnowledgeAssistant.Domain.Enums.UserRole.User) // Enum, not int
                .HasComment("User's authorization level: 0=User, 1=Admin");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'")
                .HasComment("When user account was created (UTC)");

            // Unique constraint on Email
            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email_Unique");

            // ====================================
            // USER RELATIONSHIPS
            // ====================================

            // One User -> Many Documents
            entity.HasMany(e => e.Documents)
                .WithOne(d => d.User)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade) // If user deleted, delete their documents
                .HasConstraintName("FK_Documents_Users");

            // One User -> Many Conversations
            entity.HasMany(e => e.Conversations)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade) // If user deleted, delete their conversations
                .HasConstraintName("FK_Conversations_Users");

            // One User -> Many RefreshTokens
            entity.HasMany(e => e.RefreshTokens)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade) // If user deleted, delete their tokens
                .HasConstraintName("FK_RefreshTokens_Users");
        });

        // ====================================
        // DOCUMENT CONFIGURATION
        // ====================================

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("Documents");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.FileName)
                .IsRequired()
                .HasMaxLength(500)
                .HasComment("Original filename as uploaded by user");

            entity.Property(e => e.FilePath)
                .IsRequired()
                .HasMaxLength(1000)
                .HasComment("Full path where file is stored (S3, disk, etc)");

            entity.Property(e => e.FileType)
                .IsRequired()
                .HasComment("File type: 0=Pdf, 1=Docx, 2=Doc, 3=Txt");

            entity.Property(e => e.UploadedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'")
                .HasComment("When document was uploaded");

            entity.Property(e => e.FileSizeBytes)
                .IsRequired()
                .HasComment("File size in bytes");

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasComment("Document owner (Foreign key to Users)");

            // Index on UserId for quick lookup of user's documents
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_Documents_UserId");

            // ====================================
            // DOCUMENT RELATIONSHIPS
            // ====================================

            // One Document -> Many DocumentChunks
            entity.HasMany(e => e.Chunks)
                .WithOne(c => c.Document)
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.Cascade) // If document deleted, delete all chunks
                .HasConstraintName("FK_DocumentChunks_Documents");
        });

        // ====================================
        // DOCUMENT CHUNK CONFIGURATION
        // ====================================
        // THIS IS THE RAG CORE - Chunks with Vector Embeddings

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.ToTable("DocumentChunks");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Content)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasComment("Text content of this chunk");

            entity.Property(e => e.PageNumber)
                .IsRequired()
                .HasComment("Original page number in document");

            entity.Property(e => e.ChunkIndex)
                .IsRequired()
                .HasComment("Sequential index of chunk within document");

            // ====================================
            // VECTOR EMBEDDING CONFIGURATION
            // ====================================
            // 
            // VectorEmbedding is a float[] representing 1536 dimensions
            // For now, stored as JSON array in PostgreSQL
            // 
            // FUTURE: When pgvector NuGet is available:
            // .HasColumnType("vector(1536)")
            // This enables similarity search:
            // SELECT * FROM DocumentChunks
            // ORDER BY VectorEmbedding <-> @questionEmbedding
            // LIMIT 5;
            // 
            // CURRENT: Using jsonb column type
            // Full similarity search will be implemented with proper pgvector
            
            entity.Property(e => e.VectorEmbedding)
                .HasColumnType("jsonb") // Store as JSON array for now
                .HasConversion(embeddingConverter, embeddingComparer)
                .HasComment("Vector embedding for semantic similarity search (JSON array format)");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'")
                .HasComment("When chunk was created");

            entity.Property(e => e.DocumentId)
                .IsRequired();

            // Index on DocumentId for quick retrieval
            entity.HasIndex(e => e.DocumentId)
                .HasDatabaseName("IX_DocumentChunks_DocumentId");

            // JSON index for efficient searching
            entity.HasIndex(e => e.VectorEmbedding)
                .HasMethod("gin")
                .HasDatabaseName("IX_DocumentChunks_VectorEmbedding");
        });

        // ====================================
        // CONVERSATION CONFIGURATION
        // ====================================

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("Conversations");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Question)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasComment("Question asked by user");

            entity.Property(e => e.Answer)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasComment("AI-generated answer");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'")
                .HasComment("When this Q&A occurred");

            entity.Property(e => e.UpdatedAt)
                .HasComment("When answer was last updated (if corrected)");

            entity.Property(e => e.UserId)
                .IsRequired();

            // Index for quick retrieval of user's conversations
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_Conversations_UserId");

            // ====================================
            // CONVERSATION RELATIONSHIPS
            // ====================================

            // One Conversation -> Many MessageSources
            entity.HasMany(e => e.Sources)
                .WithOne(s => s.Conversation)
                .HasForeignKey(s => s.ConversationId)
                .OnDelete(DeleteBehavior.Cascade) // If conversation deleted, delete sources
                .HasConstraintName("FK_MessageSources_Conversations");
        });

        // ====================================
        // MESSAGE SOURCE CONFIGURATION
        // ====================================
        // Citations that link answers to source documents

        modelBuilder.Entity<MessageSource>(entity =>
        {
            entity.ToTable("MessageSources");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.DocumentName)
                .IsRequired()
                .HasMaxLength(500)
                .HasComment("Document name (denormalized for performance)");

            entity.Property(e => e.PageNumber)
                .IsRequired()
                .HasComment("Page number in source document");

            entity.Property(e => e.RelevanceScore)
                .IsRequired()
                .HasComment("Similarity score: 0.0 to 1.0");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'")
                .HasComment("When this source was cited");

            entity.Property(e => e.ConversationId)
                .IsRequired();

            entity.Property(e => e.DocumentChunkId)
                .IsRequired();

            // Indexes for quick lookups
            entity.HasIndex(e => e.ConversationId)
                .HasDatabaseName("IX_MessageSources_ConversationId");

            entity.HasIndex(e => e.DocumentChunkId)
                .HasDatabaseName("IX_MessageSources_DocumentChunkId");
        });

        // ====================================
        // REFRESH TOKEN CONFIGURATION
        // ====================================

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Token)
                .IsRequired()
                .HasMaxLength(255)
                .HasComment("Hashed refresh token");

            entity.Property(e => e.ExpirationDate)
                .IsRequired()
                .HasComment("When this token expires");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'")
                .HasComment("When token was issued");

            entity.Property(e => e.RevokedAt)
                .HasComment("When token was revoked (if applicable)");

            entity.Property(e => e.RevocationReason)
                .HasMaxLength(255)
                .HasComment("Why was token revoked?");

            entity.Property(e => e.DeviceIdentifier)
                .HasMaxLength(255)
                .HasComment("Device that created this token");

            entity.Property(e => e.UserId)
                .IsRequired();

            // Indexes for quick validation
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_RefreshTokens_UserId");

            entity.HasIndex(e => new { e.Token, e.RevokedAt })
                .HasDatabaseName("IX_RefreshTokens_Token_RevokedAt");

            entity.HasIndex(e => e.ExpirationDate)
                .HasDatabaseName("IX_RefreshTokens_ExpirationDate");
        });
    }
}
