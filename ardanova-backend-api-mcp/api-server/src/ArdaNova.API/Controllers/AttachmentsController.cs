namespace ArdaNova.API.Controllers;

using System;
using System.Linq;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Common.Security;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// File attachment and storage routes. Every one is bound to the calling actor: the owner comes
/// from the verified actor claim, never from a request header or a request body.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
public class AttachmentsController : ControllerBase
{
    /// <summary>Why the multipart route cannot establish an uploader, and what to use instead.</summary>
    public const string MultipartUploadRetiredMessage =
        "Direct multipart upload is retired: a multipart body cannot carry an actor assertion, so "
        + "the uploader could not be established. Request an upload URL from "
        + "POST /api/attachments/upload-url instead.";

    /// <summary>A size that is not declared cannot be checked, so it is refused rather than skipped.</summary>
    public const string FileSizeRequiredMessage =
        "fileSize is required and must be greater than zero; an upload whose size is not declared "
        + "cannot be checked against the limit.";

    private const string AttachmentNotFoundMessage = "Attachment not found";

    private readonly IAttachmentService _attachmentService;
    private readonly IStorageService _storageService;

    public AttachmentsController(
        IAttachmentService attachmentService,
        IStorageService storageService)
    {
        _attachmentService = attachmentService;
        _storageService = storageService;
    }

    /// <summary>The caller's own attachments, paginated. There is no parameter for anyone else's.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var owned = await _attachmentService.GetByUserIdAsync(ActorId, ct);
        if (owned.IsFailure)
        {
            return ToActionResult(owned);
        }

        var requestedPage = page < 1 ? 1 : page;
        var requestedPageSize = pageSize < 1 ? 10 : Math.Min(pageSize, 100);
        var items = owned.Value!;
        var pageItems = items
            .Skip((requestedPage - 1) * requestedPageSize)
            .Take(requestedPageSize)
            .ToList();

        return Ok(new PagedResult<AttachmentDto>(pageItems, items.Count, requestedPage, requestedPageSize));
    }

    /// <summary>An attachment the caller uploaded; anyone else's reads as missing, not as forbidden.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _attachmentService.GetByIdAsync(id, ct);
        if (result.IsFailure)
        {
            return ToActionResult(result);
        }

        return UploadedByCaller(result.Value!) ? Ok(result.Value) : AttachmentNotFound();
    }

    /// <summary>Kept for the existing client route; the id must be the caller's own.</summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(string userId, CancellationToken ct)
    {
        if (!string.Equals(userId, ActorId, StringComparison.Ordinal))
        {
            return CallerForbidden("Attachments can only be listed for the signed-in account.");
        }

        return ToActionResult(await _attachmentService.GetByUserIdAsync(ActorId, ct));
    }

    /// <summary>
    /// Mints an upload URL under the CALLING actor's prefix. OI-18: the owner is never taken from
    /// a request header, so a shared API key cannot mint a URL under someone else's name.
    /// </summary>
    [HttpPost("upload-url")]
    public async Task<IActionResult> GetUploadUrl(
        [FromBody] UploadRequestDto request,
        CancellationToken ct)
    {
        if (!HasDeclaredSize(request))
        {
            return BadRequest(new { error = FileSizeRequiredMessage });
        }

        var result = await _storageService.GetPresignedUploadUrlAsync(request, ActorId, ct);
        return ToActionResult(result);
    }

    /// <summary>The bulk form of <see cref="GetUploadUrl"/>, under the same actor and the same cap.</summary>
    [HttpPost("upload-urls")]
    public async Task<IActionResult> GetUploadUrls(
        [FromBody] BulkUploadRequestDto request,
        CancellationToken ct)
    {
        if (request.Files.Count == 0)
        {
            return BadRequest(new { error = "At least one file is required" });
        }

        if (!request.Files.All(HasDeclaredSize))
        {
            return BadRequest(new { error = FileSizeRequiredMessage });
        }

        var result = await _storageService.GetPresignedUploadUrlsAsync(request, ActorId, ct);
        return ToActionResult(result);
    }

    /// <summary>A signed download link for the caller's own attachment.</summary>
    [HttpGet("{id}/download-url")]
    public async Task<IActionResult> GetDownloadUrl(
        string id,
        [FromQuery] int expirationMinutes = 60,
        CancellationToken ct = default)
    {
        var attachmentResult = await _attachmentService.GetByIdAsync(id, ct);
        if (attachmentResult.IsFailure)
        {
            return ToActionResult(attachmentResult);
        }

        var attachment = attachmentResult.Value!;
        if (!UploadedByCaller(attachment))
        {
            return AttachmentNotFound();
        }

        if (string.IsNullOrEmpty(attachment.BucketPath))
        {
            return NotFound(new { error = "Attachment has no associated file" });
        }

        var result = await _storageService.GetPresignedDownloadUrlAsync(
            attachment.BucketPath,
            attachment.FileName,
            expirationMinutes,
            ct);

        if (result.IsSuccess)
        {
            await _attachmentService.UpdateLastUsedAsync(id, ct);
        }

        return ToActionResult(result);
    }

    /// <summary>
    /// Registers an uploaded file. The uploader is the calling actor: a caller-supplied
    /// <c>UploadedById</c> is ignored, and a storage path stamped for another account is refused.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAttachmentDto dto,
        CancellationToken ct)
    {
        var owner = ActorId;
        if (!BucketPathBelongsToCaller(dto.BucketPath, owner))
        {
            return CallerForbidden("That storage path was minted for another account.");
        }

        var existsResult = await _storageService.ExistsAsync(dto.BucketPath, ct);
        if (existsResult.IsFailure)
        {
            return ToActionResult(existsResult);
        }

        if (!existsResult.Value)
        {
            return BadRequest(new { error = "File not found in storage. Upload may have failed." });
        }

        var result = await _attachmentService.CreateAsync(dto with { UploadedById = owner }, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    /// <summary>Deletes the caller's own attachment, record and stored file together.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var attachmentResult = await _attachmentService.GetByIdAsync(id, ct);
        if (attachmentResult.IsFailure)
        {
            return ToActionResult(attachmentResult);
        }

        var attachment = attachmentResult.Value!;
        if (!UploadedByCaller(attachment))
        {
            return AttachmentNotFound();
        }

        if (!string.IsNullOrEmpty(attachment.BucketPath))
        {
            // An orphaned file is swept later; the record must still go.
            await _storageService.DeleteAsync(attachment.BucketPath, ct);
        }

        var result = await _attachmentService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    /// <summary>
    /// Refuses rather than uploading. An actor assertion cannot be carried on a multipart body
    /// (ActorAssertionMiddleware rejects multipart outright), so this route had no way to
    /// establish an uploader other than the header OI-18 removed.
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(52_428_800)]
    public IActionResult Upload()
        => StatusCode(StatusCodes.Status501NotImplemented, new { error = MultipartUploadRetiredMessage });

    /// <summary>The public URL of the caller's own attachment, when storage will serve one.</summary>
    [HttpGet("{id}/public-url")]
    public async Task<IActionResult> GetPublicUrl(string id, CancellationToken ct)
    {
        var attachmentResult = await _attachmentService.GetByIdAsync(id, ct);
        if (attachmentResult.IsFailure)
        {
            return ToActionResult(attachmentResult);
        }

        var attachment = attachmentResult.Value!;
        if (!UploadedByCaller(attachment))
        {
            return AttachmentNotFound();
        }

        if (string.IsNullOrEmpty(attachment.BucketPath))
        {
            return NotFound(new { error = "Attachment has no associated file" });
        }

        // Storage returns an empty URL for a path it will not serve; that is a missing file, not a
        // link to nowhere.
        var publicUrl = _storageService.GetPublicUrl(attachment.BucketPath);
        return string.IsNullOrEmpty(publicUrl)
            ? NotFound(new { error = "Attachment has no associated file" })
            : Ok(new { url = publicUrl });
    }

    /// <summary>The authenticated actor. The policy on this controller guarantees one exists.</summary>
    private string ActorId =>
        ActorAssertionMiddleware.TryGetActorId(User, out var actorId) ? actorId : string.Empty;

    private bool UploadedByCaller(AttachmentDto attachment)
        => string.Equals(attachment.UploadedById, ActorId, StringComparison.Ordinal);

    /// <summary>
    /// Whether a storage key was minted for the calling actor. The proof - a matching owner
    /// segment that must be PRESENT, carrying the whole actor id - lives in
    /// <see cref="UserStoragePrefix"/>, the same type the key is minted with, so the two halves
    /// cannot drift. A key with no owner segment is refused rather than shared with everybody.
    /// </summary>
    private static bool BucketPathBelongsToCaller(string bucketPath, string actorId)
        => UserStoragePrefix.PathBelongsTo(bucketPath, actorId);

    private static bool HasDeclaredSize(UploadRequestDto request)
        => request.FileSize is > 0;

    private IActionResult AttachmentNotFound()
        => NotFound(new { error = AttachmentNotFoundMessage });

    private IActionResult CallerForbidden(string error)
        => StatusCode(StatusCodes.Status403Forbidden, new { error });

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Type switch
        {
            ResultType.NotFound => NotFound(new { error = result.Error }),
            ResultType.ValidationError => BadRequest(new { error = result.Error }),
            ResultType.Unauthorized => Unauthorized(new { error = result.Error }),
            ResultType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error }),
            ResultType.Conflict => Conflict(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error })
        };
    }
}
