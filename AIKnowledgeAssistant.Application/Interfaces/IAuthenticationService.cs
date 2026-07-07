namespace AIKnowledgeAssistant.Application.Interfaces;

using AIKnowledgeAssistant.Application.DTOs.Authentication;

/// <summary>
/// Authentication service interface.
/// 
/// RESPONSIBILITIES:
/// - User registration
/// - User login
/// - Token refresh
/// - Token validation
/// 
/// THIS IS APPLICATION LAYER:
/// - Orchestrates: Password hashing, token generation, repository access
/// - Doesn't know: JWT internals, Bcrypt details
/// - Uses: Interfaces from Infrastructure (IPasswordHasher, IJwtTokenGenerator)
/// 
/// DEPENDENCY FLOW:
/// API Controller
///     ↓ calls
/// IAuthenticationService (interface)
///     ↓ implemented by
/// AuthenticationService (application logic)
///     ↓ uses
/// IPasswordHasher, IJwtTokenGenerator, IUnitOfWork
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Register a new user.
    /// 
    /// PROCESS:
    /// 1. Validate input (email format, password strength)
    /// 2. Check if email already exists
    /// 3. Hash password
    /// 4. Create user entity
    /// 5. Save to database
    /// 6. Return success with user info
    /// 
    /// VALIDATIONS:
    /// - Email must be valid format
    /// - Email must be unique
    /// - Password must be strong
    /// - FirstName/LastName required
    /// 
    /// RETURNS:
    /// AuthResponse with user info (NOT token, user must login)
    /// 
    /// THROWS:
    /// - InvalidBusinessRuleException: Email exists, weak password
    /// - ValidationException: Bad email format, missing fields
    /// </summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Login user and generate tokens.
    /// 
    /// PROCESS:
    /// 1. Find user by email
    /// 2. Verify password (Bcrypt)
    /// 3. Generate access token (JWT, 15 min)
    /// 4. Generate refresh token (random string, 7 days)
    /// 5. Save refresh token to database
    /// 6. Return both tokens to client
    /// 
    /// ACCESS TOKEN:
    /// - Includes claims (user ID, email, role)
    /// - Expires in 15 minutes
    /// - Used in Authorization header
    /// 
    /// REFRESH TOKEN:
    /// - Random string, hashed for storage
    /// - Expires in 7 days
    /// - Can be revoked
    /// - Stored in database
    /// 
    /// CLIENT USAGE:
    /// - Store access token in memory
    /// - Store refresh token in secure httpOnly cookie
    /// - When access token expires, use refresh token to get new one
    /// 
    /// RETURNS:
    /// LoginResponse with:
    /// - accessToken (JWT)
    /// - refreshToken (random string)
    /// - expiresIn (seconds until expiration)
    /// 
    /// THROWS:
    /// - AuthenticationFailedException: Wrong email/password
    /// </summary>
    Task<LoginResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// Refresh access token using refresh token.
    /// 
    /// WORKFLOW:
    /// 1. User's access token expires (15 min)
    /// 2. Client sends refresh token
    /// 3. Server looks up refresh token in database
    /// 4. Verify: Not expired, not revoked
    /// 5. Get associated user
    /// 6. Generate new access token
    /// 7. Optionally: Generate new refresh token (rotation)
    /// 8. Return new access token
    /// 
    /// WHY REFRESH TOKENS?
    /// - Access tokens short-lived (15 min)
    /// - User doesn't have to re-login every 15 minutes
    /// - Refresh tokens stored in database (can be revoked)
    /// - If access token stolen, only works 15 minutes
    /// - If refresh token stolen, can revoke it immediately
    /// 
    /// SECURITY: Token Rotation
    /// When client refreshes:
    /// - Old refresh token invalidated
    /// - New refresh token issued
    /// - If attacker replays old token, server detects
    /// - User can immediately revoke suspicious tokens
    /// 
    /// RETURNS:
    /// RefreshTokenResponse with:
    /// - accessToken (new JWT)
    /// - refreshToken (new or same, depends on policy)
    /// - expiresIn (15 * 60 = 900 seconds)
    /// 
    /// THROWS:
    /// - AuthenticationFailedException: Invalid/expired refresh token
    /// </summary>
    Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request);

    /// <summary>
    /// Revoke a refresh token (logout from specific device).
    /// 
    /// MULTI-DEVICE LOGOUT:
    /// User logged in on 3 devices:
    /// - Desktop (refresh token A)
    /// - Mobile (refresh token B)
    /// - Tablet (refresh token C)
    /// 
    /// If mobile is lost, want to logout only from mobile
    /// Don't logout from desktop/tablet
    /// 
    /// PROCESS:
    /// 1. Find refresh token
    /// 2. Mark as revoked (RevokedAt = now)
    /// 3. Save reason (e.g., "User logout")
    /// 4. That device can't refresh anymore
    /// 5. Other devices still work
    /// 
    /// USAGE:
    /// POST /api/auth/logout
    /// Send: refreshToken
    /// Result: That device is logged out
    /// 
    /// ADMIN REVOKE ALL:
    /// Admin can revoke all refresh tokens for a user
    /// Force logout from all devices
    /// </summary>
    Task RevokeRefreshTokenAsync(string refreshToken, string reason);

    /// <summary>
    /// Revoke all refresh tokens for a user (force logout everywhere).
    /// 
    /// USE CASES:
    /// - User changed password
    /// - User compromised account
    /// - Admin forced logout
    /// - Security incident
    /// 
    /// RESULT:
    /// User logged out from ALL devices
    /// Must login again
    /// </summary>
    Task RevokeAllRefreshTokensAsync(Guid userId, string reason);
}
