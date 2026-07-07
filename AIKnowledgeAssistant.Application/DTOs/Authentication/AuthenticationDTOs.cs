namespace AIKnowledgeAssistant.Application.DTOs.Authentication;

/// <summary>
/// User registration request DTO.
/// 
/// DTO = Data Transfer Object
/// Used to transfer data between layers (API → Application)
/// 
/// WHY SEPARATE FROM ENTITY?
/// - API doesn't send entire User object
/// - Only sends: email, password, first name, last name
/// - Entity has: ID, PasswordHash, CreatedAt (internal)
/// - DTO is schema contract with client
/// 
/// VALIDATION:
/// Applied in Application layer using FluentValidation
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// User's email address (login credential).
    /// Must be valid email format, unique.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's password (plaintext from client).
    /// Will be hashed before storage.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// User's first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;
}

/// <summary>
/// User login request DTO.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password (plaintext from client).
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Device name for tracking.
    /// Example: "Chrome on Windows 10"
    /// Used to track which device is logged in.
    /// </summary>
    public string? DeviceIdentifier { get; set; }
}

/// <summary>
/// Refresh token request DTO.
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// The refresh token (random string).
    /// Client sends this to get new access token.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Registration/Login response DTO.
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// User ID (UUID).
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User's email.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's full name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User's role (for authorization).
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Success message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Login response DTO with tokens.
/// </summary>
public class LoginResponse : AuthResponse
{
    /// <summary>
    /// Access token (JWT).
    /// Send in Authorization header: "Bearer {accessToken}"
    /// Expires in 15 minutes.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token (random string).
    /// Store in httpOnly cookie.
    /// Expires in 7 days.
    /// Use to refresh access token.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiration time (seconds).
    /// Client can use to know when to refresh.
    /// Example: 900 (15 minutes)
    /// </summary>
    public int ExpiresIn { get; set; }
}

/// <summary>
/// Refresh token response DTO with new access token.
/// </summary>
public class RefreshTokenResponse
{
    /// <summary>
    /// New access token (JWT).
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// New refresh token (optional, depends on rotation policy).
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Expiration time in seconds.
    /// </summary>
    public int ExpiresIn { get; set; }
}

/// <summary>
/// Logout request DTO.
/// </summary>
public class LogoutRequest
{
    /// <summary>
    /// The refresh token to revoke.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}
