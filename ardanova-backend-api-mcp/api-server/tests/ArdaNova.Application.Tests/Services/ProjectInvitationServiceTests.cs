namespace ArdaNova.Application.Tests.Services;

using System.Linq.Expressions;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using FluentAssertions;
using Moq;

public class ProjectInvitationServiceTests
{
    private readonly Mock<IRepository<ProjectInvitation>> _invitationRepoMock;
    private readonly Mock<IRepository<ProjectMember>> _memberRepoMock;
    private readonly Mock<IRepository<User>> _userRepoMock;
    private readonly Mock<IRepository<Project>> _projectRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly ProjectInvitationService _sut;

    public ProjectInvitationServiceTests()
    {
        _invitationRepoMock = new Mock<IRepository<ProjectInvitation>>();
        _memberRepoMock = new Mock<IRepository<ProjectMember>>();
        _userRepoMock = new Mock<IRepository<User>>();
        _projectRepoMock = new Mock<IRepository<Project>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();

        _sut = new ProjectInvitationService(
            _invitationRepoMock.Object,
            _memberRepoMock.Object,
            _userRepoMock.Object,
            _projectRepoMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object);
    }

    private static ProjectInvitationDto MakeMappedDto(string id, string projectId, string role, InvitationStatus status) =>
        new()
        {
            Id = id,
            ProjectId = projectId,
            InvitedById = "inviter-1",
            Role = role,
            Status = status.ToString(),
            CreatedAt = DateTime.UtcNow
        };

    // ========================================================================
    // CreateAsync — Happy Path
    // ========================================================================

    [Fact]
    public async Task CreateAsync_HappyPath_CreatesInvitationAndReturnsDto()
    {
        // Arrange
        var dto = new CreateProjectInvitationDto
        {
            ProjectId = "project-1",
            InvitedById = "owner-1",
            InvitedUserId = "user-1",
            Role = "CONTRIBUTOR",
            Message = "Join us!"
        };

        _invitationRepoMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProjectInvitation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectInvitation>());

        _memberRepoMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProjectMember, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectMember>());

        _mapperMock
            .Setup(m => m.Map<ProjectInvitationDto>(It.IsAny<ProjectInvitation>()))
            .Returns(MakeMappedDto("inv-1", "project-1", "CONTRIBUTOR", InvitationStatus.PENDING));

        _projectRepoMock
            .Setup(r => r.GetByIdAsync("project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { id = "project-1", title = "Test", slug = "test" });

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("PENDING");
        result.Value.Role.Should().Be("CONTRIBUTOR");

        _invitationRepoMock.Verify(r => r.AddAsync(It.IsAny<ProjectInvitation>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ========================================================================
    // CreateAsync — Duplicate Pending Invitation
    // ========================================================================

    [Fact]
    public async Task CreateAsync_DuplicatePending_ReturnsValidationError()
    {
        // Arrange
        var dto = new CreateProjectInvitationDto
        {
            ProjectId = "project-1",
            InvitedById = "owner-1",
            InvitedUserId = "user-1",
            Role = "CONTRIBUTOR"
        };

        _invitationRepoMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProjectInvitation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectInvitation>
            {
                new() { id = "existing", projectId = "project-1", invitedUserId = "user-1", status = InvitationStatus.PENDING }
            });

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("pending invitation already exists");
    }

    // ========================================================================
    // CreateAsync — User Already a Member
    // ========================================================================

    [Fact]
    public async Task CreateAsync_UserAlreadyMember_ReturnsValidationError()
    {
        // Arrange
        var dto = new CreateProjectInvitationDto
        {
            ProjectId = "project-1",
            InvitedById = "owner-1",
            InvitedUserId = "user-1",
            Role = "CONTRIBUTOR"
        };

        _invitationRepoMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProjectInvitation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectInvitation>());

        _memberRepoMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProjectMember, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectMember>
            {
                new() { id = "member-1", projectId = "project-1", userId = "user-1", role = ProjectRole.CONTRIBUTOR }
            });

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("already has a role");
    }

    // ========================================================================
    // AcceptAsync — Happy Path (auto-creates member)
    // ========================================================================

    [Fact]
    public async Task AcceptAsync_HappyPath_SetsAcceptedAndCreatesMember()
    {
        // Arrange
        var invitation = new ProjectInvitation
        {
            id = "inv-1",
            projectId = "project-1",
            invitedById = "owner-1",
            invitedUserId = "user-1",
            role = ProjectRole.CONTRIBUTOR,
            status = InvitationStatus.PENDING,
            createdAt = DateTime.UtcNow
        };

        _invitationRepoMock
            .Setup(r => r.GetByIdAsync("inv-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        _memberRepoMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProjectMember, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectMember>());

        _mapperMock
            .Setup(m => m.Map<ProjectInvitationDto>(It.IsAny<ProjectInvitation>()))
            .Returns(MakeMappedDto("inv-1", "project-1", "CONTRIBUTOR", InvitationStatus.ACCEPTED));

        _projectRepoMock
            .Setup(r => r.GetByIdAsync("project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { id = "project-1", title = "Test", slug = "test" });

        // Act
        var result = await _sut.AcceptAsync("inv-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.status.Should().Be(InvitationStatus.ACCEPTED);
        invitation.respondedAt.Should().NotBeNull();

        _memberRepoMock.Verify(r => r.AddAsync(
            It.Is<ProjectMember>(m => m.userId == "user-1" && m.role == ProjectRole.CONTRIBUTOR),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ========================================================================
    // AcceptAsync — Already a Member
    // ========================================================================

    [Fact]
    public async Task AcceptAsync_AlreadyMember_ReturnsValidationError()
    {
        // Arrange
        var invitation = new ProjectInvitation
        {
            id = "inv-1",
            projectId = "project-1",
            invitedById = "owner-1",
            invitedUserId = "user-1",
            role = ProjectRole.CONTRIBUTOR,
            status = InvitationStatus.PENDING,
            createdAt = DateTime.UtcNow
        };

        _invitationRepoMock
            .Setup(r => r.GetByIdAsync("inv-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        _memberRepoMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProjectMember, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectMember>
            {
                new() { id = "member-1", projectId = "project-1", userId = "user-1", role = ProjectRole.OBSERVER }
            });

        // Act
        var result = await _sut.AcceptAsync("inv-1");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("already has a role");
    }

    // ========================================================================
    // RejectAsync — Sets Declined
    // ========================================================================

    [Fact]
    public async Task RejectAsync_HappyPath_SetsDeclined()
    {
        // Arrange
        var invitation = new ProjectInvitation
        {
            id = "inv-1",
            projectId = "project-1",
            invitedById = "owner-1",
            invitedUserId = "user-1",
            role = ProjectRole.CONTRIBUTOR,
            status = InvitationStatus.PENDING,
            createdAt = DateTime.UtcNow
        };

        _invitationRepoMock
            .Setup(r => r.GetByIdAsync("inv-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        _mapperMock
            .Setup(m => m.Map<ProjectInvitationDto>(It.IsAny<ProjectInvitation>()))
            .Returns(MakeMappedDto("inv-1", "project-1", "CONTRIBUTOR", InvitationStatus.DECLINED));

        _projectRepoMock
            .Setup(r => r.GetByIdAsync("project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { id = "project-1", title = "Test", slug = "test" });

        // Act
        var result = await _sut.RejectAsync("inv-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.status.Should().Be(InvitationStatus.DECLINED);
        invitation.respondedAt.Should().NotBeNull();

        _memberRepoMock.Verify(r => r.AddAsync(It.IsAny<ProjectMember>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ========================================================================
    // AcceptAsync — Not Pending
    // ========================================================================

    [Fact]
    public async Task AcceptAsync_NotPending_ReturnsValidationError()
    {
        // Arrange
        var invitation = new ProjectInvitation
        {
            id = "inv-1",
            projectId = "project-1",
            invitedById = "owner-1",
            invitedUserId = "user-1",
            role = ProjectRole.CONTRIBUTOR,
            status = InvitationStatus.ACCEPTED,
            createdAt = DateTime.UtcNow
        };

        _invitationRepoMock
            .Setup(r => r.GetByIdAsync("inv-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        // Act
        var result = await _sut.AcceptAsync("inv-1");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("no longer pending");
    }
}
