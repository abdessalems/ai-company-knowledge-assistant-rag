namespace AIKnowledgeAssistant.Application.Validators;

using FluentValidation;
using AIKnowledgeAssistant.Application.DTOs.Authentication;

/// <summary>
/// Validator for user registration requests.
/// 
/// FLUENTVALIDATION:
/// Type-safe validation rules in code
/// Alternative to Data Annotations (attributes)
/// 
/// WHY FLUENTVALIDATION?
/// - Composable rules
/// - Reusable validators
/// - Can inject dependencies (database checks)
/// - Better error messages
/// - Testing friendly
/// 
/// EXAMPLE:
/// var validator = new RegisterRequestValidator();
/// var result = await validator.ValidateAsync(request);
/// if (!result.IsValid)
/// {
///     var errors = result.Errors;
///     // Handle errors
/// }
/// </summary>
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        // ====================================
        // EMAIL VALIDATION
        // ====================================
        
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            
            .EmailAddress()
            .WithMessage("Email must be valid format")
            
            .MaximumLength(255)
            .WithMessage("Email cannot exceed 255 characters");

        // ====================================
        // PASSWORD VALIDATION
        // ====================================
        
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            
            // Minimum length for security
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters")
            
            // Maximum length (reasonable limit)
            .MaximumLength(128)
            .WithMessage("Password cannot exceed 128 characters")
            
            // Must contain uppercase
            .Matches("[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter")
            
            // Must contain lowercase
            .Matches("[a-z]")
            .WithMessage("Password must contain at least one lowercase letter")
            
            // Must contain digit
            .Matches("[0-9]")
            .WithMessage("Password must contain at least one number")
            
            // Must contain special character
            .Matches("[!@#$%^&*()_+=\\-\\[\\]{};:'\",.<>?/\\\\|`~]")
            .WithMessage("Password must contain at least one special character (!@#$%^&* etc)");

        // ====================================
        // FIRST NAME VALIDATION
        // ====================================
        
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required")
            
            .MaximumLength(100)
            .WithMessage("First name cannot exceed 100 characters")
            
            // Only letters and hyphens (supports names like Jean-Pierre)
            .Matches("^[a-zA-Z\\-'\\s]+$")
            .WithMessage("First name can only contain letters, hyphens, apostrophes, and spaces");

        // ====================================
        // LAST NAME VALIDATION
        // ====================================
        
        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required")
            
            .MaximumLength(100)
            .WithMessage("Last name cannot exceed 100 characters")
            
            .Matches("^[a-zA-Z\\-'\\s]+$")
            .WithMessage("Last name can only contain letters, hyphens, apostrophes, and spaces");
    }
}

/// <summary>
/// Validator for user login requests.
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        // ====================================
        // EMAIL VALIDATION
        // ====================================
        
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            
            .EmailAddress()
            .WithMessage("Email must be valid format");

        // ====================================
        // PASSWORD VALIDATION
        // ====================================
        
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required");

        // ====================================
        // DEVICE IDENTIFIER (optional)
        // ====================================
        
        RuleFor(x => x.DeviceIdentifier)
            .MaximumLength(255)
            .WithMessage("Device identifier cannot exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.DeviceIdentifier));
    }
}

/// <summary>
/// Validator for refresh token requests.
/// </summary>
public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        // ====================================
        // REFRESH TOKEN VALIDATION
        // ====================================
        
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required")
            
            .MinimumLength(64)
            .WithMessage("Refresh token format is invalid")
            
            .MaximumLength(256)
            .WithMessage("Refresh token format is invalid");
    }
}

/// <summary>
/// Validator for logout requests.
/// </summary>
public class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required")
            
            .MinimumLength(64)
            .WithMessage("Refresh token format is invalid");
    }
}
