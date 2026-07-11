namespace AIKnowledgeAssistant.API.Controllers;

using System.Security.Claims;
using AIKnowledgeAssistant.Application.DTOs.Documents;
using AIKnowledgeAssistant.Application.Interfaces;
using AIKnowledgeAssistant.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Document management endpoints.
///
/// HTTP ENDPOINTS:
///   POST   /api/documents/upload   - upload a PDF/Word/TXT file
///   GET    /api/documents          - list the current user's documents
///   GET    /api/documents/{id}     - get one document (if owned)
///   DELETE /api/documents/{id}     - delete a document (if owned)
///
/// [Authorize] on the class means EVERY endpoint requires a valid JWT access
/// token. The user's identity is read from that token's claims — the client
/// never tells us "who they are" in the body, which would be trivially forged.
///
/// This controller is deliberately THIN: it converts HTTP ↔ service calls and
/// nothing else. Validation and business errors are thrown by the service and
/// turned into proper HTTP responses by ErrorHandlingMiddleware.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentService documentService,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a document. Sent as multipart/form-data with a single "file" part.
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(60_000_000)] // ~57 MB, a bit above our 50 MB business limit
    public async Task<ActionResult<DocumentDto>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        // Basic presence check; deeper rules (type/size) live in the service.
        if (file is null || file.Length == 0)
            throw new InvalidBusinessRuleException("No file was uploaded, or the file is empty.");

        var userId = GetUserId();
        _logger.LogInformation("Upload attempt: {FileName} ({Size} bytes) by {UserId}",
            file.FileName, file.Length, userId);

        // OpenReadStream() hands us the uploaded bytes as a Stream without
        // buffering the whole file into memory.
        await using var stream = file.OpenReadStream();

        var document = await _documentService.UploadAsync(
            userId, stream, file.FileName, file.Length, cancellationToken);

        // 201 Created + a Location header pointing at the new resource.
        return CreatedAtAction(nameof(GetById), new { id = document.Id }, document);
    }

    /// <summary>List all documents owned by the current user.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var documents = await _documentService.GetUserDocumentsAsync(userId, cancellationToken);
        return Ok(documents);
    }

    /// <summary>Get a single document by id (only if the current user owns it).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var document = await _documentService.GetByIdAsync(userId, id, cancellationToken);
        return Ok(document);
    }

    /// <summary>Delete a document (only if the current user owns it).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await _documentService.DeleteAsync(userId, id, cancellationToken);
        _logger.LogInformation("Document {DocumentId} deleted by {UserId}", id, userId);

        // 204 No Content: success, nothing to return.
        return NoContent();
    }

    /// <summary>
    /// Read the authenticated user's id from the JWT's NameIdentifier claim.
    /// The JwtTokenGenerator put it there at login time.
    /// </summary>
    private Guid GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var userId))
            throw new AuthenticationFailedException("Invalid or missing user identity in token.");
        return userId;
    }
}
