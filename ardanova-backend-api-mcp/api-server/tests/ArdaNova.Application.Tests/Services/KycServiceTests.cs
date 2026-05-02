namespace ArdaNova.Application.Tests.Services;

using System.Linq.Expressions;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using FluentAssertions;
using Moq;

public class KycServiceTests
{
    private readonly Mock<IRepository<KycSubmission>> _submissionRepoMock;
    private readonly Mock<IRepository<KycDocument>> _documentRepoMock;
    private readonly Mock<IRepository<User>> _userRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IKycProviderService> _providerMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly KycService _sut;

    public KycServiceTests()
    {
        _submissionRepoMock = new Mock<IRepository<KycSubmission>>();
        _documentRepoMock = new Mock<IRepository<KycDocument>>();
        _userRepoMock = new Mock<IRepository<User>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _providerMock = new Mock<IKycProviderService>();
        _userServiceMock = new Mock<IUserService>();

        // Default mapper setup for KycDocumentDto
        _mapperMock
            .Setup(x => x.Map<KycDocumentDto>(It.IsAny<KycDocument>()))
            .Returns((KycDocument d) => new KycDocumentDto
            {
                Id = d.id,
                SubmissionId = d.submissionId,
                Type = d.type,
                FileUrl = d.fileUrl,
                FileName = d.fileName,
                MimeType = d.mimeType,
                FileSizeBytes = d.fileSizeBytes,
                CreatedAt = d.createdAt
            });

        _mapperMock
            .Setup(x => x.Map<List<KycDocumentDto>>(It.IsAny<IReadOnlyList<KycDocument>>()))
            .Returns((IReadOnlyList<KycDocument> docs) => docs.Select(d => new KycDocumentDto
            {
                Id = d.id,
                SubmissionId = d.submissionId,
                Type = d.type,
                FileUrl = d.fileUrl,
                FileName = d.fileName,
                CreatedAt = d.createdAt
            }).ToList());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _sut = new KycService(
            _submissionRepoMock.Object,
            _documentRepoMock.Object,
            _userRepoMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _providerMock.Object,
            _userServiceMock.Object
        );
    }

    private static SubmitKycDto CreateValidSubmitDto(string userId = "user-123") => new()
    {
        UserId = userId,
        Documents = new List<SubmitKycDocumentDto>
        {
            new()
            {
                Type = KycDocumentType.PASSPORT,
                FileUrl = "https://example.com/passport.jpg",
                FileName = "passport.jpg",
            }
        }
    };

    private void SetupSubmitDefaults()
    {
        _submissionRepoMock
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<KycSubmission, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KycSubmission>());

        _providerMock
            .Setup(x => x.ValidateDocumentsAsync(It.IsAny<List<SubmitKycDocumentDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        _providerMock
            .Setup(x => x.CreateSessionAsync(It.IsAny<string>(), It.IsAny<List<KycDocumentDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("session-123"));

        _mapperMock
            .Setup(x => x.Map<KycSubmissionDto>(It.IsAny<KycSubmission>()))
            .Returns((KycSubmission s) => new KycSubmissionDto
            {
                Id = s.id,
                UserId = s.userId,
                Provider = s.provider,
                Status = s.status,
                ProviderSessionId = s.providerSessionId,
                SubmittedAt = s.submittedAt,
                CreatedAt = s.createdAt,
                UpdatedAt = s.updatedAt,
            });
    }

    #region SubmitAsync

    [Fact]
    public async Task SubmitAsync_WithNoActiveSubmission_CreatesSubmissionAndDocuments()
    {
        // Arrange
        SetupSubmitDefaults();
        var dto = CreateValidSubmitDto();

        // Act
        var result = await _sut.SubmitAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.UserId.Should().Be("user-123");
        result.Value.Status.Should().Be(KycStatus.PENDING);
        result.Value.Provider.Should().Be(KycProvider.MANUAL);

        _submissionRepoMock.Verify(x => x.AddAsync(It.IsAny<KycSubmission>(), It.IsAny<CancellationToken>()), Times.Once);
        _documentRepoMock.Verify(x => x.AddRangeAsync(It.IsAny<IEnumerable<KycDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SubmitAsync_CallsProviderCreateSession()
    {
        // Arrange
        SetupSubmitDefaults();
        var dto = CreateValidSubmitDto();

        // Act
        await _sut.SubmitAsync(dto);

        // Assert
        _providerMock.Verify(x => x.CreateSessionAsync(
            "user-123",
            It.IsAny<List<KycDocumentDto>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_WithActiveSubmission_ReturnsValidationError()
    {
        // Arrange
        var activeSubmission = new KycSubmission
        {
            id = "existing-123",
            userId = "user-123",
            status = KycStatus.PENDING,
            provider = KycProvider.MANUAL,
            submittedAt = DateTime.UtcNow,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _submissionRepoMock
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<KycSubmission, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KycSubmission> { activeSubmission });

        var dto = CreateValidSubmitDto();

        // Act
        var result = await _sut.SubmitAsync(dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("active");
        _submissionRepoMock.Verify(x => x.AddAsync(It.IsAny<KycSubmission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_WithDocumentValidationFailure_ReturnsValidationError()
    {
        // Arrange
        _submissionRepoMock
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<KycSubmission, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KycSubmission>());

        _providerMock
            .Setup(x => x.ValidateDocumentsAsync(It.IsAny<List<SubmitKycDocumentDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.ValidationError("Invalid document format"));

        var dto = CreateValidSubmitDto();

        // Act
        var result = await _sut.SubmitAsync(dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid document");
        _submissionRepoMock.Verify(x => x.AddAsync(It.IsAny<KycSubmission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_AfterRejection_AllowsResubmission()
    {
        // Arrange — FindAsync returns empty because the REJECTED submission
        // does NOT match the PENDING/IN_REVIEW predicate in the real service
        SetupSubmitDefaults();
        var dto = CreateValidSubmitDto();

        // Act
        var result = await _sut.SubmitAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _submissionRepoMock.Verify(x => x.AddAsync(It.IsAny<KycSubmission>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsSubmissionWithDocuments()
    {
        // Arrange
        var submissionId = "submission-123";
        var submission = new KycSubmission
        {
            id = submissionId,
            userId = "user-123",
            provider = KycProvider.MANUAL,
            status = KycStatus.PENDING,
            submittedAt = DateTime.UtcNow,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        var documents = new List<KycDocument>
        {
            new()
            {
                id = "doc-1",
                submissionId = submissionId,
                type = KycDocumentType.PASSPORT,
                fileUrl = "https://example.com/passport.jpg",
                fileName = "passport.jpg",
                createdAt = DateTime.UtcNow
            }
        };

        _submissionRepoMock
            .Setup(x => x.GetByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        _documentRepoMock
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<KycDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        _mapperMock
            .Setup(x => x.Map<KycSubmissionDto>(It.IsAny<KycSubmission>()))
            .Returns(new KycSubmissionDto { Id = submissionId, UserId = "user-123", Status = KycStatus.PENDING });

        // Act
        var result = await _sut.GetByIdAsync(submissionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(submissionId);
        result.Value.Documents.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        _submissionRepoMock
            .Setup(x => x.GetByIdAsync("invalid-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((KycSubmission?)null);

        // Act
        var result = await _sut.GetByIdAsync("invalid-123");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.NotFound);
        result.Error.Should().Contain("not found");
    }

    #endregion

    #region GetByUserIdAsync

    [Fact]
    public async Task GetByUserIdAsync_WithMultipleSubmissions_ReturnsMostRecent()
    {
        // Arrange
        var userId = "user-123";
        var olderSubmission = new KycSubmission
        {
            id = "submission-old",
            userId = userId,
            status = KycStatus.REJECTED,
            provider = KycProvider.MANUAL,
            submittedAt = DateTime.UtcNow.AddDays(-2),
            createdAt = DateTime.UtcNow.AddDays(-2),
            updatedAt = DateTime.UtcNow.AddDays(-2)
        };
        var newerSubmission = new KycSubmission
        {
            id = "submission-new",
            userId = userId,
            status = KycStatus.PENDING,
            provider = KycProvider.MANUAL,
            submittedAt = DateTime.UtcNow,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _submissionRepoMock
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<KycSubmission, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KycSubmission> { olderSubmission, newerSubmission });

        _documentRepoMock
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<KycDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KycDocument>());

        _mapperMock
            .Setup(x => x.Map<KycSubmissionDto>(It.Is<KycSubmission>(s => s.id == "submission-new")))
            .Returns(new KycSubmissionDto { Id = "submission-new", UserId = userId, Status = KycStatus.PENDING });

        // Act
        var result = await _sut.GetByUserIdAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be("submission-new");
    }

    [Fact]
    public async Task GetByUserIdAsync_WithNoSubmissions_ReturnsNotFound()
    {
        // Arrange
        _submissionRepoMock
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<KycSubmission, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KycSubmission>());

        // Act
        var result = await _sut.GetByUserIdAsync("user-123");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.NotFound);
    }

    #endregion

    #region GetPendingAsync

    [Fact]
    public async Task GetPendingAsync_ReturnsPendingAndInReviewSubmissions()
    {
        // Arrange
        var submissions = new List<KycSubmission>
        {
            new()
            {
                id = "s1", userId = "u1", status = KycStatus.PENDING,
                provider = KycProvider.MANUAL, submittedAt = DateTime.UtcNow,
                createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow
            },
            new()
            {
                id = "s2", userId = "u2", status = KycStatus.IN_REVIEW,
                provider = KycProvider.MANUAL, submittedAt = DateTime.UtcNow,
                createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow
            }
        };

        _submissionRepoMock
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<KycSubmission, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(submissions);

        _documentRepoMock
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<KycDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KycDocument>());

        _mapperMock
            .Setup(x => x.Map<KycSubmissionDto>(It.IsAny<KycSubmission>()))
            .Returns((KycSubmission s) => new KycSubmissionDto
            {
                Id = s.id, UserId = s.userId, Status = s.status
            });

        // Act
        var result = await _sut.GetPendingAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    #endregion

    #region ApproveAsync

    [Fact]
    public async Task ApproveAsync_WithPendingSubmission_ApprovesAndUpgradesUserToPro()
    {
        // Arrange
        var submissionId = "sub-123";
        var reviewerId = "admin-1";
        var submission = new KycSubmission
        {
            id = submissionId,
            userId = "user-123",
            status = KycStatus.PENDING,
            provider = KycProvider.MANUAL,
            submittedAt = DateTime.UtcNow,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _submissionRepoMock.Setup(x => x.GetByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        _documentRepoMock
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<KycDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KycDocument>());

        _mapperMock
            .Setup(x => x.Map<KycSubmissionDto>(It.IsAny<KycSubmission>()))
            .Returns((KycSubmission s) => new KycSubmissionDto
            {
                Id = s.id, UserId = s.userId, Status = s.status,
                ReviewerId = s.reviewerId, ReviewNotes = s.reviewNotes
            });

        _userServiceMock
            .Setup(x => x.UpdateVerificationLevelAsync("user-123", It.IsAny<AdminUpdateVerificationLevelDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Success(new UserDto { Id = "user-123" }));

        // Act
        var result = await _sut.ApproveAsync(submissionId, reviewerId, "Looks good");

        // Assert
        result.IsSuccess.Should().BeTrue();

        _submissionRepoMock.Verify(x => x.UpdateAsync(
            It.Is<KycSubmission>(s =>
                s.status == KycStatus.APPROVED &&
                s.reviewerId == reviewerId &&
                s.reviewNotes == "Looks good" &&
                s.reviewedAt != null),
            It.IsAny<CancellationToken>()), Times.Once);

        _userServiceMock.Verify(x => x.UpdateVerificationLevelAsync(
            "user-123",
            It.Is<AdminUpdateVerificationLevelDto>(dto => dto.VerificationLevel == VerificationLevel.PRO),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        _submissionRepoMock.Setup(x => x.GetByIdAsync("invalid", It.IsAny<CancellationToken>()))
            .ReturnsAsync((KycSubmission?)null);

        // Act
        var result = await _sut.ApproveAsync("invalid", "admin-1", null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.NotFound);
        _userServiceMock.Verify(x => x.UpdateVerificationLevelAsync(
            It.IsAny<string>(), It.IsAny<AdminUpdateVerificationLevelDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApproveAsync_WithAlreadyApproved_ReturnsValidationError()
    {
        // Arrange
        var submission = new KycSubmission
        {
            id = "sub-1", userId = "u1", status = KycStatus.APPROVED,
            provider = KycProvider.MANUAL, submittedAt = DateTime.UtcNow,
            createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow
        };

        _submissionRepoMock.Setup(x => x.GetByIdAsync("sub-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _sut.ApproveAsync("sub-1", "admin-1", null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("PENDING or IN_REVIEW");
    }

    [Fact]
    public async Task ApproveAsync_WithInReviewStatus_Succeeds()
    {
        // Arrange
        var submission = new KycSubmission
        {
            id = "sub-1", userId = "u1", status = KycStatus.IN_REVIEW,
            provider = KycProvider.MANUAL, submittedAt = DateTime.UtcNow,
            createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow
        };

        _submissionRepoMock.Setup(x => x.GetByIdAsync("sub-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        _documentRepoMock
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<KycDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KycDocument>());

        _mapperMock
            .Setup(x => x.Map<KycSubmissionDto>(It.IsAny<KycSubmission>()))
            .Returns(new KycSubmissionDto { Id = "sub-1", Status = KycStatus.APPROVED });

        _userServiceMock
            .Setup(x => x.UpdateVerificationLevelAsync(It.IsAny<string>(), It.IsAny<AdminUpdateVerificationLevelDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Success(new UserDto { Id = "u1" }));

        // Act
        var result = await _sut.ApproveAsync("sub-1", "admin-1", null);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region RejectAsync

    [Fact]
    public async Task RejectAsync_WithPendingSubmission_RejectsWithReason()
    {
        // Arrange
        var submissionId = "sub-123";
        var reviewerId = "admin-1";
        var reason = "Documents are blurry";
        var submission = new KycSubmission
        {
            id = submissionId,
            userId = "user-123",
            status = KycStatus.PENDING,
            provider = KycProvider.MANUAL,
            submittedAt = DateTime.UtcNow,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _submissionRepoMock.Setup(x => x.GetByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        _documentRepoMock
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<KycDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KycDocument>());

        _mapperMock
            .Setup(x => x.Map<KycSubmissionDto>(It.IsAny<KycSubmission>()))
            .Returns((KycSubmission s) => new KycSubmissionDto
            {
                Id = s.id, UserId = s.userId, Status = s.status,
                ReviewerId = s.reviewerId, RejectionReason = s.rejectionReason
            });

        // Act
        var result = await _sut.RejectAsync(submissionId, reviewerId, "Internal notes", reason);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _submissionRepoMock.Verify(x => x.UpdateAsync(
            It.Is<KycSubmission>(s =>
                s.status == KycStatus.REJECTED &&
                s.reviewerId == reviewerId &&
                s.rejectionReason == reason &&
                s.reviewNotes == "Internal notes" &&
                s.reviewedAt != null),
            It.IsAny<CancellationToken>()), Times.Once);

        // Should NOT upgrade user on rejection
        _userServiceMock.Verify(x => x.UpdateVerificationLevelAsync(
            It.IsAny<string>(), It.IsAny<AdminUpdateVerificationLevelDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RejectAsync_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        _submissionRepoMock.Setup(x => x.GetByIdAsync("invalid", It.IsAny<CancellationToken>()))
            .ReturnsAsync((KycSubmission?)null);

        // Act
        var result = await _sut.RejectAsync("invalid", "admin-1", null, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task RejectAsync_WithAlreadyRejected_ReturnsValidationError()
    {
        // Arrange
        var submission = new KycSubmission
        {
            id = "sub-1", userId = "u1", status = KycStatus.REJECTED,
            provider = KycProvider.MANUAL, submittedAt = DateTime.UtcNow,
            createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow
        };

        _submissionRepoMock.Setup(x => x.GetByIdAsync("sub-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _sut.RejectAsync("sub-1", "admin-1", null, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("PENDING or IN_REVIEW");
    }

    #endregion
}
