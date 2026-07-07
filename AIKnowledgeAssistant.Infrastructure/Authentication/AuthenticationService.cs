namespace AIKnowledgeAssistant.Infrastructure.Authentication;

using AIKnowledgeAssistant.Application.DTOs.Authentication;
using AIKnowledgeAssistant.Application.Interfaces;
using AIKnowledgeAssistant.Domain.Entities;
using AIKnowledgeAssistant.Domain.Exceptions;

/// <summary>
/// Authentication service implementation.
/// 
/// ORCHESTRATES:
/// - User registration (validation → hashing → database)
/// - User login (lookup → password verification → token generation)
/// - Token refresh (validation → new token generation)
/// - Token revocation (logout)
/// 
/// USES:
/// - IUnitOfWork: Database access
/// - IPasswordHasher: Bcrypt hashing
/// - IJwtTokenGenerator: JWT token creation
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly JwtSettings _jwtSettings;

    public AuthenticationService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        JwtSettings jwtSettings)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _jwtSettings = jwtSettings;
    }

    /// <summary>
    /// Register a new user.
    /// </summary>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // ====================================
        // VALIDATION
        // ====================================
        
        // Check email format (simplified, use FluentValidation in real app)
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains("@"))
            throw new InvalidBusinessRuleException("Invalid email format");

        // Check password strength (simplified)
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            throw new InvalidBusinessRuleException("Password must be at least 8 characters");

        // ====================================
        // CHECK IF EMAIL ALREADY EXISTS
        // ====================================
        
        if (await _unitOfWork.Users.EmailExistsAsync(request.Email))
            throw new InvalidBusinessRuleException("Email already registered");

        // ====================================
        // CREATE USER
        // ====================================
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password), // Hash password!
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = Domain.Enums.UserRole.User, // New users are regular users
            CreatedAt = DateTime.UtcNow
        };

        // ====================================
        // SAVE TO DATABASE
        // ====================================
        
        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // ====================================
        // RETURN SUCCESS
        // ====================================
        
        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.GetDisplayName(),
            Role = user.Role.ToString(),
            Message = "Registration successful. Please login to continue."
        };
    }

    /// <summary>
    /// Login user and generate tokens.
    /// </summary>
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        // ====================================
        // FIND USER BY EMAIL
        // ====================================
        
        var user = await _unitOfWork.Users.GetByEmailWithDetailsAsync(request.Email);
        if (user == null)
            throw new AuthenticationFailedException("Invalid email or password");

        // ====================================
        // VERIFY PASSWORD
        // ====================================
        
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            throw new AuthenticationFailedException("Invalid email or password");

        // ====================================
        // GENERATE ACCESS TOKEN
        // ====================================
        
        var accessToken = _tokenGenerator.GenerateAccessToken(user);

        // ====================================
        // GENERATE REFRESH TOKEN
        // ====================================
        
        var refreshToken = GenerateRefreshTokenString();
        var hashedRefreshToken = _passwordHasher.HashPassword(refreshToken);

        // ====================================
        // SAVE REFRESH TOKEN TO DATABASE
        // ====================================
        
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = hashedRefreshToken,
            ExpirationDate = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            DeviceIdentifier = request.DeviceIdentifier
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity);
        await _unitOfWork.SaveChangesAsync();

        // ====================================
        // RETURN RESPONSE WITH BOTH TOKENS
        // ====================================
        
        return new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.GetDisplayName(),
            Role = user.Role.ToString(),
            AccessToken = accessToken,
            RefreshToken = refreshToken, // Send plaintext to client (they'll send it back)
            ExpiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60,
            Message = "Login successful"
        };
    }

    /// <summary>
    /// Refresh access token using refresh token.
    /// </summary>
    public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        // ====================================
        // FIND ALL REFRESH TOKENS FOR THIS HASH
        // ====================================
        
        // Note: This is simplified. In production:
        // - Store token hash in database
        // - Search by hash
        // - For now, we'll search by comparison
        
        var refreshTokens = await _unitOfWork.RefreshTokens.WhereAsync(t => 
            !t.RevokedAt.HasValue && t.ExpirationDate > DateTime.UtcNow);

        RefreshToken? tokenEntity = null;
        foreach (var token in refreshTokens)
        {
            if (_passwordHasher.VerifyPassword(request.RefreshToken, token.Token))
            {
                tokenEntity = token;
                break;
            }
        }

        if (tokenEntity == null)
            throw new AuthenticationFailedException("Invalid or expired refresh token");

        if (tokenEntity.IsRevoked() || tokenEntity.IsExpired())
            throw new AuthenticationFailedException("Refresh token has been revoked or expired");

        // ====================================
        // GET USER
        // ====================================
        
        var user = await _unitOfWork.Users.GetByIdAsync(tokenEntity.UserId);
        if (user == null)
            throw new EntityNotFoundException("User", tokenEntity.UserId);

        // ====================================
        // GENERATE NEW ACCESS TOKEN
        // ====================================
        
        var newAccessToken = _tokenGenerator.GenerateAccessToken(user);

        // ====================================
        // OPTIONAL: ROTATE REFRESH TOKEN
        // ====================================
        // Mark old token as revoked, issue new one
        
        tokenEntity.Revoke("Token rotated");
        _unitOfWork.RefreshTokens.Update(tokenEntity);

        var newRefreshTokenString = GenerateRefreshTokenString();
        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = _passwordHasher.HashPassword(newRefreshTokenString),
            ExpirationDate = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            DeviceIdentifier = tokenEntity.DeviceIdentifier
        };

        await _unitOfWork.RefreshTokens.AddAsync(newRefreshTokenEntity);
        await _unitOfWork.SaveChangesAsync();

        // ====================================
        // RETURN NEW TOKENS
        // ====================================
        
        return new RefreshTokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshTokenString,
            ExpiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60
        };
    }

    /// <summary>
    /// Revoke a single refresh token (logout from one device).
    /// </summary>
    public async Task RevokeRefreshTokenAsync(string refreshToken, string reason)
    {
        var tokens = await _unitOfWork.RefreshTokens.WhereAsync(t => 
            !t.RevokedAt.HasValue);

        foreach (var token in tokens)
        {
            if (_passwordHasher.VerifyPassword(refreshToken, token.Token))
            {
                token.Revoke(reason);
                _unitOfWork.RefreshTokens.Update(token);
                await _unitOfWork.SaveChangesAsync();
                return;
            }
        }

        // Token not found or already revoked
        // Don't throw error (idempotent operation)
    }

    /// <summary>
    /// Revoke all refresh tokens for a user (logout from all devices).
    /// </summary>
    public async Task RevokeAllRefreshTokensAsync(Guid userId, string reason)
    {
        var tokens = await _unitOfWork.RefreshTokens.WhereAsync(t => 
            t.UserId == userId && !t.RevokedAt.HasValue);

        foreach (var token in tokens)
        {
            token.Revoke(reason);
            _unitOfWork.RefreshTokens.Update(token);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Generate a cryptographically secure random refresh token string.
    /// 
    /// FORMAT:
    /// 64 random bytes → base64 encoded
    /// Result: ~88 character string
    /// Example: "abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890"
    /// 
    /// SECURITY:
    /// - RNGCryptoServiceProvider: Cryptographically secure random
    /// - 64 bytes: 2^512 possible values (impossible to guess)
    /// - One-way: Can't reverse to get original bytes
    /// 
    /// STORAGE:
    /// Hash before storing in database
    /// Even if database stolen, tokens are worthless
    /// </summary>
    private string GenerateRefreshTokenString()
    {
        var randomNumber = new byte[64];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
