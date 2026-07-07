namespace AIKnowledgeAssistant.Domain.Entities;

using AIKnowledgeAssistant.Domain.Enums;

/// <summary>
/// Represents a user in the system.
/// 
/// DOMAIN RESPONSIBILITY:
/// - Store user information
/// - Manage user identity and roles
/// - Never store plaintext passwords (only hashed in Infrastructure layer)
/// 
/// WHY THIS STRUCTURE?
/// - Id: Unique identifier (GUID for security, not sequential integers)
/// - Email: Unique, used for login and identification
/// - PasswordHash: Never store password, always hash it
/// - FirstName/LastName: For display purposes
/// - Role: Authorization level (User or Admin)
/// - CreatedAt: Audit trail (when was user created?)
/// 
/// BUSINESS RULES:
/// - Email must be unique (only one user per email)
/// - Email must be valid format (checked in Application layer)
/// - User must have a role (User or Admin)
/// - Password hash must be set before user can log in
/// 
/// ARCHITECTURAL NOTE:
/// This entity is PURE DOMAIN - no EF Core attributes, no validation attributes.
/// Why? Because:
/// 1. Domain should work in tests without database
/// 2. EF Core configuration goes in Infrastructure layer
/// 3. Validation goes in Application layer
/// 
/// This follows the "Separation of Concerns" principle:
/// - Domain = WHAT (what is a user?)
/// - Infrastructure = HOW TO PERSIST (how to save to database?)
/// - Application = HOW TO USE (how to validate user registration?)
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier for this user.
    /// Using GUID (Guid) instead of int because:
    /// - Sequential IDs can leak information (counting users)
    /// - GUIDs work across distributed systems
    /// - Modern security best practice
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User's email address - unique and used for login.
    /// IMPORTANT: Never null or empty
    /// Example: john.doe@company.com
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Bcrypt or Argon2 hashed password.
    /// NEVER stored plaintext - always hashed in Infrastructure layer
    /// Why Bcrypt/Argon2?
    /// - Slow algorithms prevent brute force attacks
    /// - Built-in salt prevents rainbow table attacks
    /// - Even if database is stolen, passwords are protected
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// User's first name for display purposes.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name for display purposes.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User's role for authorization.
    /// Values: User (regular user) or Admin (administrator)
    /// Used to determine: which documents can access, what operations allowed
    /// </summary>
    public UserRole Role { get; set; } = UserRole.User;

    /// <summary>
    /// When this user account was created.
    /// Useful for:
    /// - Audit trails (who created accounts when?)
    /// - Identifying dormant accounts
    /// - Compliance requirements
    /// 
    /// NOTE: Using UTC time (DateTime.UtcNow) to avoid timezone issues
    /// Never use DateTime.Now - it's machine-dependent!
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Collection of documents uploaded by this user.
    /// This creates a relationship: User has many Documents
    /// 
    /// WHY RELATIONSHIPS MATTER:
    /// - Database can enforce referential integrity
    /// - Deleting a user can cascade-delete their documents (configurable)
    /// - Querying is efficient: user.Documents instead of joins
    /// 
    /// NOTE: Using ICollection because Entity Framework uses this for relationships
    /// Collection initializer ensures it's never null
    /// </summary>
    public ICollection<Document> Documents { get; set; } = new List<Document>();

    /// <summary>
    /// Collection of conversations initiated by this user.
    /// Each conversation is a Q&A exchange
    /// </summary>
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();

    /// <summary>
    /// Collection of refresh tokens for this user.
    /// Used for JWT token rotation (security best practice)
    /// 
    /// WHY MULTIPLE TOKENS?
    /// - User can be logged in on multiple devices
    /// - Each device gets its own refresh token
    /// - If one token is compromised, others still valid
    /// - Admin can revoke specific device's token
    /// </summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    /// <summary>
    /// Helper method to get display name.
    /// 
    /// WHY HELPER METHODS IN ENTITIES?
    /// - Encapsulation of common logic
    /// - Reusable across application layer
    /// - If display format changes, change once
    /// 
    /// EXAMPLE: await _userService.GetDisplayNameAsync(user)
    /// Result: "John Doe"
    /// </summary>
    public string GetDisplayName() => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Business rule: Check if user is admin.
    /// 
    /// This is a domain method because it represents business logic:
    /// "Who has admin permissions?"
    /// 
    /// EXAMPLE USAGE IN APPLICATION LAYER:
    /// if (!user.IsAdmin())
    ///     throw new UnauthorizedAccessException("Only admins can delete users");
    /// </summary>
    public bool IsAdmin() => Role == UserRole.Admin;
}
