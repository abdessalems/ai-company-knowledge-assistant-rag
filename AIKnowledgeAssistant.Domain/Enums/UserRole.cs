namespace AIKnowledgeAssistant.Domain.Enums;

/// <summary>
/// User role enumeration for authorization and access control.
/// 
/// WHY WE USE ENUMS:
/// - Enums provide type-safety (can't accidentally use invalid roles)
/// - Compile-time checking (catch errors before runtime)
/// - IntelliSense support (IDE shows available options)
/// - Database performance (stores as int, not string)
/// 
/// BUSINESS RULES:
/// - Admin: Can manage documents and other users
/// - User: Can only manage their own documents
/// 
/// NOTE: This is an enum, not just string values, because:
/// - A user MUST have a role (can't be null)
/// - Only these two values are allowed
/// - Strongly typed (fewer bugs)
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Regular user - can upload documents and ask questions.
    /// </summary>
    User = 0,

    /// <summary>
    /// Administrator - can manage users, documents, and system configuration.
    /// </summary>
    Admin = 1
}
