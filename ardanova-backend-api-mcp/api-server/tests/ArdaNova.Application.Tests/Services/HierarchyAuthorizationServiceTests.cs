namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using FluentAssertions;
using Moq;

public class HierarchyAuthorizationServiceTests
{
    [Fact]
    public async Task CanManageProjectAsync_LeaderMember_ReturnsTrue()
    {
        var projects = new Mock<IProjectService>();
        projects.Setup(service => service.GetByIdAsync("project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectDto>.Success(Project("project-1")));
        var members = new Mock<IProjectMemberService>();
        members.Setup(service => service.GetByProjectIdAsync("project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<ProjectMemberDto>>.Success([
                new ProjectMemberDto
                {
                    Id = "member-1",
                    ProjectId = "project-1",
                    UserId = "leader-1",
                    Role = ProjectRole.LEADER
                }
            ]));
        var service = CreateService(projects, members);

        var allowed = await service.CanManageProjectAsync("leader-1", "project-1");

        allowed.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveCommentTargetProjectAsync_CrossProjectFeatureChain_ReturnsNull()
    {
        var projects = new Mock<IProjectService>();
        projects.Setup(service => service.GetByIdAsync("project-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectDto>.Success(Project("project-2")));
        var milestones = new Mock<IProjectMilestoneService>();
        milestones.Setup(service => service.GetByIdAsync("milestone-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectMilestoneDto>.Success(new ProjectMilestoneDto
            {
                Id = "milestone-1",
                ProjectId = "project-2",
                Title = "Milestone"
            }));
        var epics = new Mock<IEpicService>();
        epics.Setup(service => service.GetByIdAsync("epic-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<EpicDto>.Success(new EpicDto
            {
                Id = "epic-1",
                ProjectId = "project-2",
                MilestoneId = "milestone-1",
                Title = "Epic"
            }));
        var sprints = new Mock<ISprintService>();
        sprints.Setup(service => service.GetByIdAsync("sprint-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SprintDto>.Success(new SprintDto
            {
                Id = "sprint-1",
                ProjectId = "project-2",
                EpicId = "epic-1",
                Name = "Sprint"
            }));
        var features = new Mock<IFeatureService>();
        features.Setup(service => service.GetByIdAsync("feature-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FeatureDto>.Success(new FeatureDto
            {
                Id = "feature-1",
                ProjectId = "project-1",
                SprintId = "sprint-1",
                Title = "Feature"
            }));
        var service = CreateService(
            projects,
            milestones: milestones,
            epics: epics,
            sprints: sprints,
            features: features);

        var projectId = await service.ResolveCommentTargetProjectAsync(
            CommentTargetType.FEATURE,
            "feature-1");

        projectId.Should().BeNull();
    }

    [Fact]
    public async Task ResolveCommentTargetProjectAsync_UnrelatedPbiAndFeature_ReturnsNull()
    {
        var projects = new Mock<IProjectService>();
        projects.Setup(service => service.GetByIdAsync("project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectDto>.Success(Project("project-1")));
        var milestones = new Mock<IProjectMilestoneService>();
        milestones.Setup(service => service.GetByIdAsync("milestone-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectMilestoneDto>.Success(new ProjectMilestoneDto
            {
                Id = "milestone-1",
                ProjectId = "project-1",
                Title = "Milestone"
            }));
        var epics = new Mock<IEpicService>();
        epics.Setup(service => service.GetByIdAsync("epic-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<EpicDto>.Success(new EpicDto
            {
                Id = "epic-1",
                ProjectId = "project-1",
                MilestoneId = "milestone-1",
                Title = "Epic"
            }));
        var sprints = new Mock<ISprintService>();
        sprints.Setup(service => service.GetByIdAsync("sprint-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SprintDto>.Success(new SprintDto
            {
                Id = "sprint-1",
                ProjectId = "project-1",
                EpicId = "epic-1",
                Name = "Sprint"
            }));
        var features = new Mock<IFeatureService>();
        features.Setup(service => service.GetByIdAsync("feature-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FeatureDto>.Success(new FeatureDto
            {
                Id = "feature-1",
                ProjectId = "project-1",
                SprintId = "sprint-1",
                Title = "Feature"
            }));
        var backlog = new Mock<IProductBacklogItemService>();
        backlog.Setup(service => service.GetByIdAsync("pbi-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProductBacklogItemDto>.Success(new ProductBacklogItemDto
            {
                Id = "pbi-1",
                ProjectId = "project-1",
                Title = "Root PBI"
            }));
        var tasks = new Mock<ITaskService>();
        tasks.Setup(service => service.GetByIdAsync("task-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDto>.Success(new TaskDto
            {
                Id = "task-1",
                ProjectId = "project-1",
                PbiId = "pbi-1",
                FeatureId = "feature-1",
                Title = "Task"
            }));
        var service = CreateService(
            projects,
            milestones: milestones,
            epics: epics,
            sprints: sprints,
            features: features,
            backlog: backlog,
            tasks: tasks);

        var projectId = await service.ResolveCommentTargetProjectAsync(
            CommentTargetType.TASK,
            "task-1");

        projectId.Should().BeNull();
    }

    private static HierarchyAuthorizationService CreateService(
        Mock<IProjectService> projects,
        Mock<IProjectMemberService>? members = null,
        Mock<IProjectMilestoneService>? milestones = null,
        Mock<IEpicService>? epics = null,
        Mock<ISprintService>? sprints = null,
        Mock<IFeatureService>? features = null,
        Mock<IProductBacklogItemService>? backlog = null,
        Mock<ITaskService>? tasks = null)
        => new(
            projects.Object,
            (members ?? new Mock<IProjectMemberService>()).Object,
            (milestones ?? new Mock<IProjectMilestoneService>()).Object,
            (epics ?? new Mock<IEpicService>()).Object,
            (sprints ?? new Mock<ISprintService>()).Object,
            (features ?? new Mock<IFeatureService>()).Object,
            (backlog ?? new Mock<IProductBacklogItemService>()).Object,
            (tasks ?? new Mock<ITaskService>()).Object);

    private static ProjectDto Project(string id) => new()
    {
        Id = id,
        CreatedById = "owner-1",
        Title = "Project",
        Slug = id,
        Description = ""
    };
}
