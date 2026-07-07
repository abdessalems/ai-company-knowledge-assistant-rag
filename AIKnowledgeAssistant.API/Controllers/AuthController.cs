namespace AIKnowledgeAssistant.API.Controllers;

using AIKnowledgeAssistant.Application.DTOs.Authentication;
using AIKnowledgeAssistant.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Authentication controller.
/// 
/// HTTP ENDPOINTS:
/// POST /api/auth/register - Create new user account
/// POST /api/auth/login - Login and get tokens
/// POST /api/auth/refresh-token - Get new access token
/// POST /api/auth/logout - Revoke refresh token
/// 
/// CLIENT WORKFLOW:
/// 1. POST /api/auth/register → Create account
/// 2. POST /api/auth/login → Get tokens
/// 3. Store access token in memory
/// 4. Store refresh token in HttpOnly cookie
/// 5. Use access token in requests (Authorization header)
/// 6. When access token expires (15 min):
///    POST /api/auth/refresh-token → Get new access token
/// 7. POST /api/auth/logout → Revoke token (optional)
/// 
/// SECURITY:
/// - Passwords sent over HTTPS only
/// - Tokens sent in Authorization header (HTTPS)
/// - Refresh tokens in HttpOnly cookies (JS can't access)
/// - Never send sensitive data in logs
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthenticationService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account.
    /// 
    /// HTTP: POST /api/auth/register
    /// 
    /// REQUEST BODY:
    /// {
    ///   "email": "user@company.com",
    ///   "password": "MySecurePassword123!",
    ///   "firstName": "John",
    ///   "lastName": "Doe"
    /// }
    /// 
    /// RESPONSE: 200 OK
    /// {
    ///   "userId": "123e4567-e89b-12d3-a456-426614174000",
    ///   "email": "user@company.com",
    ///   "fullName": "John Doe",
    ///   "role": "User",
    ///   "message": "Registration successful. Please login to continue."
    /// }
    /// 
    /// VALIDATION:
    /// - Email must be valid format
    /// - Email must be unique
    /// - Password must be strong
    /// 
    /// ERRORS:
    /// - 400: Invalid input (email format, weak password)
    /// - 409: Email already registered
    /// 
    /// NOTE:
    /// Registration returns user info, NOT tokens
    /// User must call /login to get tokens
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            _logger.LogInformation("Registration attempt for email: {Email}", request.Email);

            var response = await _authService.RegisterAsync(request);

            _logger.LogInformation("User registered successfully: {UserId}", response.UserId);

            return CreatedAtAction(nameof(Register), response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for email: {Email}", request.Email);
            
            return BadRequest(new
            {
                message = "Registration failed",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Login user and get authentication tokens.
    /// 
    /// HTTP: POST /api/auth/login
    /// 
    /// REQUEST BODY:
    /// {
    ///   "email": "user@company.com",
    ///   "password": "MySecurePassword123!",
    ///   "deviceIdentifier": "Chrome on Windows 10"
    /// }
    /// 
    /// RESPONSE: 200 OK
    /// {
    ///   "userId": "123e4567-e89b-12d3-a456-426614174000",
    ///   "email": "user@company.com",
    ///   "fullName": "John Doe",
    ///   "role": "User",
    ///   "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    ///   "refreshToken": "abcdef1234567890abcdef1234567890...",
    ///   "expiresIn": 900,
    ///   "message": "Login successful"
    /// }
    /// 
    /// TOKEN STORAGE (CLIENT):
    /// - accessToken: In memory (lost on page refresh)
    /// - refreshToken: HttpOnly cookie (survives page refresh)
    /// 
    /// TOKEN USAGE:
    /// Authorization header:
    /// "Authorization": "Bearer {accessToken}"
    /// 
    /// ERRORS:
    /// - 401: Invalid email/password
    /// - 400: Invalid input
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Login attempt for email: {Email}", request.Email);

            var response = await _authService.LoginAsync(request);

            // Set refresh token as HttpOnly cookie
            Response.Cookies.Append(
                "refreshToken",
                response.RefreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // HTTPS only
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                }
            );

            _logger.LogInformation("User logged in: {UserId}", response.UserId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for email: {Email}", request.Email);

            return Unauthorized(new
            {
                message = "Login failed",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token.
    /// 
    /// HTTP: POST /api/auth/refresh-token
    /// 
    /// REQUEST BODY:
    /// {
    ///   "refreshToken": "abcdef1234567890abcdef1234567890..."
    /// }
    /// 
    /// NOTE:
    /// Client can also send refresh token from cookie
    /// Browser automatically sends cookies with requests
    /// 
    /// RESPONSE: 200 OK
    /// {
    ///   "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    ///   "refreshToken": "xyz1234567890xyz1234567890...",
    ///   "expiresIn": 900
    /// }
    /// 
    /// WORKFLOW:
    /// 1. Access token expires (15 min)
    /// 2. Client sends refresh token
    /// 3. Server validates refresh token
    /// 4. Server generates new access token
    /// 5. Server rotates refresh token
    /// 6. Response: New access token + new refresh token
    /// 
    /// TOKEN ROTATION:
    /// Old refresh token is invalidated
    /// If attacker replays old token, server detects
    /// 
    /// ERRORS:
    /// - 401: Invalid/expired refresh token
    /// - 400: Invalid input
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            _logger.LogInformation("Token refresh attempt");

            var response = await _authService.RefreshTokenAsync(request);

            // Update refresh token cookie
            Response.Cookies.Append(
                "refreshToken",
                response.RefreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                }
            );

            _logger.LogInformation("Token refreshed successfully");

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");

            return Unauthorized(new
            {
                message = "Token refresh failed",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Logout user (revoke refresh token).
    /// 
    /// HTTP: POST /api/auth/logout
    /// 
    /// REQUEST BODY:
    /// {
    ///   "refreshToken": "abcdef1234567890abcdef1234567890..."
    /// }
    /// 
    /// RESPONSE: 200 OK
    /// {
    ///   "message": "Logout successful"
    /// }
    /// 
    /// WORKFLOW:
    /// 1. Client sends refresh token
    /// 2. Server marks refresh token as revoked
    /// 3. That device can't refresh anymore
    /// 4. Other devices still work
    /// 
    /// CLIENT WORKFLOW:
    /// 1. User clicks Logout
    /// 2. Delete access token from memory
    /// 3. GET refresh token from cookie
    /// 4. POST /api/auth/logout with refresh token
    /// 5. Delete refresh token cookie
    /// 6. Redirect to login page
    /// 
    /// AUTHORIZATION:
    /// [Authorize] means:
    /// - Requires valid access token in Authorization header
    /// - Request fails if token missing/invalid
    /// - Only authenticated users can logout
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout([FromBody] LogoutRequest request)
    {
        try
        {
            _logger.LogInformation("Logout attempt");

            await _authService.RevokeRefreshTokenAsync(request.RefreshToken, "User logout");

            // Clear refresh token cookie
            Response.Cookies.Delete("refreshToken");

            _logger.LogInformation("User logged out successfully");

            return Ok(new { message = "Logout successful" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed");

            return BadRequest(new
            {
                message = "Logout failed",
                error = ex.Message
            });
        }
    }
}
