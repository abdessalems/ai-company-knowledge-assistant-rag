namespace AIKnowledgeAssistant.Infrastructure.Authentication;

/// <summary>
/// JWT (JSON Web Token) configuration settings.
/// 
/// WHAT IS JWT?
/// Three-part token: Header.Payload.Signature
/// 
/// EXAMPLE:
/// eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.
/// eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.
/// SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
/// 
/// PARTS:
/// 1. Header: Algorithm (HS256), Token type (JWT)
/// 2. Payload: Claims (user ID, roles, email)
/// 3. Signature: Proves token hasn't been tampered with
/// 
/// WHY JWT?
/// - Stateless: Server doesn't store tokens (unlike sessions)
/// - Scalable: Can verify on multiple servers
/// - Mobile-friendly: Works with mobile apps
/// - Standard: Used everywhere (OpenID Connect, OAuth2)
/// 
/// PROCESS:
/// 1. User logs in with email + password
/// 2. Server creates JWT token with claims
/// 3. Server sends token to client
/// 4. Client sends token in Authorization header
/// 5. Server verifies signature (proves token is real)
/// 6. Server extracts claims (user ID, roles)
/// 7. Request is authorized
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Secret key for signing JWT tokens.
    /// 
    /// ⚠️ CRITICAL SECURITY:
    /// - Must be very long (32+ characters)
    /// - Must be random and unique
    /// - Must be kept secret (not in source code!)
    /// - Should be in environment variables or Key Vault
    /// 
    /// EXAMPLE:
    /// "your-super-secret-key-that-is-very-long-and-random-32-plus-chars"
    /// 
    /// WHY SECRET?
    /// - Proof that token came from YOUR server
    /// - If leaked, anyone can create fake tokens!
    /// - This is your app's "master key"
    /// 
    /// STORAGE (not this simple config):
    /// Development: appsettings.Development.json
    /// Production: Azure Key Vault, AWS Secrets Manager, etc.
    /// 
    /// ROTATION:
    /// Change every 6-12 months
    /// If compromised: Change immediately
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Issuer of the token.
    /// 
    /// CLAIM: "iss" (issuer)
    /// 
    /// EXAMPLE:
    /// "https://ai-knowledge-assistant.company.com"
    /// 
    /// PURPOSE:
    /// - Identifies which app created the token
    /// - Prevents token reuse in other apps
    /// - Security: Only accept tokens from trusted issuers
    /// 
    /// VALIDATION:
    /// When verifying token, check:
    /// if (token.Issuer != JwtSettings.Issuer)
    ///     throw new UnauthorizedAccessException("Invalid issuer");
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Audience for the token.
    /// 
    /// CLAIM: "aud" (audience)
    /// 
    /// EXAMPLE:
    /// "ai-knowledge-assistant-api"
    /// 
    /// PURPOSE:
    /// - Identifies who can use this token
    /// - Prevents token reuse between apps
    /// - Scenario: One company, multiple APIs
    ///   - API1 tokens only work for API1
    ///   - API2 tokens only work for API2
    /// 
    /// VALIDATION:
    /// if (token.Audience != JwtSettings.Audience)
    ///     throw new UnauthorizedAccessException("Invalid audience");
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// How long the access token is valid (in minutes).
    /// 
    /// TYPICALLY: 15 minutes
    /// 
    /// WHY SHORT?
    /// - If token stolen, only works for 15 minutes
    /// - User can't be locked out by revoked token
    /// 
    /// TRADE-OFF:
    /// - Too short: User logs in constantly
    /// - Too long: Stolen token is useful longer
    /// - 15 minutes: Good balance
    /// 
    /// EXAMPLE:
    /// Token issued at: 2024-01-01 10:00:00
    /// Access token expires at: 2024-01-01 10:15:00
    /// After 10:15, need refresh token
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// How long the refresh token is valid (in days).
    /// 
    /// TYPICALLY: 7 days
    /// 
    /// WHY LONGER?
    /// - User can be away for weekend, still works
    /// - Still short enough for security
    /// - Stored in database (can be revoked)
    /// 
    /// PROCESS:
    /// 1. Access token expires after 15 minutes
    /// 2. Client sends refresh token
    /// 3. Server creates new access token (15 min)
    /// 4. User doesn't have to log in again!
    /// 5. After 7 days, refresh token expires
    /// 6. User logs in again
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>
    /// Validate JWT settings are properly configured.
    /// 
    /// VALIDATION:
    /// - SecretKey must be long enough (prevent weak key)
    /// - Issuer must be set
    /// - Audience must be set
    /// - Timeouts must be positive
    /// 
    /// CALLED: In DI setup before creating auth service
    /// THROWS: InvalidOperationException if invalid
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SecretKey) || SecretKey.Length < 32)
            throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long");

        if (string.IsNullOrWhiteSpace(Issuer))
            throw new InvalidOperationException("JWT Issuer must be configured");

        if (string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("JWT Audience must be configured");

        if (AccessTokenExpirationMinutes <= 0)
            throw new InvalidOperationException("AccessTokenExpirationMinutes must be positive");

        if (RefreshTokenExpirationDays <= 0)
            throw new InvalidOperationException("RefreshTokenExpirationDays must be positive");
    }
}
