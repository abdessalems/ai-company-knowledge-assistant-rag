namespace AIKnowledgeAssistant.Infrastructure.Authentication;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using AIKnowledgeAssistant.Domain.Entities;

/// <summary>
/// JWT token generator interface.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generate a JWT access token for a user.
    /// 
    /// CLAIMS INCLUDED:
    /// - sub (subject): User ID
    /// - email: User email
    /// - name: User full name
    /// - role: User role (User or Admin)
    /// - iat (issued at): When token was created
    /// - exp (expiration): When token expires
    /// - iss (issuer): Token creator
    /// - aud (audience): Who can use token
    /// 
    /// RESULT:
    /// JWT token string (e.g., "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...")
    /// 
    /// CLIENT USAGE:
    /// Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
    /// </summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Verify and parse a JWT token.
    /// 
    /// VERIFICATION:
    /// - Check signature (proves it's valid)
    /// - Check expiration (not expired)
    /// - Check issuer and audience (correct origin)
    /// 
    /// RETURNS:
    /// ClaimsPrincipal with all claims extracted
    /// 
    /// THROWS:
    /// SecurityTokenException if token invalid/expired
    /// 
    /// EXAMPLE:
    /// try
    /// {
    ///     var principal = _generator.ValidateToken(tokenString);
    ///     var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    /// }
    /// catch (SecurityTokenException ex)
    /// {
    ///     // Token invalid or expired
    ///     throw new UnauthorizedAccessException("Invalid token");
    /// }
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);
}

/// <summary>
/// JWT token generator implementation using System.IdentityModel.Tokens.Jwt.
/// 
/// LIBRARY:
/// System.IdentityModel.Tokens.Jwt (Microsoft official)
/// - Industry standard
/// - Used by Azure AD, OAuth2, OpenID Connect
/// - Actively maintained
/// 
/// PROCESS:
/// 1. Create SecurityTokenDescriptor with claims
/// 2. Create JwtSecurityTokenHandler
/// 3. Call CreateToken()
/// 4. Result is JwtSecurityToken
/// 5. Convert to string (header.payload.signature)
/// 
/// SECURITY:
/// - Sign with HMAC-SHA256 (symmetric)
/// - Uses SecretKey from JwtSettings
/// - Only server can create/verify tokens
/// </summary>
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(JwtSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Generate access token.
    /// 
    /// PROCESS:
    /// 1. Create claims (user info)
    /// 2. Set expiration (15 minutes)
    /// 3. Sign with secret key
    /// 4. Return token string
    /// </summary>
    public string GenerateAccessToken(User user)
    {
        // ====================================
        // CREATE CLAIMS
        // ====================================
        // Claims are key-value pairs of user data
        
        var claims = new List<Claim>
        {
            // Subject: User ID (standard claim)
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            
            // Email (standard claim)
            new Claim(ClaimTypes.Email, user.Email),
            
            // Name (standard claim)
            new Claim(ClaimTypes.Name, user.GetDisplayName()),
            
            // Role (custom claim, used for authorization)
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        // ====================================
        // CREATE SECURITY KEY
        // ====================================
        // Convert secret key string to bytes
        // Used for HMAC-SHA256 signing
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // ====================================
        // CREATE TOKEN DESCRIPTOR
        // ====================================
        // Describes everything about the token
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            SigningCredentials = creds
        };

        // ====================================
        // CREATE JWT TOKEN
        // ====================================
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        // Convert to string (Header.Payload.Signature)
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Validate and parse token.
    /// 
    /// SECURITY VALIDATIONS:
    /// 1. Signature valid (using secret key)
    /// 2. Not expired
    /// 3. Issuer matches
    /// 4. Audience matches
    /// 
    /// RETURNS:
    /// ClaimsPrincipal with user claims
    /// Null if token invalid
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));

            var tokenHandler = new JwtSecurityTokenHandler();

            // ValidateToken throws if invalid
            // Returns ClaimsPrincipal if valid
            var principal = tokenHandler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _settings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // Strict expiration
                },
                out SecurityToken validatedToken
            );

            return principal;
        }
        catch (SecurityTokenException)
        {
            // Token invalid, expired, or tampered with
            return null;
        }
        catch (Exception)
        {
            // Other errors (invalid format, etc.)
            return null;
        }
    }
}
