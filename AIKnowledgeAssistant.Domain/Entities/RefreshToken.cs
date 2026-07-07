namespace AIKnowledgeAssistant.Domain.Entities;

/// <summary>
/// Represents a JWT refresh token for a user.
/// 
/// SECURITY CONCEPT: TOKEN ROTATION
/// 
/// THE PROBLEM WITH LONG-LIVED JWT TOKENS:
/// If JWT token is stolen, attacker can use it forever!
/// 
/// ❌ BAD APPROACH:
/// - Issue JWT that expires in 1 year
/// - If token leaked, attacker has 1 year access
/// - User can't force logout (token is stateless)
/// 
/// ✅ GOOD APPROACH (Token Rotation):
/// - Access Token (JWT): Expires in 15 minutes
/// - Refresh Token (long-lived): Expires in 7 days
/// - Store Refresh Tokens in DATABASE (can revoke!)
/// 
/// WORKFLOW:
/// 1. User logs in
/// 2. Server issues:
///    - Access Token (15 min expiry) → use in Authorization header
///    - Refresh Token (7 days) → store in HttpOnly cookie
/// 3. User makes requests with Access Token
/// 4. Access Token expires
/// 5. Client sends Refresh Token
/// 6. Server validates Refresh Token in DATABASE
/// 7. Server issues new Access Token + new Refresh Token
/// 8. User continues (uninterrupted)
/// 
/// SECURITY BENEFITS:
/// - Leaked Access Token only works 15 minutes
/// - Leaked Refresh Token can be revoked immediately (in database)
/// - User can force logout (delete all refresh tokens)
/// - User can see which devices have active sessions
/// 
/// MULTI-DEVICE SUPPORT:
/// User logged in on:
/// - Desktop computer (Refresh Token 1)
/// - Mobile phone (Refresh Token 2)
/// - Tablet (Refresh Token 3)
/// 
/// If user logs out on desktop, other devices still work!
/// (Because each device has separate token)
/// 
/// Admin could revoke mobile token if device is lost
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Unique identifier for this refresh token.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user who owns this token.
    /// Foreign key to User table.
    /// 
    /// RELATIONSHIP:
    /// User (1) ---- (Many) RefreshTokens
    /// One user can have multiple active tokens
    /// (one per device/login)
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to User.
    /// Set by Entity Framework.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// The actual token string (hashed for security).
    /// 
    /// HASHING REASON:
    /// Even if database is stolen, tokens are protected!
    /// 
    /// PROCESS:
    /// 1. Generate: token = secureRandomString() = "abc123xyz..."
    /// 2. Hash: tokenHash = Bcrypt.Hash(token)
    /// 3. Store: Save tokenHash in database
    /// 4. Send: Send token to client (not hash)
    /// 
    /// LATER VALIDATION:
    /// 1. Client sends: token = "abc123xyz..."
    /// 2. Server: Bcrypt.Verify(token, storedHash) == true?
    /// 3. If true, token is valid
    /// 
    /// WHY NOT STORE PLAINTEXT?
    /// If database leaks, attacker gets all tokens!
    /// With hashing, tokens are useless without original
    /// 
    /// STORAGE:
    /// - Length: ~88 characters (bcrypt hash standard)
    /// - Can't regenerate (one-way function)
    /// - Comparison is expensive (intentionally slow)
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// When this refresh token expires.
    /// 
    /// EXPIRY LOGIC:
    /// - Issued at: now
    /// - Expires at: now + 7 days
    /// - After expiry: token is INVALID
    /// 
    /// VALIDATION IN APPLICATION LAYER:
    /// if (token.ExpirationDate < DateTime.UtcNow)
    ///     throw new AuthenticationFailedException("Token expired");
    /// 
    /// WHY 7 DAYS?
    /// - Long enough: User can be away over weekend
    /// - Short enough: If token leaks, limited window
    /// - Industry standard: 7 days is common
    /// 
    /// CONFIGURABLE:
    /// In Infrastructure layer, this duration is config:
    /// services.Configure<JwtSettings>(config =>
    /// {
    ///     config.RefreshTokenExpirationDays = 7; // Configurable
    /// });
    /// 
    /// DIFFERENT SCENARIOS:
    /// - High security app: 3-7 days
    /// - Regular app: 7-14 days
    /// - Personal app: 30 days
    /// </summary>
    public DateTime ExpirationDate { get; set; }

    /// <summary>
    /// When this token was created/issued.
    /// 
    /// AUDIT PURPOSES:
    /// - Know when token was issued
    /// - Correlate with login attempt
    /// - Security analysis: "When did this device log in?"
    /// - Cleanup: Delete tokens older than X days
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Optional: When this token was revoked.
    /// 
    /// REVOCATION USE CASES:
    /// - User manually logged out
    /// - User changed password (invalidate all tokens)
    /// - Admin revoked device's access
    /// - Security incident (forced logout)
    /// 
    /// WHY USE IsRevoked INSTEAD OF DELETION?
    /// - Keep audit trail (show when token was revoked)
    /// - Can query: "How many tokens revoked last month?"
    /// - Compliance: Some regulations require audit logs
    /// 
    /// WORKFLOW:
    /// Instead of: DELETE FROM RefreshTokens WHERE Id = X
    /// Do: UPDATE RefreshTokens SET RevokedAt = NOW() WHERE Id = X
    /// 
    /// VALIDATION:
    /// if (token.RevokedAt.HasValue)
    ///     throw new AuthenticationFailedException("Token was revoked");
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Optional: Why was this token revoked?
    /// 
    /// EXAMPLES:
    /// - "User logout"
    /// - "Password changed"
    /// - "Admin revocation"
    /// - "Security: Possible compromise"
    /// - "Device lost"
    /// - "Session timeout"
    /// 
    /// AUDIT VALUE:
    /// Can query: "How many tokens revoked for security reasons?"
    /// Helps identify patterns (brute force? compromises?)
    /// </summary>
    public string? RevocationReason { get; set; }

    /// <summary>
    /// Optional: Identifier of the device that created this token.
    /// 
    /// EXAMPLE VALUES:
    /// - "Chrome on Windows 10"
    /// - "Safari on iPhone 15"
    /// - "Android App v2.1.0"
    /// - "Windows Desktop App v1.0.5"
    /// 
    /// WHY USEFUL?
    /// - User can see active sessions: "Logged in on 3 devices"
    /// - Can revoke specific device: "Logout on mobile phone"
    /// - Security: Identify unexpected devices
    /// 
    /// IMPLEMENTATION:
    /// Client sends device info in login request:
    /// POST /api/auth/login
    /// {
    ///   "email": "user@company.com",
    ///   "password": "...",
    ///   "deviceName": "Chrome on Windows 10"
    /// }
    /// 
    /// For v1: Optional, implement in v2
    /// </summary>
    public string? DeviceIdentifier { get; set; }

    /// <summary>
    /// Business rule: Check if token is still valid and not revoked.
    /// 
    /// VALIDATION CHECKS:
    /// - Not expired? (CreatedAt + duration > now)
    /// - Not revoked? (RevokedAt is null)
    /// 
    /// EXAMPLE USAGE IN APPLICATION LAYER:
    /// var refreshToken = await _context.RefreshTokens.FindAsync(tokenId);
    /// if (!refreshToken.IsValid())
    ///     throw new AuthenticationFailedException("Token invalid or expired");
    /// </summary>
    public bool IsValid() 
        => ExpirationDate > DateTime.UtcNow && !RevokedAt.HasValue;

    /// <summary>
    /// Business rule: Check if token is expired.
    /// </summary>
    public bool IsExpired() => ExpirationDate <= DateTime.UtcNow;

    /// <summary>
    /// Business rule: Check if token has been revoked.
    /// </summary>
    public bool IsRevoked() => RevokedAt.HasValue;

    /// <summary>
    /// Revoke this token (mark as no longer valid).
    /// 
    /// IDEMPOTENT: Can call multiple times safely
    /// 
    /// EXAMPLE USAGE:
    /// var token = await _context.RefreshTokens.FindAsync(tokenId);
    /// token.Revoke("User logout");
    /// await _context.SaveChangesAsync();
    /// 
    /// WHY METHOD INSTEAD OF JUST SETTING RevokedAt?
    /// - Encapsulation: Token controls its own revocation
    /// - Ensures RevokedAt and RevocationReason are set together
    /// - Can add more logic if needed later
    /// </summary>
    public void Revoke(string reason)
    {
        RevokedAt = DateTime.UtcNow;
        RevocationReason = reason;
    }
}
