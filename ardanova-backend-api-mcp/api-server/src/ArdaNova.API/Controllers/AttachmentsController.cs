namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controller for file attachment and storage operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AttachmentsController : ControllerBase
{
    private readonly IAttachmentService _attachmentService;
    private readonly IStorageService _storageService;

    public AttachmentsController(
        IAttachmentService attachmentService,
        IStorageService storageService)
    {
        _attachmentService = attachmentService;
        _storageService = storageService;
    }

    /// <summary>
    /// Get all attachments (paginated)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _attachmentService.GetPagedAsync(page, pageSize, ct);
        return ToActionResult(result);
    }

    /// <summary>
    /// Get attachment by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _attachmentService.GetByIdAsync(id, ct);
        return ToActionResult(result);
    }

    /// <summary>
    /// Get attachments by user ID
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(string userId, CancellationToken ct)
    {
        var result = await _attachmentService.GetByUserIdAsync(userId, ct);
        return ToActionResult(result);
    }

    /// <summary>
    /// Get a presigned URL for uploading a file
    /// </summary>
    [HttpPost("upload-url")]
    public async Task<IActionResult> GetUploadUrl(
        [FromBody] UploadRequestDto request,
        [FromHeader(Name = "X-User-Id")] string? userId,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { error = "X-User-Id header is required" });
        }

        var result = await _storageService.GetPresignedUploadUrlAsync(request, userId, ct);
        return ToActionResult(result);
    }

    /// <summary>
    /// Get presigned URLs for bulk file upload
    /// </summary>
    [HttpPost("upload-urls")]
    public async Task<IActionResult> GetUploadUrls(
        [FromBody] BulkUploadRequestDto request,
        [FromHeader(Name = "X-User-Id")] string? userId,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { error = "X-User-Id header is required" });
        }

        var result = await _storageService.GetPresignedUploadUrlsAsync(request, userId, ct);
        return ToActionResult(result);
    }

    /// <summary>
    /// Get a presigned URL for downloading a file
    /// </summary>
    [HttpGet("{id}/download-url")]
    public async Task<IActionResult> GetDownloadUrl(
        string id,
        [FromQuery] int expirationMinutes = 60,
        CancellationToken ct = default)
    {
        // First get the attachment to get the bucket path
        var attachmentResult = await _attachmentService.GetByIdAsync(id, ct);
        if (attachmentResult.IsFailure)
        {
            return ToActionResult(attachmentResult);
        }

        var attachment = attachmentResult.Value!;
        if (string.IsNullOrEmpty(attachment.BucketPath))
        {
            return NotFound(new { error = "Attachment has no associated file" });
        }

        var result = await _storageService.GetPresignedDownloadUrlAsync(
            attachment.BucketPath,
            attachment.FileName,
            expirationMinutes,
            ct);

        // Update last used timestamp
        if (result.IsSuccess)
        {
            await _attachmentService.UpdateLastUsedAsync(id, ct);
        }

        return ToActionResult(result);
    }

    /// <summary>
    /// Register an uploaded file as an attachment
    /// Called after successful upload to presigned URL
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAttachmentDto dto,
        CancellationToken ct)
    {
        // Verify the file exists in storage
        var existsResult = await _storageService.ExistsAsync(dto.BucketPath, ct);
        if (existsResult.IsFailure)
        {
            return BadRequest(new { error = existsResult.Error });
        }

        if (!existsResult.Value)
        {
            return BadRequest(new { error = "File not found in storage. Upload may have failed." });
        }

        var result = await _attachmentService.CreateAsync(dto, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    /// <summary>
    /// Delete an attachment (removes both database record and file from storage)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        // Get the attachment first to get the bucket path
        var attachmentResult = await _attachmentService.GetByIdAsync(id, ct);
        if (attachmentResult.IsFailure)
        {
            return ToActionResult(attachmentResult);
        }

        var attachment = attachmentResult.Value!;

        // Delete from storage if bucket path exists
        if (!string.IsNullOrEmpty(attachment.BucketPath))
        {
            var storageDeleteResult = await _storageService.DeleteAsync(attachment.BucketPath, ct);
            if (storageDeleteResult.IsFailure)
            {
                // Log but don't fail - orphaned files can be cleaned up later
                // The database record should still be deleted
            }
        }

        // Delete the database record
        var result = await _attachmentService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    /// <summary>
    /// Direct file upload (server-side upload, returns attachment record)
    /// For small files or when presigned URLs aren't suitable
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(52_428_800)] // 50MB limit for direct uploads
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromHeader(Name = "X-User-Id")] string? userId,
        [FromQuery] string? folder,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { error = "X-User-Id header is required" });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file provided" });
        }

        await using var stream = file.OpenReadStream();
        var uploadResult = await _storageService.UploadAsync(
            stream,
            file.FileName,
            file.ContentType,
            folder,
            ct);

        if (uploadResult.IsFailure)
        {
            return ToActionResult(uploadResult);
        }

        // Create the attachment record
        var attachmentDto = new CreateAttachmentDto
        {
            UploadedById = userId,
            BucketPath = uploadResult.Value!,
            Type = _storageService.GetMimeType(file.ContentType),
            FileName = file.FileName,
            FileSize = file.Length
        };

        var attachmentResult = await _attachmentService.CreateAsync(attachmentDto, ct);
        return attachmentResult.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = attachmentResult.Value!.Id }, attachmentResult.Value)
            : ToActionResult(attachmentResult);
    }

    /// <summary>
    /// Get the public URL for an attachment
    /// </summary>
    [HttpGet("{id}/public-url")]
    public async Task<IActionResult> GetPublicUrl(string id, CancellationToken ct)
    {
        var attachmentResult = await _attachmentService.GetByIdAsync(id, ct);
        if (attachmentResult.IsFailure)
        {
            return ToActionResult(attachmentResult);
        }

        var attachment = attachmentResult.Value!;
        if (string.IsNullOrEmpty(attachment.BucketPath))
        {
            return NotFound(new { error = "Attachment has no associated file" });
        }

        var publicUrl = _storageService.GetPublicUrl(attachment.BucketPath);
        return Ok(new { url = publicUrl });
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Type switch
        {
            ResultType.NotFound => NotFound(new { error = result.Error }),
            ResultType.ValidationError => BadRequest(new { error = result.Error }),
            ResultType.Unauthorized => Unauthorized(new { error = result.Error }),
            ResultType.Forbidden => Forbid(),
            _ => BadRequest(new { error = result.Error })
        };
    }
}
