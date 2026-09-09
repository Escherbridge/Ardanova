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

/// <summary>
/// The publish rule has two entry points — the edit route and the publish route — and these tests
/// prove both answer to one predicate, asserted at the counter: a refused move leaves the stored
/// status and the publish date exactly as they were, with nothing written.
/// </summary>
public class ProjectPublishTransitionTests
{
    private static readonly DateTime FirstPublishDate = new(2026, 3, 14, 9, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IProjectRepository> _repositoryMock = new();
    private readonly Mock<IRepository<User>> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly ProjectService _sut;

    public ProjectPublishTransitionTests()
    {
        _mapperMock.Setup(mapper => mapper.Map<ProjectDto>(It.IsAny<Project>()))
            .Returns(new ProjectDto { Title = "Community garden" });
        _userRepositoryMock.Setup(repository => repository.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _repositoryMock.Setup(repository => repository.UpdateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _sut = new ProjectService(
            _repositoryMock.Object,
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task UpdateAsync_MovingACancelledProjectToPublished_IsRefusedAndNothingMoves()
    {
        var project = GivenStoredProject(ProjectStatus.CANCELLED);

        var result = await _sut.UpdateAsync(project.id, new UpdateProjectDto { Status = ProjectStatus.PUBLISHED });

        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("CANCELLED").And.Contain("PUBLISHED");
        project.status.Should().Be(ProjectStatus.CANCELLED);
        project.publishedAt.Should().BeNull();
        NothingWasWritten();
    }

    [Fact]
    public async Task PublishAsync_OnACancelledProject_IsRefusedAndNothingMoves()
    {
        var project = GivenStoredProject(ProjectStatus.CANCELLED);

        var result = await _sut.PublishAsync(project.id);

        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("CANCELLED").And.Contain("PUBLISHED");
        project.status.Should().Be(ProjectStatus.CANCELLED);
        project.publishedAt.Should().BeNull();
        NothingWasWritten();
    }

    /// <summary>One predicate, two doors: the same move is refused with the same words whichever route asks.</summary>
    [Fact]
    public async Task ACancelledProjectIsRefusedIdenticallyByBothSeams()
    {
        var editedProject = GivenStoredProject(ProjectStatus.CANCELLED);
        var publishedProject = GivenStoredProject(ProjectStatus.CANCELLED);

        var editResult = await _sut.UpdateAsync(editedProject.id, new UpdateProjectDto { Status = ProjectStatus.PUBLISHED });
        var publishResult = await _sut.PublishAsync(publishedProject.id);

        editResult.Type.Should().Be(ResultType.ValidationError);
        publishResult.Type.Should().Be(ResultType.ValidationError);
        editResult.Error.Should().Be(publishResult.Error, "both seams answer to the same transition predicate");
        NothingWasWritten();
    }

    [Fact]
    public async Task PublishAsync_CalledTwice_KeepsTheFirstPublishDateAndWritesOnce()
    {
        var project = GivenStoredProject(ProjectStatus.DRAFT);

        var firstPublish = await _sut.PublishAsync(project.id);
        firstPublish.IsSuccess.Should().BeTrue();
        project.status.Should().Be(ProjectStatus.PUBLISHED);
        var stampedDate = project.publishedAt;
        stampedDate.Should().NotBeNull();

        var secondPublish = await _sut.PublishAsync(project.id);

        secondPublish.IsSuccess.Should().BeTrue();
        project.publishedAt.Should().Be(stampedDate);
        _repositoryMock.Verify(
            repository => repository.UpdateAsync(project, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>The second publish through the other door must not move the date the first one set either.</summary>
    [Fact]
    public async Task UpdateAsync_RepublishingAnAlreadyPublishedProject_KeepsTheFirstPublishDate()
    {
        var project = GivenStoredProject(ProjectStatus.PUBLISHED, FirstPublishDate);

        var result = await _sut.UpdateAsync(project.id, new UpdateProjectDto { Status = ProjectStatus.PUBLISHED });

        result.IsSuccess.Should().BeTrue();
        project.publishedAt.Should().Be(FirstPublishDate);
    }

    [Fact]
    public async Task PublishAsync_AfterAReopen_KeepsTheOriginalPublishDate()
    {
        var project = GivenStoredProject(ProjectStatus.CANCELLED, FirstPublishDate);

        var reopen = await _sut.UpdateAsync(project.id, new UpdateProjectDto { Status = ProjectStatus.DRAFT });
        var republish = await _sut.PublishAsync(project.id);

        reopen.IsSuccess.Should().BeTrue();
        republish.IsSuccess.Should().BeTrue();
        project.status.Should().Be(ProjectStatus.PUBLISHED);
        project.publishedAt.Should().Be(FirstPublishDate, "the first publish is a fact of the record");
    }

    [Fact]
    public async Task UpdateAsync_ReopeningACancelledProjectAsADraft_IsTheOneWayBack()
    {
        var project = GivenStoredProject(ProjectStatus.CANCELLED);

        var result = await _sut.UpdateAsync(project.id, new UpdateProjectDto { Status = ProjectStatus.DRAFT });

        result.IsSuccess.Should().BeTrue();
        project.status.Should().Be(ProjectStatus.DRAFT);
        project.publishedAt.Should().BeNull("a reopened draft is private again until it is published");
    }

    [Fact]
    public async Task UpdateAsync_CancellingADraft_DoesNotStampAPublishDate()
    {
        var project = GivenStoredProject(ProjectStatus.DRAFT);

        var result = await _sut.UpdateAsync(project.id, new UpdateProjectDto { Status = ProjectStatus.CANCELLED });

        result.IsSuccess.Should().BeTrue();
        project.status.Should().Be(ProjectStatus.CANCELLED);
        project.publishedAt.Should().BeNull("an ask withdrawn before it ever went live was never published");
    }

    [Fact]
    public async Task UpdateAsync_SkippingTheWholeLifecycle_IsRefused()
    {
        var project = GivenStoredProject(ProjectStatus.DRAFT);

        var result = await _sut.UpdateAsync(project.id, new UpdateProjectDto { Status = ProjectStatus.COMPLETED });

        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("DRAFT").And.Contain("COMPLETED");
        project.status.Should().Be(ProjectStatus.DRAFT);
        NothingWasWritten();
    }

    [Fact]
    public async Task UpdateAsync_WithNoStatusOnTheWire_LeavesTheStatusAndTheDateAlone()
    {
        var project = GivenStoredProject(ProjectStatus.SEEKING_SUPPORT, FirstPublishDate);

        var result = await _sut.UpdateAsync(project.id, new UpdateProjectDto { Title = "Community garden, phase two" });

        result.IsSuccess.Should().BeTrue();
        project.status.Should().Be(ProjectStatus.SEEKING_SUPPORT);
        project.title.Should().Be("Community garden, phase two");
        project.publishedAt.Should().Be(FirstPublishDate);
    }

    /// <summary>A live project that predates the publish date column gets its stamp without a status move.</summary>
    [Fact]
    public async Task PublishAsync_OnALiveProjectMissingItsDate_StampsWithoutChangingTheStatus()
    {
        var project = GivenStoredProject(ProjectStatus.SEEKING_SUPPORT);

        var result = await _sut.PublishAsync(project.id);

        result.IsSuccess.Should().BeTrue();
        project.status.Should().Be(ProjectStatus.SEEKING_SUPPORT);
        project.publishedAt.Should().NotBeNull();
    }

    private Project GivenStoredProject(ProjectStatus status, DateTime? publishedAt = null)
    {
        var project = new Project
        {
            id = Guid.NewGuid().ToString(),
            createdById = Guid.NewGuid().ToString(),
            title = "Community garden",
            description = "A shared plot on the corner lot",
            problemStatement = "The block has nowhere to grow food",
            solution = "Clear the lot and plant it together",
            categories = "COMMUNITY",
            status = status,
            publishedAt = publishedAt,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(repository => repository.GetByIdAsync(project.id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        return project;
    }

    /// <summary>The counter did not move: a refused transition reaches neither the repository nor the unit of work.</summary>
    private void NothingWasWritten()
    {
        _repositoryMock.Verify(
            repository => repository.UpdateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWorkMock.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
