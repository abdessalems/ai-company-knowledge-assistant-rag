namespace AIKnowledgeAssistant.API.Controllers;

using System.Security.Claims;
using AIKnowledgeAssistant.Application.DTOs.Chat;
using AIKnowledgeAssistant.Application.Interfaces;
using AIKnowledgeAssistant.Application.Validators;
using AIKnowledgeAssistant.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// RAG chat endpoint.
///
///   POST /api/chat/ask  - ask a question, get an answer grounded in your documents
///
/// [Authorize] means a valid JWT is required, and answers are drawn only from the
/// authenticated user's own documents.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly AskRequestValidator _validator;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatService chatService,
        AskRequestValidator validator,
        ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Ask a question about your uploaded documents.</summary>
    [HttpPost("ask")]
    public async Task<ActionResult<ChatResponse>> Ask([FromBody] AskRequest request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new FluentValidation.ValidationException(validation.Errors);

        var userId = GetUserId();
        _logger.LogInformation("Chat question from {UserId}: {Question}", userId, request.Question);

        var response = await _chatService.AskAsync(userId, request.Question, cancellationToken);
        return Ok(response);
    }

    /// <summary>Read the authenticated user's id from the JWT NameIdentifier claim.</summary>
    private Guid GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var userId))
            throw new AuthenticationFailedException("Invalid or missing user identity in token.");
        return userId;
    }
}
