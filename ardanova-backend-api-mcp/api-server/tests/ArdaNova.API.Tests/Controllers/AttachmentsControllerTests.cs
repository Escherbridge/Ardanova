namespace ArdaNova.API.Tests.Controllers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using ArdaNova.API.Controllers;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

/// <summary>
/// OI-18: the uploader is the authenticated actor, never a request header.
///
/// The old controller took the owner from <c>[FromHeader(Name = "X-User-Id")]</c> and carried no
/// class-level <c>[Authorize]</c>, so any holder of the shared API key could mint a presigned
/// upload URL under any user's prefix. These tests assert the enforcement where it now lives:
/// the storage call receives the ACTOR, a spoofed header changes nothing, and the header binding
/// is structurally gone rather than merely unused.
/// </summary>
public sealed class AttachmentsControllerTests
{
    private const string Actor = "owner-actor";
    private const string Victim = "victim-actor";
    private const string ActorPrefixedPath = "user_owner-ac/20260909/photo_1234abcd.png";

    [Fact]
    public async Task UploadUrl_IsMintedForTheActor_EvenWhenAnXUserIdHeaderNamesSomeoneElse()
    {
        var storage = new Mock<IStorageService>(MockBehavior.Strict);
        var mintedFor = new List<string>();
        storage
            .Setup(service => service.GetPresignedUploadUrlAsync(
                It.IsAny<UploadRequestDto>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<UploadRequestDto, string, CancellationToken>((_, userId, _) => mintedFor.Add(userId))
            .ReturnsAsync(Result<UploadResponseDto>.Success(new UploadResponseDto
            {
                UploadUrl = "https://storage.example/put",
                BucketPath = ActorPrefixedPath,
                PublicUrl = "https://storage.example/get",
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            }));

        var controller = WithActor(CreateController(storage: storage), Actor);
        controller.ControllerContext.HttpContext.Request.Headers["X-User-Id"] = Victim;

        var result = await controller.GetUploadUrl(
            new UploadRequestDto { FileName = "photo.png", ContentType = "image/png", FileSize = 1_024 },
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        mintedFor.Should().ContainSingle().Which.Should().Be(
            Actor,
            "the prefix an upload URL is minted under comes from the verified actor claim");
        mintedFor.Should().NotContain(Victim);
    }

    [Fact]
    public async Task BulkUploadUrls_AreMintedForTheActor_EvenWhenAnXUserIdHeaderNamesSomeoneElse()
    {
        var storage = new Mock<IStorageService>(MockBehavior.Strict);
        var mintedFor = new List<string>();
        storage
            .Setup(service => service.GetPresignedUploadUrlsAsync(
                It.IsAny<BulkUploadRequestDto>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<BulkUploadRequestDto, string, CancellationToken>((_, userId, _) => mintedFor.Add(userId))
            .ReturnsAsync(Result<BulkUploadResponseDto>.Success(new BulkUploadResponseDto()));

        var controller = WithActor(CreateController(storage: storage), Actor);
        controller.ControllerContext.HttpContext.Request.Headers["X-User-Id"] = Victim;

        var result = await controller.GetUploadUrls(
            new BulkUploadRequestDto
            {
                Files = new List<UploadRequestDto>
                {
                    new() { FileName = "photo.png", ContentType = "image/png", FileSize = 1_024 }
                }
            },
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        mintedFor.Should().ContainSingle().Which.Should().Be(Actor);
    }

    [Fact]
    public async Task UploadUrl_RefusesWhenTheSizeIsNotDeclared_AndNeverReachesStorage()
    {
        var storage = new Mock<IStorageService>(MockBehavior.Strict);
        var controller = WithActor(CreateController(storage: storage), Actor);

        var result = await controller.GetUploadUrl(
            new UploadRequestDto { FileName = "photo.png", ContentType = "image/png", FileSize = null },
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_RecordsTheActorAsTheUploader_AndIgnoresTheBodysOwner()
    {
        var storage = new Mock<IStorageService>(MockBehavior.Strict);
        storage
            .Setup(service => service.ExistsAsync(ActorPrefixedPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var attachments = new Mock<IAttachmentService>(MockBehavior.Strict);
        var recorded = new List<CreateAttachmentDto>();
        attachments
            .Setup(service => service.CreateAsync(It.IsAny<CreateAttachmentDto>(), It.IsAny<CancellationToken>()))
            .Callback<CreateAttachmentDto, CancellationToken>((dto, _) => recorded.Add(dto))
            .ReturnsAsync(Result<AttachmentDto>.Success(new AttachmentDto { Id = "attachment-1", UploadedById = Actor }));

        var controller = WithActor(CreateController(attachments, storage), Actor);

        var result = await controller.Create(
            new CreateAttachmentDto
            {
                UploadedById = Victim,
                BucketPath = ActorPrefixedPath,
                Type = MimeType.IMAGE,
                FileName = "photo.png",
                FileSize = 1_024
            },
            CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        recorded.Should().ContainSingle().Which.UploadedById.Should().Be(
            Actor,
            "a caller-supplied uploader is ignored, so an attachment cannot be attributed to another account");
    }

    [Fact]
    public async Task Create_RefusesAStoragePathMintedForAnotherAccount_AndWritesNothing()
    {
        var storage = new Mock<IStorageService>(MockBehavior.Strict);
        var attachments = new Mock<IAttachmentService>(MockBehavior.Strict);
        var controller = WithActor(CreateController(attachments, storage), Actor);

        var result = await controller.Create(
            new CreateAttachmentDto
            {
                UploadedById = Actor,
                BucketPath = "user_victim-a/20260909/photo_1234abcd.png",
                Type = MimeType.IMAGE
            },
            CancellationToken.None);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        attachments.VerifyNoOtherCalls();
        storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_LeavesAnotherAccountsAttachmentIntact()
    {
        var storage = new Mock<IStorageService>(MockBehavior.Strict);
        var attachments = new Mock<IAttachmentService>(MockBehavior.Strict);
        attachments
            .Setup(service => service.GetByIdAsync("attachment-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AttachmentDto>.Success(new AttachmentDto
            {
                Id = "attachment-1",
                UploadedById = Victim,
                BucketPath = "user_victim-a/20260909/photo_1234abcd.png"
            }));

        var controller = WithActor(CreateController(attachments, storage), Actor);

        var result = await controller.Delete("attachment-1", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        attachments.Verify(
            service => service.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "the record must survive a delete attempted by someone who does not own it");
        storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DownloadUrl_IsNotIssuedForAnotherAccountsAttachment()
    {
        var storage = new Mock<IStorageService>(MockBehavior.Strict);
        var attachments = new Mock<IAttachmentService>(MockBehavior.Strict);
        attachments
            .Setup(service => service.GetByIdAsync("attachment-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AttachmentDto>.Success(new AttachmentDto
            {
                Id = "attachment-1",
                UploadedById = Victim,
                BucketPath = "user_victim-a/20260909/photo_1234abcd.png"
            }));

        var controller = WithActor(CreateController(attachments, storage), Actor);

        var result = await controller.GetDownloadUrl("attachment-1", 60, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ListingByUserId_RefusesAnyAccountButTheCallers()
    {
        var attachments = new Mock<IAttachmentService>(MockBehavior.Strict);
        var controller = WithActor(CreateController(attachments), Actor);

        var result = await controller.GetByUserId(Victim, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        attachments.VerifyNoOtherCalls();
    }

    [Fact]
    public void MultipartUpload_RefusesRatherThanUploadingUnderAnUnverifiedName()
    {
        var storage = new Mock<IStorageService>(MockBehavior.Strict);
        var controller = WithActor(CreateController(storage: storage), Actor);

        var result = controller.Upload();

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status501NotImplemented);
        storage.VerifyNoOtherCalls();
    }

    [Fact]
    public void EveryRouteOnTheControllerRequiresAnActorAssertion()
    {
        var controller = typeof(AttachmentsController);

        controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Select(attribute => attribute.Policy)
            .Should().Contain(
                AuthorizationPolicies.ActorAssertion,
                "OI-18 was open because this controller carried no class-level [Authorize]");

        HttpActions(controller)
            .Where(action => action.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any())
            .Select(action => action.Name)
            .Should().BeEmpty("an anonymous route here would reopen OI-18 for that route alone");
    }

    [Fact]
    public void NoRouteBindsItsOwnerFromARequestHeader()
    {
        HttpActions(typeof(AttachmentsController))
            .SelectMany(action => action.GetParameters())
            .Where(parameter => parameter.GetCustomAttributes<FromHeaderAttribute>(inherit: true).Any())
            .Select(parameter => parameter.Name!)
            .Should().BeEmpty(
                "the owner comes from the verified actor claim; a header binding is the OI-18 defect "
                + "itself, so it has to be structurally absent rather than merely unused");
    }

    private static IEnumerable<MethodInfo> HttpActions(Type controller)
        => controller
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Where(method => method
                .GetCustomAttributes<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>(inherit: true)
                .Any());

    private static AttachmentsController CreateController(
        Mock<IAttachmentService>? attachments = null,
        Mock<IStorageService>? storage = null)
        => new(
            (attachments ?? new Mock<IAttachmentService>(MockBehavior.Strict)).Object,
            (storage ?? new Mock<IStorageService>(MockBehavior.Strict)).Object);

    /// <summary>
    /// The principal shape <see cref="ActorAssertionMiddleware.TryGetActorId"/> accepts: the
    /// ActorAssertion identity plus the v2 marker the middleware stamps after verifying a signature.
    /// </summary>
    private static T WithActor<T>(T controller, string actorId)
        where T : ControllerBase
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, actorId),
                    new Claim(ActorAssertionMiddleware.ClaimType, "v2")
                },
                ActorAssertionMiddleware.AuthenticationType))
        };
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }
}

/// <summary>
/// OI-17: the default storage provider enforced nothing.
///
/// <see cref="LocalStorageService"/> is what a blank <c>STORAGE_PROVIDER</c> selects, and it used
/// to accept any extension at any size and to answer a presigned-PUT request with a multipart POST
/// endpoint. These tests assert at the counter — the bytes on disk — rather than at the door.
/// </summary>
public sealed class LocalStorageRefusalTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "ardanova-local-storage-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task AnOversizedUploadRefusesAndLeavesNothingOnDisk()
    {
        var basePath = Path.Combine(_root, "uploads");
        var storage = CreateStorage(basePath, maximumFileSizeBytes: 1_024);

        var result = await storage.UploadAsync(
            new MemoryStream(new byte[4_096]),
            "photo.png",
            "image/png",
            folder: "project",
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("exceeds maximum allowed size");
        StoredFiles(basePath).Should().BeEmpty("an over-cap upload must not leave a partial file behind");
    }

    [Fact]
    public async Task ADisallowedExtensionRefusesAndLeavesNothingOnDisk()
    {
        var basePath = Path.Combine(_root, "uploads");
        var storage = CreateStorage(basePath);

        var result = await storage.UploadAsync(
            new MemoryStream(new byte[16]),
            "payload.exe",
            "application/octet-stream",
            folder: null,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain(".exe");
        StoredFiles(basePath).Should().BeEmpty();
    }

    [Fact]
    public async Task AnAllowedUploadUnderTheCapStillSucceeds()
    {
        var basePath = Path.Combine(_root, "uploads");
        var storage = CreateStorage(basePath);

        var result = await storage.UploadAsync(
            new MemoryStream(new byte[16]),
            "photo.png",
            "image/png",
            folder: "project",
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error ?? string.Empty);
        StoredFiles(basePath).Should().ContainSingle();
    }

    [Fact]
    public async Task AFolderThatEscapesTheStorageRootRefuses()
    {
        var basePath = Path.Combine(_root, "uploads");
        var storage = CreateStorage(basePath);

        var result = await storage.UploadAsync(
            new MemoryStream(new byte[16]),
            "photo.png",
            "image/png",
            folder: "../escaped",
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.ValidationError);
        Directory.Exists(Path.Combine(_root, "escaped")).Should().BeFalse(
            "a folder that resolves outside the storage root must not create anything outside it");
    }

    [Fact]
    public async Task APresignedUploadUrlIsRefusedInsteadOfAnsweredWithAMultipartEndpoint()
    {
        var storage = CreateStorage(Path.Combine(_root, "uploads"));

        var single = await storage.GetPresignedUploadUrlAsync(
            new UploadRequestDto { FileName = "photo.png", ContentType = "image/png", FileSize = 16 },
            "owner-actor",
            CancellationToken.None);
        var bulk = await storage.GetPresignedUploadUrlsAsync(
            new BulkUploadRequestDto { Files = new List<UploadRequestDto>() },
            "owner-actor",
            CancellationToken.None);

        single.IsFailure.Should().BeTrue();
        single.Type.Should().Be(ResultType.Conflict);
        single.Error.Should().Be(LocalStorageService.PresignedUploadUnsupportedMessage);
        bulk.IsFailure.Should().BeTrue();
        bulk.Error.Should().Be(LocalStorageService.PresignedUploadUnsupportedMessage);
    }

    [Fact]
    public async Task AnEmptyAllowListRefusesEverythingInsteadOfAllowingEverything()
    {
        var basePath = Path.Combine(_root, "uploads");
        var storage = CreateStorage(basePath, allowedExtensions: new List<string>());

        var result = await storage.UploadAsync(
            new MemoryStream(new byte[16]),
            "photo.png",
            "image/png",
            folder: null,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.Conflict);
        result.Error.Should().Contain("allow-list");
        Directory.Exists(basePath).Should().BeFalse(
            "an unconfigured local provider refuses before it touches the disk at all");
    }

    [Fact]
    public async Task ABlankBasePathRefusesInsteadOfFallingBackToAnImplicitDirectory()
    {
        var storage = CreateStorage(basePath: "   ");

        var result = await storage.UploadAsync(
            new MemoryStream(new byte[16]),
            "photo.png",
            "image/png",
            folder: null,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.Conflict);
        result.Error.Should().Contain("LOCAL_STORAGE_PATH");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch (IOException)
        {
            // A scratch directory the OS is still holding is not a test failure.
        }
    }

    private static LocalStorageService CreateStorage(
        string basePath,
        long maximumFileSizeBytes = 25_000_000,
        List<string>? allowedExtensions = null)
        => new(
            Options.Create(new StorageConfiguration
            {
                Provider = "Local",
                MaxFileSizeBytes = maximumFileSizeBytes,
                AllowedExtensions = allowedExtensions ?? new List<string> { ".png", ".pdf" },
                Local = new LocalStorageConfiguration
                {
                    BasePath = basePath,
                    PublicBaseUrl = "http://localhost:8080/files"
                }
            }),
            new Mock<ILogger<LocalStorageService>>().Object);

    private static IReadOnlyList<string> StoredFiles(string basePath)
        => Directory.Exists(basePath)
            ? Directory.GetFiles(basePath, "*", SearchOption.AllDirectories)
            : Array.Empty<string>();
}
