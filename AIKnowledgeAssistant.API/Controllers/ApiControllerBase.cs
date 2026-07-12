namespace AIKnowledgeAssistant.API.Controllers;

using System.Security.Claims;
using AIKnowledgeAssistant.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Base class for authenticated API controllers. Provides shared helpers so
/// individual controllers stay thin and don't repeat the same code.
/// </summary>
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// The authenticated user's id, read from the JWT's NameIdentifier claim.
    /// </summary>
    protected Guid GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var userId))
            throw new AuthenticationFailedException("Invalid or missing user identity in token.");
        return userId;
    }
}
