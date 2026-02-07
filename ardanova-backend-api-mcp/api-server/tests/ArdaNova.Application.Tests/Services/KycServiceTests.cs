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
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IKycProviderService> _providerMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly KycService _sut;

    public KycServiceTests()
    {
        _submissionRepoMock = new Mock<IRepository<KycSubmission>>();
        _documentRepoMock = new Mock<IRepository<KycDocument>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _providerMock = new Mock<IKycProviderService>();
        _userServiceMock = new Mock<IUserService>();
        _sut = new KycService(
            _submissionRepoMock.Object,
            _documentRepoMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _providerMock.Object,
            _userServiceMock.Object);
    }

    #region SubmitAsync

    [Fact]
    public async Task SubmitAsync_WithValidDto_CreatesSubmissionAndDocuments()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var dto = new SubmitKycDto
        {
            UserId = userId,
            Documents = new List<SubmitKycDocumentDto>
            {
                new SubmitKycDocumentDto
                {
                    Type = KycDocumentType.GOVERNMENT_ID,
                    FileUrl = "https://s3.example.com/doc1.jpg",
                    FileName = "id-front.jpg",
                    MimeType = "image/jpeg",
                    FileSizeBytes = 1024
                }
            }
        };

        _submissionRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KycSubmission, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KycSubmission>());

        _providerMock.Setup(p => p.ValidateDocumentsAsync(dto.Documents, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        _submissionRepoMock.Setup(r => r.AddAsync(It.IsAny<KycSubmission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KycSubmission s, CancellationToken _) => s);

        _documentRepoMock.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<KycDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<KycDocument> docs, CancellationToken _) => docs);

        _providerMock.Setup(p => p.CreateSessionAsync(userId, It.IsAny<List<KycDocumentDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success(userId));

        _mapperMock.Setup(m => m.Map<KycDocumentDto>(It.IsAny<KycDocument>()))
            .Returns((KycDocument d) => new KycDocumentDto
            {
                Id = d.id,
                SubmissionId = d.submissionId,
                Type = d.type,
                FileUrl = d.fileUrl,
                FileName = d.fileName
            });

        _mapperMock.Setup(m => m.Map<KycSubmissionDto>(It.IsAny<KycSubmission>()))
            .Returns((KycSubmission s) => new KycSubmissionDto
            {
                Id = s.id,
                UserId = s.userId,
                Status = s.status,
                Provider = s.provider,
                SubmittedAt = s.submittedAt
            });

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.SubmitAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.UserId.Should().Be(userId);
        result.Value.Status.Should().Be(KycStatus.PENDING);
        result.Value.Documents.Should().HaveCount(1);

        _submissionRepoMock.Verify(r => r.AddAsync(It.Is<KycSubmission>(s =>
            s.userId == userId && s.status == KycStatus.PENDING && s.provider == KycProvider.MANUAL),
            It.IsAny<CancellationToken>()), Times.Once);

        _documentRepoMock.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<KycDocument>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_WithActiveSubmission_ReturnsValidationError()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var dto = new SubmitKycDto
        {
            UserId = userId,
            Documents = new List<SubmitKycDocumentDto>
            {
                new SubmitKycDocumentDto
                {
                    Type = KycDocumentType.GOVERNMENT_ID,
                    FileUrl = "https://s3.example.com/doc1.jpg",
                    FileName = "id-front.jpg"
                }
            }
        };

        var existingSubmission = new KycSubmission
        {
            id = Guid.NewGuid().ToString(),
            userId = userId,
            status = KycStatus.PENDING,
            provider = KycProvider.MANUAL,
            submittedAt = DateTime.UtcNow,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _submissionRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KycSubmission, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KycSubmission> { existingSubmission });

        // Act
        var result = await _sut.SubmitAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("active KYC submission already exists");
    }

    [Fact]
    public async Task SubmitAsync_WithInvalidDocuments_ReturnsValidationError()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var dto = new SubmitKycDto
        {
            UserId = userId,
            Documents = new List<SubmitKycDocumentDto>()
        };

        _submissionRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KycSubmission, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KycSubmission>());

        _providerMock.Setup(p => p.ValidateDocumentsAsync(dto.Documents, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.ValidationError("At least one document is required"));

        // Act
        var result = await _sut.SubmitAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("At least one document is required");
    }

    [Fact]
    public async Task SubmitAsync_AfterRejection_AllowsResubmission()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var dto = new SubmitKycDto
        {
            UserId = userId,
            Documents = new List<SubmitKycDocumentDto>
            {
                new SubmitKycDocumentDto
                {
                    Type = KycDocumentType.PASSPORT,
                    FileUrl = "https://s3.example.com/passport.jpg",
                    FileName = "passport.jpg",
                    MimeType = "image/jpeg"
                }
            }
        };

        // Only a REJECTED submission exists, not PENDING/IN_REVIEW
        _submissionRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KycSubmission, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KycSubmission>());

        _providerMock.Setup(p => p.ValidateDocumentsAsync(dto.Documents, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        _submissionRepoMock.Setup(r => r.AddAsync(It.IsAny<KycSubmission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KycSubmission s, CancellationToken _) => s);

        _documentRepoMock.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<KycDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<KycDocument> docs, CancellationToken _) => docs);

        _providerMock.Setup(p => p.CreateSessionAsync(userId, It.IsAny<List<KycDocumentDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success(userId));

        _mapperMock.Setup(m => m.Map<KycDocumentDto>(It.IsAny<KycDocument>()))
            .Returns(new KycDocumentDto { Id = "doc1", SubmissionId = "sub1", FileName = "passport.jpg", FileUrl = "url" });

        _mapperMock.Setup(m => m.Map<KycSubmissionDto>(It.IsAny<KycSubmission>()))
            .Returns((KycSubmission s) => new KycSubmissionDto
            {
                Id = s.id,
                UserId = s.userId,
                Status = s.status
            });

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.SubmitAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(KycStatus.PENDING);
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_WhenSubmissionExists_ReturnsSuccess()
    {
        // Arrange
        var submissionId = Guid.NewGuid().ToString();
        var submission = new KycSubmission
        {
            id = submissionId,
            userId = Guid.NewGuid().ToString(),
            provider = KycProvider.MANUAL,
            status = KycStatus.PENDING,
            submittedAt = DateTime.UtcNow,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _submissionRepoMock.Setup(r => r.GetByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _documentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KycDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KycDocument>());
        _mapperMock.Setup(m => m.Map<KycSubmissionDto>(submission))
            .Returns(new KycSubmissionDto { Id = submissionId, Status = KycStatus.PENDING });
        _mapperMock.Setup(m => m.Map<List<KycDocumentDto>>(It.IsAny<IReadOnlyList<KycDocument>>()))
            .Returns(new List<KycDocumentDto>());

        // Act
        var result = await _sut.GetByIdAsync(submissionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(submissionId);
    }

    [Fact]
    public async Task GetByIdAsync_WhenSubmissionNotExists_ReturnsNotFound()
    {
        // Arrange
        var submissionId = Guid.NewGuid().ToString();
        _submissionRepoMock.Setup(r => r.GetByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KycSubmission?)null);

        // Act
        var result = await _sut.GetByIdAsync(submissionId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    #endregion

    #region GetByUserIdAsync

    [Fact]
    public async Task GetByUserIdAsync_WhenSubmissionExists_ReturnsMostRecent()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var olderSubmission = new KycSubmission
        {
            id = Guid.NewGuid().ToString(),
            userId = userId,
            status = KycStatus.REJECTED,
            provider = KycProvider.MANUAL,
            submittedAt = DateTime.UtcNow.AddDays(-10),
            createdAt = DateTime.UtcNow.AddDays(-10),
            updatedAt = DateTime.UtcNow.AddDays(-10)
        };
        var newerSubmission = new KycSubmission
        {
            id = Guid.NewGuid().ToString(),
            userId = userId,
            status = KycStatus.PENDING,
            provider = KycProvider.MANUAL,
            submittedAt = DateTime.UtcNow,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _submissionRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KycSubmission, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KycSubmission> { olderSubmission, newerSubmission });
        _documentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KycDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KycDocument>());
        _mapperMock.Setup(m => m.Map<KycSubmissionDto>(newerSubmission))
            .Returns(new KycSubmissionDto { Id = newerSubmission.id, Status = KycStatus.PENDING });
        _mapperMock.Setup(m => m.Map<List<KycDocumentDto>>(It.IsAny<IReadOnlyList<KycDocument>>()))
            .Returns(new List<KycDocumentDto>());

        // Act
        var result = await _sut.GetByUserIdAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(newerSubmission.id);
        result.Value.Status.Should().Be(KycStatus.PENDING);
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenNoSubmission_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _submissionRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KycSubmission, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KycSubmission>());

        // Act
        var result = await _sut.GetByUserIdAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
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
            new KycSubmission
            {
                id = Guid.NewGuid().ToString(),
                userId = Guid.NewGuid().ToString(),
                status = KycStatus.PENDING,
                provider = KycProvider.MANUAL,
                submittedAt = DateTime.UtcNow,
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow
            },
            new KycSubmission
            {
                id = Guid.NewGuid().ToString(),
                userId = Guid.NewGuid().ToString(),
                status = KycStatus.IN_REVIEW,
                provider = KycProvider.MANUAL,
                submittedAt = DateTime.UtcNow,
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow
            }
        };

        _submissionRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KycSubmission, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(submissions);
        _documentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KycDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KycDocument>());

        _mapperMock.Setup(m => m.Map<KycSubmissionDto>(It.IsAny<KycSubmission>()))
            .Returns((KycSubmission s) => new KycSubmissionDto { Id = s.id, Status = s.status });
        _mapperMock.Setup(m => m.Map<List<KycDocumentDto>>(It.IsAny<IReadOnlyList<KycDocument>>()))
            .Returns(new List<KycDocumentDto>());

        // Act
        var result = await _sut.GetPendingAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    #endregion

    #region ApproveAsync

    [Fact]
    public async Task ApproveAsync_WithPendingSubmission_ApprovesAndUpgradesUser()
    {
        // Arrange
        var submissionId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var reviewerId = Guid.NewGuid().ToString();
        var submission = new KycSubmission
        {
            id = submissionId,
            userId = userId,
            status = KycStatus.PENDING,
            provider = KycProvider.MANUAL,
            submittedAt = DateTime.UtcNow,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _submissionRepoMock.Setup(r => r.GetByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KycSubmission>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _userServiceMock.Setup(u => u.UpdateVerificationLevelAsync(userId,
            It.Is<AdminUpdateVerificationLevelDto>(d => d.VerificationLevel == VerificationLevel.PRO),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Success(new UserDto { Id = userId, VerificationLevel = VerificationLevel.PRO }));
        _documentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KycDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KycDocument>());
        _mapperMock.Setup(m => m.Map<KycSubmissionDto>(It.IsAny<KycSubmission>()))
            .Returns((KycSubmission s) => new KycSubmissionDto
            {
                Id = s.id,
                UserId = s.userId,
                Status = s.status,
                ReviewerId = s.reviewerId,
                ReviewNotes = s.reviewNotes
            });
        _mapperMock.Setup(m => m.Map<List<KycDocumentDto>>(It.IsAny<IReadOnlyList<KycDocument>>()))
            .Returns(new List<KycDocumentDto>());

        // Act
        var result = await _sut.ApproveAsync(submissionId, reviewerId, "All documents verified");

        // Assert
        result.IsSuccess.Should().BeTrue();
        submission.status.Should().Be(KycStatus.APPROVED);
        submission.reviewerId.Should().Be(reviewerId);
        submission.reviewNotes.Should().Be("All documents verified");
        submission.reviewedAt.Should().NotBeNull();

        _userServiceMock.Verify(u => u.UpdateVerificationLevelAsync(userId,
            It.Is<AdminUpdateVerificationLevelDto>(d => d.VerificationLevel == VerificationLevel.PRO),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_WithInReviewSubmission_ApprovesSuccessfully()
    {
        // Arrange
        var submissionId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var reviewerId = Guid.NewGuid().ToString();
        var submission = new KycSubmission
        {
            id = submissionId,
            userId = userId,
            status = KycStatus.IN_REVIEW,
            provider = KycProvider.MANUAL,
            submittedAt = DateTime.UtcNow,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _submissionRepoMock.Setup(r => r.GetByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KycSubmission>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _userServiceMock.Setup(u => u.UpdateVerificationLevelAsync(userId,
            It.IsAny<AdminUpdateVerificationLevelDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Success(new UserDto { Id = userId }));
        _documentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KycDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KycDocument>());
        _mapperMock.Setup(m => m.Map<KycSubmissionDto>(It.IsAny<KycSubmission>()))
            .Returns((KycSubmission s) => new KycSubmissionDto { Id = s.id, Status = s.status });
        _mapperMock.Setup(m => m.Map<List<KycDocumentDto>>(It.IsAny<IReadOnlyList<KycDocument>>()))
            .Returns(new List<KycDocumentDto>());

        // Act
        var result = await _sut.ApproveAsync(submissionId, reviewerId, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        submission.status.Should().Be(KycStatus.APPROVED);
    }

    [Fact]
    public async Task ApproveAsync_WithAlreadyApprovedSubmission_ReturnsValidationError()
    {
        // Arrange
        var submissionId = Guid.NewGuid().ToString();
        var submission = new KycSubmission
        {
            id = submissionId,
            userId = Guid.NewGuid().ToString(),
            status = KycStatus.APPROVED,
            provider = KycProvider.MANUAL,
            submittedAt = DateTime.UtcNow,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _submissionRepoMock.Setup(r => r.GetByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _sut.ApproveAsync(submissionId, "reviewer1", null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("Cannot approve");
    }

    [Fact]
    public async Task ApproveAsync_WithNonExistentSubmission_ReturnsNotFound()
    {
        // Arrange
        var submissionId = Guid.NewGuid().ToString();
        _submissionRepoMock.Setup(r => r.GetByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KycSubmission?)null);

        // Act
        var result = await _sut.ApproveAsync(submissionId, "reviewer1", null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    #endregion

    #region RejectAsync

    [Fact]
    public async Task RejectAsync_WithPendingSubmission_RejectsWithReason()
    {
        // Arrange
        var submissionId = Guid.NewGuid().ToString();
        var reviewerId = Guid.NewGuid().ToString();
        var submission = new KycSubmission
        {
            id = submissionId,
            userId = Guid.NewGuid().ToString(),
            status = KycStatus.PENDING,
            provider = KycProvider.MANUAL,
            submittedAt = DateTime.UtcNow,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _submissionRepoMock.Setup(r => r.GetByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KycSubmission>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _documentRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<KycDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KycDocument>());
        _mapperMock.Setup(m => m.Map<KycSubmissionDto>(It.IsAny<KycSubmission>()))
            .Returns((KycSubmission s) => new KycSubmissionDto
            {
                Id = s.id,
                Status = s.status,
                ReviewerId = s.reviewerId,
                RejectionReason = s.rejectionReason
            });
        _mapperMock.Setup(m => m.Map<List<KycDocumentDto>>(It.IsAny<IReadOnlyList<KycDocument>>()))
            .Returns(new List<KycDocumentDto>());

        // Act
        var result = await _sut.RejectAsync(submissionId, reviewerId, "Blurry document", "Please resubmit with a clearer image");

        // Assert
        result.IsSuccess.Should().BeTrue();
        submission.status.Should().Be(KycStatus.REJECTED);
        submission.reviewerId.Should().Be(reviewerId);
        submission.reviewNotes.Should().Be("Blurry document");
        submission.rejectionReason.Should().Be("Please resubmit with a clearer image");
        submission.reviewedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RejectAsync_WithAlreadyRejectedSubmission_ReturnsValidationError()
    {
        // Arrange
        var submissionId = Guid.NewGuid().ToString();
        var submission = new KycSubmission
        {
            id = submissionId,
            userId = Guid.NewGuid().ToString(),
            status = KycStatus.REJECTED,
            provider = KycProvider.MANUAL,
            submittedAt = DateTime.UtcNow,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _submissionRepoMock.Setup(r => r.GetByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        // Act
        var result = await _sut.RejectAsync(submissionId, "reviewer1", null, null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("Cannot reject");
    }

    [Fact]
    public async Task RejectAsync_WithNonExistentSubmission_ReturnsNotFound()
    {
        // Arrange
        var submissionId = Guid.NewGuid().ToString();
        _submissionRepoMock.Setup(r => r.GetByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KycSubmission?)null);

        // Act
        var result = await _sut.RejectAsync(submissionId, "reviewer1", null, null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    #endregion
}
