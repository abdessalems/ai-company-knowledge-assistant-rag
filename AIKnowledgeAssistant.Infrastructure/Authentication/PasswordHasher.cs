namespace AIKnowledgeAssistant.Infrastructure.Authentication;

using BCrypt.Net;

/// <summary>
/// Password hashing service interface.
/// 
/// WHY ABSTRACT PASSWORD HASHING?
/// - Can swap implementation (Bcrypt → Argon2 → PBKDF2)
/// - Testable: Mock with fake hasher
/// - Follows SOLID: Depend on abstraction
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hash a plaintext password for storage.
    /// 
    /// IMPORTANT: One-way function!
    /// Can't reverse the hash to get password
    /// 
    /// EXAMPLE:
    /// password = "MySecurePassword123!"
    /// hash = HashPassword(password)
    /// result = "$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcg7b3XeKeUxWdeS86AGR0Hs/WK"
    /// 
    /// Hash is always different even for same password:
    /// HashPassword("password") → "$2a$11$abc..."
    /// HashPassword("password") → "$2a$11$xyz..."
    /// Different each time! (includes random salt)
    /// 
    /// PROCESS:
    /// 1. Take password: "MyPassword123"
    /// 2. Generate random salt
    /// 3. Hash password + salt
    /// 4. Return: salt + hash
    /// 5. Store in database
    /// 
    /// WHY SALT?
    /// - Prevents rainbow tables (precomputed hashes)
    /// - Same password → different hash each time
    /// - Brute force takes ~100x longer
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verify plaintext password against stored hash.
    /// 
    /// PROCESS:
    /// 1. User enters password: "MyPassword123"
    /// 2. Fetch stored hash from database
    /// 3. Call VerifyPassword(entered, stored)
    /// 4. Returns true/false
    /// 
    /// EXAMPLE:
    /// // During login
    /// var user = await _repo.GetByEmailAsync(email);
    /// if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
    ///     throw new AuthenticationFailedException("Wrong password");
    /// 
    /// SECURITY:
    /// - Bcrypt is deliberately slow
    /// - Takes ~100-300ms to hash (intentional!)
    /// - Prevents brute force: Can't try 1000 passwords/sec
    /// - Rainbow table attacks impossible (includes salt)
    /// </summary>
    bool VerifyPassword(string password, string hash);
}

/// <summary>
/// Bcrypt password hashing implementation.
/// 
/// WHY BCRYPT?
/// - Industry standard for password hashing
/// - Designed for passwords (not like SHA256)
/// - Adjustable work factor (cost parameter)
/// - Built-in salt handling
/// - Time-hardened: Deliberately slow
/// 
/// COMPARISON:
/// SHA256: Fast (bad for passwords)
///   - Hashes 1 million times/second
///   - Brute force: Try all 8-character passwords in ~10 hours
/// 
/// Bcrypt: Slow (good for passwords)
///   - Hashes ~100 times/second
///   - Brute force: Try all 8-character passwords in ~100 days
///   - Algorithm improves: Cost factor can increase with hardware
/// 
/// NUGET PACKAGE:
/// BCrypt.Net-Next (recommended, maintained actively)
/// Provides: BCrypt static methods, IPasswordHasher support
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    /// <summary>
    /// Work factor for Bcrypt.
    /// 
    /// RANGE: 4 to 31 (typically 10-12)
    /// 
    /// WHAT IS WORK FACTOR?
    /// Number of rounds of hashing
    /// Cost = 2^workFactor
    /// 
    /// EXAMPLES:
    /// - workFactor 10: 1024 rounds (typical)
    /// - workFactor 11: 2048 rounds (more secure)
    /// - workFactor 12: 4096 rounds (very slow)
    /// 
    /// TIME VS SECURITY:
    /// - workFactor 10: ~100ms per hash
    /// - workFactor 11: ~200ms per hash
    /// - workFactor 12: ~400ms per hash
    /// 
    /// AS HARDWARE IMPROVES:
    /// Can increase work factor
    /// 2010: workFactor 10 was secure
    /// 2024: workFactor 12 recommended
    /// 2040: workFactor 13 might be needed
    /// 
    /// BACKWARDS COMPATIBLE:
    /// Old bcrypt(10) hashes still verify with bcrypt(12)
    /// But new passwords use the higher cost
    /// </summary>
    private const int WorkFactor = 11;

    /// <summary>
    /// Hash a plaintext password using Bcrypt.
    /// 
    /// BCRYPT PROCESS:
    /// 1. Generate random salt
    /// 2. Combine password + salt
    /// 3. Apply hash function iteratively (2^workFactor times)
    /// 4. Return: $2a$11$[salt+hash]
    /// 
    /// IMPLEMENTATION:
    /// Uses BCrypt.Net library
    /// Handles all the cryptography
    /// </summary>
    public string HashPassword(string password)
    {
        // BCrypt.Net-Next handles salt generation automatically
        // WorkFactor is included in the hash string
        // Format: $2a$11$salt...hash...
        return BCrypt.HashPassword(password, workFactor: WorkFactor);
    }

    /// <summary>
    /// Verify plaintext password against Bcrypt hash.
    /// 
    /// BCRYPT VERIFICATION:
    /// 1. Extract salt from stored hash
    /// 2. Hash entered password with same salt
    /// 3. Compare hashes
    /// 4. Return true/false
    /// 
    /// TIMING ATTACK PROTECTION:
    /// BCrypt is designed to prevent timing attacks
    /// Time taken is consistent regardless of match result
    /// Prevents: Hacker determining correct passwords by measuring response time
    /// </summary>
    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Verify(password, hash);
        }
        catch
        {
            // If hash is invalid format, return false
            return false;
        }
    }
}
