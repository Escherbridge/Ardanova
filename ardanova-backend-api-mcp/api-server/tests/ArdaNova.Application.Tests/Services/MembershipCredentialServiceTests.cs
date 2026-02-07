namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using FluentAssertions;
using Moq;

public class MembershipCredentialServiceTests
{
    private readonly Mock<IRepository<MembershipCredential>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly MembershipCredentialService _sut;

    public MembershipCredentialServiceTests()
    {
        _repositoryMock = new Mock<IRepository<MembershipCredential>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new MembershipCredentialService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    // ========================================================================
    // GetByIdAsync
    // ========================================================================

    [Fact]
    public async Task GetByIdAsync_WhenCredentialExists_ReturnsSuccess()
    {
        // Arrange
        var credentialId = Guid.NewGuid().ToString();
        var credential = CreateTestCredential(credentialId);
        var credentialDto = CreateTestCredentialDto(credentialId);

        _repositoryMock.Setup(r => r.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);
        _mapperMock.Setup(m => m.Map<MembershipCredentialDto>(credential)).Returns(credentialDto);

        // Act
        var result = await _sut.GetByIdAsync(credentialId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(credentialId);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCredentialNotExists_ReturnsNotFound()
    {
        // Arrange
        var credentialId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MembershipCredential?)null);

        // Act
        var result = await _sut.GetByIdAsync(credentialId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // ========================================================================
    // GetByProjectIdAsync
    // ========================================================================

    [Fact]
    public async Task GetByProjectIdAsync_ReturnsCredentialsForProject()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var credentials = new List<MembershipCredential>
        {
            CreateTestCredential(projectId: projectId),
            CreateTestCredential(projectId: projectId)
        };
        var credentialDtos = new List<MembershipCredentialDto>
        {
            CreateTestCredentialDto(projectId: projectId),
            CreateTestCredentialDto(projectId: projectId)
        };

        _repositoryMock.Setup(r => r.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<MembershipCredential, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(credentials);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<MembershipCredentialDto>>(credentials))
            .Returns(credentialDtos);

        // Act
        var result = await _sut.GetByProjectIdAsync(projectId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    // ========================================================================
    // GetByUserIdAsync
    // ========================================================================

    [Fact]
    public async Task GetByUserIdAsync_ReturnsCredentialsForUser()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var credentials = new List<MembershipCredential>
        {
            CreateTestCredential(userId: userId),
            CreateTestCredential(userId: userId)
        };
        var credentialDtos = new List<MembershipCredentialDto>
        {
            CreateTestCredentialDto(userId: userId),
            CreateTestCredentialDto(userId: userId)
        };

        _repositoryMock.Setup(r => r.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<MembershipCredential, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(credentials);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<MembershipCredentialDto>>(credentials))
            .Returns(credentialDtos);

        // Act
        var result = await _sut.GetByUserIdAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    // ========================================================================
    // GetByProjectAndUserAsync
    // ========================================================================

    [Fact]
    public async Task GetByProjectAndUserAsync_WhenCredentialExists_ReturnsSuccess()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var credential = CreateTestCredential(projectId: projectId, userId: userId);
        var credentialDto = CreateTestCredentialDto(projectId: projectId, userId: userId);

        _repositoryMock.Setup(r => r.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<MembershipCredential, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MembershipCredential> { credential });
        _mapperMock.Setup(m => m.Map<MembershipCredentialDto>(credential)).Returns(credentialDto);

        // Act
        var result = await _sut.GetByProjectAndUserAsync(projectId, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ProjectId.Should().Be(projectId);
        result.Value!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetByProjectAndUserAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();

        _repositoryMock.Setup(r => r.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<MembershipCredential, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MembershipCredential>());

        // Act
        var result = await _sut.GetByProjectAndUserAsync(projectId, userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // ========================================================================
    // GetActiveByProjectIdAsync
    // ========================================================================

    [Fact]
    public async Task GetActiveByProjectIdAsync_ReturnsOnlyActiveCredentials()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var activeCredentials = new List<MembershipCredential>
        {
            CreateTestCredential(projectId: projectId, status: MembershipCredentialStatus.ACTIVE)
        };
        var credentialDtos = new List<MembershipCredentialDto>
        {
            CreateTestCredentialDto(projectId: projectId, status: "ACTIVE")
        };

        _repositoryMock.Setup(r => r.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<MembershipCredential, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeCredentials);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<MembershipCredentialDto>>(activeCredentials))
            .Returns(credentialDtos);

        // Act
        var result = await _sut.GetActiveByProjectIdAsync(projectId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    // ========================================================================
    // GrantAsync - Mint non-transferable credential
    // ========================================================================

    [Fact]
    public async Task GrantAsync_WithValidInput_CreatesCredentialWithActiveStatusAndNonTransferable()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var dto = new GrantMembershipCredentialDto
        {
            ProjectId = projectId,
            UserId = userId,
            GrantedVia = "FOUNDER"
        };

        _repositoryMock.Setup(r => r.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<MembershipCredential, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MembershipCredential>());

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<MembershipCredential>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MembershipCredential c, CancellationToken _) => c);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<MembershipCredentialDto>(It.IsAny<MembershipCredential>()))
            .Returns((MembershipCredential c) => new MembershipCredentialDto
            {
                Id = c.id,
                ProjectId = c.projectId,
                UserId = c.userId,
                Status = c.status.ToString(),
                IsTransferable = c.isTransferable,
                GrantedVia = c.grantedVia.ToString(),
                CreatedAt = c.createdAt,
                UpdatedAt = c.updatedAt
            });

        // Act
        var result = await _sut.GrantAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ProjectId.Should().Be(projectId);
        result.Value!.UserId.Should().Be(userId);
        result.Value!.Status.Should().Be("ACTIVE");
        result.Value!.IsTransferable.Should().BeFalse("credentials are soulbound / non-transferable");
        result.Value!.GrantedVia.Should().Be("FOUNDER");

        _repositoryMock.Verify(r => r.AddAsync(
            It.Is<MembershipCredential>(c =>
                c.projectId == projectId &&
                c.userId == userId &&
                c.status == MembershipCredentialStatus.ACTIVE &&
                c.isTransferable == false &&
                c.grantedVia == MembershipGrantType.FOUNDER),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GrantAsync_WhenUserAlreadyHasActiveCredential_ReturnsValidationError()
    {
        // Arrange - enforce unique credential per project per user
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var dto = new GrantMembershipCredentialDto
        {
            ProjectId = projectId,
            UserId = userId,
            GrantedVia = "FOUNDER"
        };
        var existingCredential = CreateTestCredential(
            projectId: projectId,
            userId: userId,
            status: MembershipCredentialStatus.ACTIVE);

        _repositoryMock.Setup(r => r.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<MembershipCredential, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MembershipCredential> { existingCredential });

        // Act
        var result = await _sut.GrantAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("already has an active membership credential");
    }

    [Fact]
    public async Task GrantAsync_WhenUserHasSuspendedCredential_ReturnsValidationError()
    {
        // Arrange - a SUSPENDED credential still counts as existing (cannot re-mint)
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var dto = new GrantMembershipCredentialDto
        {
            ProjectId = projectId,
            UserId = userId,
            GrantedVia = "APPLICATION_APPROVED"
        };
        var existingCredential = CreateTestCredential(
            projectId: projectId,
            userId: userId,
            status: MembershipCredentialStatus.SUSPENDED);

        _repositoryMock.Setup(r => r.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<MembershipCredential, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MembershipCredential> { existingCredential });

        // Act
        var result = await _sut.GrantAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    [Fact]
    public async Task GrantAsync_WhenUserHasRevokedCredential_RemintsByReactivating()
    {
        // Arrange - a REVOKED credential can be re-minted
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var credentialId = Guid.NewGuid().ToString();
        var dto = new GrantMembershipCredentialDto
        {
            ProjectId = projectId,
            UserId = userId,
            GrantedVia = "DAO_VOTE",
            GrantedByProposalId = Guid.NewGuid().ToString()
        };
        var revokedCredential = CreateTestCredential(
            id: credentialId,
            projectId: projectId,
            userId: userId,
            status: MembershipCredentialStatus.REVOKED);

        _repositoryMock.Setup(r => r.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<MembershipCredential, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MembershipCredential> { revokedCredential });

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MembershipCredential>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<MembershipCredentialDto>(It.IsAny<MembershipCredential>()))
            .Returns((MembershipCredential c) => new MembershipCredentialDto
            {
                Id = c.id,
                ProjectId = c.projectId,
                UserId = c.userId,
                Status = c.status.ToString(),
                IsTransferable = c.isTransferable,
                GrantedVia = c.grantedVia.ToString(),
                CreatedAt = c.createdAt,
                UpdatedAt = c.updatedAt
            });

        // Act
        var result = await _sut.GrantAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("ACTIVE");
        result.Value!.IsTransferable.Should().BeFalse();
        _repositoryMock.Verify(r => r.UpdateAsync(
            It.Is<MembershipCredential>(c =>
                c.id == credentialId &&
                c.status == MembershipCredentialStatus.ACTIVE &&
                c.isTransferable == false &&
                c.revokedAt == null &&
                c.revokeTxHash == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GrantAsync_WithInvalidGrantType_ReturnsValidationError()
    {
        // Arrange
        var dto = new GrantMembershipCredentialDto
        {
            ProjectId = Guid.NewGuid().ToString(),
            UserId = Guid.NewGuid().ToString(),
            GrantedVia = "INVALID_TYPE"
        };

        // Act
        var result = await _sut.GrantAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("Invalid grant type");
    }

    [Fact]
    public async Task GrantAsync_WithProposalIdOnNonDaoVote_ReturnsValidationError()
    {
        // Arrange - grantedByProposalId should only be set for DAO_VOTE
        var dto = new GrantMembershipCredentialDto
        {
            ProjectId = Guid.NewGuid().ToString(),
            UserId = Guid.NewGuid().ToString(),
            GrantedVia = "FOUNDER",
            GrantedByProposalId = Guid.NewGuid().ToString()
        };

        // Act
        var result = await _sut.GrantAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("grantedByProposalId can only be set when grantedVia is DAO_VOTE");
    }

    // ========================================================================
    // RevokeAsync
    // ========================================================================

    [Fact]
    public async Task RevokeAsync_WhenActive_SetsRevokedStatusAndTimestamp()
    {
        // Arrange
        var credentialId = Guid.NewGuid().ToString();
        var credential = CreateTestCredential(id: credentialId, status: MembershipCredentialStatus.ACTIVE);

        _repositoryMock.Setup(r => r.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MembershipCredential>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<MembershipCredentialDto>(It.IsAny<MembershipCredential>()))
            .Returns((MembershipCredential c) => new MembershipCredentialDto
            {
                Id = c.id,
                Status = c.status.ToString(),
                RevokedAt = c.revokedAt,
                RevokeTxHash = c.revokeTxHash,
                CreatedAt = c.createdAt,
                UpdatedAt = c.updatedAt
            });

        var revokeDto = new RevokeMembershipCredentialDto { RevokeTxHash = "tx_hash_123" };

        // Act
        var result = await _sut.RevokeAsync(credentialId, revokeDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("REVOKED");
        result.Value!.RevokedAt.Should().NotBeNull();

        _repositoryMock.Verify(r => r.UpdateAsync(
            It.Is<MembershipCredential>(c =>
                c.status == MembershipCredentialStatus.REVOKED &&
                c.revokedAt != null &&
                c.revokeTxHash == "tx_hash_123"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeAsync_WhenAlreadyRevoked_ReturnsValidationError()
    {
        // Arrange
        var credentialId = Guid.NewGuid().ToString();
        var credential = CreateTestCredential(id: credentialId, status: MembershipCredentialStatus.REVOKED);

        _repositoryMock.Setup(r => r.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        // Act
        var result = await _sut.RevokeAsync(credentialId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("already revoked");
    }

    [Fact]
    public async Task RevokeAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var credentialId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MembershipCredential?)null);

        // Act
        var result = await _sut.RevokeAsync(credentialId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // ========================================================================
    // SuspendAsync
    // ========================================================================

    [Fact]
    public async Task SuspendAsync_WhenActive_SetsSuspendedStatus()
    {
        // Arrange
        var credentialId = Guid.NewGuid().ToString();
        var credential = CreateTestCredential(id: credentialId, status: MembershipCredentialStatus.ACTIVE);

        _repositoryMock.Setup(r => r.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MembershipCredential>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<MembershipCredentialDto>(It.IsAny<MembershipCredential>()))
            .Returns((MembershipCredential c) => new MembershipCredentialDto
            {
                Id = c.id,
                Status = c.status.ToString(),
                CreatedAt = c.createdAt,
                UpdatedAt = c.updatedAt
            });

        // Act
        var result = await _sut.SuspendAsync(credentialId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("SUSPENDED");
    }

    [Fact]
    public async Task SuspendAsync_WhenNotActive_ReturnsValidationError()
    {
        // Arrange - only ACTIVE credentials can be suspended
        var credentialId = Guid.NewGuid().ToString();
        var credential = CreateTestCredential(id: credentialId, status: MembershipCredentialStatus.REVOKED);

        _repositoryMock.Setup(r => r.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        // Act
        var result = await _sut.SuspendAsync(credentialId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("Only active credentials can be suspended");
    }

    [Fact]
    public async Task SuspendAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var credentialId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MembershipCredential?)null);

        // Act
        var result = await _sut.SuspendAsync(credentialId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // ========================================================================
    // ReactivateAsync
    // ========================================================================

    [Fact]
    public async Task ReactivateAsync_WhenSuspended_SetsActiveStatus()
    {
        // Arrange - only SUSPENDED -> ACTIVE transition is allowed
        var credentialId = Guid.NewGuid().ToString();
        var credential = CreateTestCredential(id: credentialId, status: MembershipCredentialStatus.SUSPENDED);

        _repositoryMock.Setup(r => r.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MembershipCredential>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<MembershipCredentialDto>(It.IsAny<MembershipCredential>()))
            .Returns((MembershipCredential c) => new MembershipCredentialDto
            {
                Id = c.id,
                Status = c.status.ToString(),
                CreatedAt = c.createdAt,
                UpdatedAt = c.updatedAt
            });

        // Act
        var result = await _sut.ReactivateAsync(credentialId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("ACTIVE");
    }

    [Fact]
    public async Task ReactivateAsync_WhenRevoked_ReturnsValidationError()
    {
        // Arrange - REVOKED credentials cannot be reactivated (must re-mint via Grant)
        var credentialId = Guid.NewGuid().ToString();
        var credential = CreateTestCredential(id: credentialId, status: MembershipCredentialStatus.REVOKED);

        _repositoryMock.Setup(r => r.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        // Act
        var result = await _sut.ReactivateAsync(credentialId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("Only suspended credentials can be reactivated");
    }

    [Fact]
    public async Task ReactivateAsync_WhenActive_ReturnsValidationError()
    {
        // Arrange - ACTIVE credentials cannot be reactivated (already active)
        var credentialId = Guid.NewGuid().ToString();
        var credential = CreateTestCredential(id: credentialId, status: MembershipCredentialStatus.ACTIVE);

        _repositoryMock.Setup(r => r.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        // Act
        var result = await _sut.ReactivateAsync(credentialId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    [Fact]
    public async Task ReactivateAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var credentialId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MembershipCredential?)null);

        // Act
        var result = await _sut.ReactivateAsync(credentialId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // ========================================================================
    // UpdateMintInfoAsync
    // ========================================================================

    [Fact]
    public async Task UpdateMintInfoAsync_WhenCredentialExists_UpdatesMintInfo()
    {
        // Arrange
        var credentialId = Guid.NewGuid().ToString();
        var credential = CreateTestCredential(id: credentialId);
        var mintDto = new UpdateMembershipCredentialMintDto { MintTxHash = "tx_mint_abc" };

        _repositoryMock.Setup(r => r.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<MembershipCredential>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<MembershipCredentialDto>(It.IsAny<MembershipCredential>()))
            .Returns((MembershipCredential c) => new MembershipCredentialDto
            {
                Id = c.id,
                MintTxHash = c.mintTxHash,
                MintedAt = c.mintedAt,
                CreatedAt = c.createdAt,
                UpdatedAt = c.updatedAt
            });

        // Act
        var result = await _sut.UpdateMintInfoAsync(credentialId, mintDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.MintTxHash.Should().Be("tx_mint_abc");
        result.Value!.MintedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateMintInfoAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var credentialId = Guid.NewGuid().ToString();
        var mintDto = new UpdateMembershipCredentialMintDto { MintTxHash = "tx_mint_abc" };

        _repositoryMock.Setup(r => r.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MembershipCredential?)null);

        // Act
        var result = await _sut.UpdateMintInfoAsync(credentialId, mintDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private static MembershipCredential CreateTestCredential(
        string? id = null,
        string? projectId = null,
        string? userId = null,
        MembershipCredentialStatus status = MembershipCredentialStatus.ACTIVE,
        MembershipGrantType grantedVia = MembershipGrantType.FOUNDER)
    {
        return new MembershipCredential
        {
            id = id ?? Guid.NewGuid().ToString(),
            projectId = projectId ?? Guid.NewGuid().ToString(),
            userId = userId ?? Guid.NewGuid().ToString(),
            status = status,
            isTransferable = false,
            grantedVia = grantedVia,
            mintedAt = DateTime.UtcNow,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
    }

    private static MembershipCredentialDto CreateTestCredentialDto(
        string? id = null,
        string? projectId = null,
        string? userId = null,
        string status = "ACTIVE",
        string grantedVia = "FOUNDER")
    {
        return new MembershipCredentialDto
        {
            Id = id ?? Guid.NewGuid().ToString(),
            ProjectId = projectId ?? Guid.NewGuid().ToString(),
            UserId = userId ?? Guid.NewGuid().ToString(),
            Status = status,
            IsTransferable = false,
            GrantedVia = grantedVia,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
