namespace AIKnowledgeAssistant.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-level errors.
/// 
/// WHY A BASE EXCEPTION CLASS?
/// - Allows catching all domain errors: catch (DomainException)
/// - Separates domain errors from infrastructure errors
/// - Makes it clear where errors originate (business logic, not database)
/// - In API layer, we can handle all domain errors the same way
/// 
/// EXAMPLE USAGE IN API:
/// try
/// {
///     await _userService.LoginAsync(email, password);
/// }
/// catch (DomainException ex)
/// {
///     return Unauthorized(ex.Message); // Always 401 for domain errors
/// }
/// catch (Exception ex)
/// {
///     return StatusCode(500, "Internal error"); // Unexpected errors
/// }
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Thrown when a user tries to access a resource they don't own.
/// 
/// EXAMPLE: Employee A tries to delete Employee B's document
/// 
/// This is a business rule: "Users can only access their own resources"
/// </summary>
public class UnauthorizedAccessException : DomainException
{
    public UnauthorizedAccessException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a resource is not found.
/// 
/// EXAMPLE: Document ID = 123 doesn't exist in database
/// 
/// Why not use System.InvalidOperationException?
/// - Domain exception is clearer about intent
/// - Easier to handle in middleware
/// - Consistent with domain error handling
/// </summary>
public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, object key) 
        : base($"{entityName} with key '{key}' was not found.") { }
}

/// <summary>
/// Thrown when business rule validation fails.
/// 
/// EXAMPLES:
/// - Uploading a file larger than 50MB
/// - Creating a user with an invalid email
/// - DocumentChunk with empty content
/// 
/// This separates domain validation errors from model validation errors
/// </summary>
public class InvalidBusinessRuleException : DomainException
{
    public InvalidBusinessRuleException(string message) : base(message) { }
}

/// <summary>
/// Thrown when authentication fails.
/// 
/// EXAMPLES:
/// - Wrong password
/// - Invalid credentials
/// - Account locked or disabled
/// </summary>
public class AuthenticationFailedException : DomainException
{
    public AuthenticationFailedException(string message) : base(message) { }
}
