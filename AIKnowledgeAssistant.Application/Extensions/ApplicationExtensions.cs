namespace AIKnowledgeAssistant.Application.Extensions;

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using AIKnowledgeAssistant.Application.Validators;
using AIKnowledgeAssistant.Application.DTOs.Authentication;
using AIKnowledgeAssistant.Application.Interfaces;
using AIKnowledgeAssistant.Application.Services;

/// <summary>
/// Extension method for registering Application layer services.
/// 
/// REGISTRATION PATTERN:
/// Domain layer: No DI (pure business logic)
/// Application layer: DTOs, interfaces, validators
/// Infrastructure layer: Implementations, database
/// API layer: Controllers, middleware
/// 
/// Each layer has its own extension method:
/// builder.Services.AddApplication();      // Application validators, mappings
/// builder.Services.AddInfrastructure();   // Database, repositories
/// </summary>
public static class ApplicationExtensions
{
    /// <summary>
    /// Register Application layer services.
    /// 
    /// INCLUDES:
    /// - FluentValidation validators
    /// - AutoMapper (when added)
    /// - Application service interfaces
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // ====================================
        // FLUENT VALIDATION
        // ====================================
        
        /// Register all validators from this assembly
        /// 
        /// SYNTAX:
        /// services.AddValidatorsFromAssembly(Assembly)
        /// 
        /// Scans assembly and auto-registers:
        /// - RegisterRequestValidator implements AbstractValidator<RegisterRequest>
        /// - LoginRequestValidator implements AbstractValidator<LoginRequest>
        /// - Automatically resolves to IValidator<T>
        /// 
        /// BENEFIT:
        /// Add new validators, they're auto-registered
        /// No manual registration needed
        services.AddValidatorsFromAssembly(typeof(RegisterRequestValidator).Assembly, ServiceLifetime.Transient);

        // ====================================
        // APPLICATION SERVICES
        // ====================================
        // Scoped: one instance per HTTP request, sharing that request's
        // UnitOfWork/DbContext so all its DB work is one transaction.
        services.AddScoped<IDocumentService, DocumentService>();

        // Text processing pipeline (stateless → Singleton):
        // - the extraction facade picks the right ITextExtractor
        // - the chunker splits extracted pages into overlapping chunks
        services.AddSingleton<ITextExtractionService, TextExtractionService>();
        services.AddSingleton<ITextChunker, SlidingWindowTextChunker>();

        return services;
    }
}
