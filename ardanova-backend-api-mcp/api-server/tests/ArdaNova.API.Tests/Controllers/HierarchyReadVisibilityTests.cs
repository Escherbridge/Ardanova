namespace ArdaNova.API.Tests.Controllers;

using System.Security.Claims;
using ArdaNova.API.Controllers;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// An outsider reading a DRAFT project's backlog (epics, sprints, features, PBIs, by id, by project
/// or by parent) gets the same 404 as for something that does not exist; the team still reads it.
/// Writes follow the same rule: a refusal must not confirm that a hidden item exists.
/// </summary>
public class HierarchyReadVisibilityTests
{
    private const string ProjectId = "project-1";

    public static TheoryData<string> Reads =>
    [
        "epic", "projectEpics", "milestoneEpics",
        "sprint", "epicSprints", "activeSprint",
        "feature", "sprintFeatures",
        "pbi", "featurePbis", "projectPbis",
    ];

    [Theory]
    [MemberData(nameof(Reads))]
    public async Task AHiddenProjectsBacklogReadsExactlyLikeAMissingOne(string read)
    {
        var hidden = await Invoke(read, new Backlog(projectVisible: false, exists: true));
        var missing = await Invoke(read, new Backlog(projectVisible: false, exists: false));

        var hiddenBody = hidden.Should().BeOfType<NotFoundObjectResult>().Which.Value;
        missing.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().BeEquivalentTo(hiddenBody);
    }

    [Theory]
    [MemberData(nameof(Reads))]
    public async Task AViewerWhoCanSeeTheProject_ReadsItsBacklog(string read)
    {
        var result = await Invoke(read, new Backlog(projectVisible: true, exists: true));

        result.Should().BeOfType<OkObjectResult>();
    }

    public static TheoryData<string> Writes =>
    [
        "createEpic", "updateEpic", "deleteEpic",
        "createSprint", "updateSprint", "deleteSprint",
        "createFeature", "updateFeature", "deleteFeature",
    ];

    [Theory]
    [MemberData(nameof(Writes))]
    public async Task AnOutsidersWriteToAHiddenProjectsBacklog_ReadsExactlyLikeAMissingItem(string write)
    {
        var hidden = await InvokeWrite(write, new Backlog(projectVisible: false, exists: true), OutsiderAuthorization());
        var missing = await InvokeWrite(write, new Backlog(projectVisible: false, exists: false), OutsiderAuthorization());

        var hiddenBody = hidden.Should().BeOfType<NotFoundObjectResult>().Which.Value;
        missing.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().BeEquivalentTo(hiddenBody);
    }

    [Theory]
    [MemberData(nameof(Writes))]
    public async Task AnOutsidersWriteToAVisibleProjectsBacklog_IsStillForbidden(string write)
    {
        var result = await InvokeWrite(write, new Backlog(projectVisible: true, exists: true), OutsiderAuthorization());

        result.Should().BeOfType<ForbidResult>();
    }

    private static Task<IActionResult> InvokeWrite(string write, Backlog backlog, IHierarchyAuthorizationService authorization)
    {
        var ct = CancellationToken.None;
        var epics = SignedIn(new EpicsController(backlog.Epics, backlog.Milestones, authorization, backlog.Visibility));
        var sprints = SignedIn(new SprintsController(backlog.Sprints, backlog.Epics, authorization, backlog.Visibility));
        var features = SignedIn(new FeaturesController(backlog.Features, backlog.Sprints, authorization, backlog.Visibility));
        return write switch
        {
            "createEpic" => epics.Create(new CreateEpicDto { MilestoneId = "milestone-1", Title = "x" }, ct),
            "updateEpic" => epics.Update("epic-1", new UpdateEpicDto { Title = "x" }, ct),
            "deleteEpic" => epics.Delete("epic-1", ct),
            "createSprint" => sprints.Create(new CreateSprintDto { EpicId = "epic-1", Name = "x" }, ct),
            "updateSprint" => sprints.Update("sprint-1", new UpdateSprintDto { Name = "x" }, ct),
            "deleteSprint" => sprints.Delete("sprint-1", ct),
            "createFeature" => features.Create(new CreateFeatureDto { SprintId = "sprint-1", Title = "x" }, ct),
            "updateFeature" => features.Update("feature-1", new UpdateFeatureDto { Title = "x" }, ct),
            "deleteFeature" => features.Delete("feature-1", ct),
            _ => throw new ArgumentOutOfRangeException(nameof(write)),
        };
    }

    /// <summary>A signed-in actor who is not on the project: ancestry checks out, every authority question is no.</summary>
    private static IHierarchyAuthorizationService OutsiderAuthorization()
    {
        var authorization = new Mock<IHierarchyAuthorizationService>();
        authorization.Setup(a => a.ResolveCommentTargetProjectAsync(It.IsAny<CommentTargetType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectId);
        return authorization.Object;
    }

    private static T SignedIn<T>(T controller) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "outsider-1"), new Claim(ActorAssertionMiddleware.ClaimType, "v2")],
                    ActorAssertionMiddleware.AuthenticationType)),
            },
        };
        return controller;
    }

    private static Task<IActionResult> Invoke(string read, Backlog backlog)
    {
        var ct = CancellationToken.None;
        return read switch
        {
            "epic" => Anonymous(new EpicsController(backlog.Epics, backlog.Milestones, Authorization(), backlog.Visibility)).GetById("epic-1", ct),
            "projectEpics" => Anonymous(new ProjectEpicsController(backlog.Epics, backlog.Visibility)).GetByProjectId(ProjectId, ct),
            "milestoneEpics" => Anonymous(new MilestoneEpicsController(backlog.Epics, backlog.Milestones, Authorization(), backlog.Visibility)).GetByMilestoneId("milestone-1", ct),
            "sprint" => Anonymous(Sprints(backlog)).GetById("sprint-1", ct),
            "epicSprints" => Anonymous(Sprints(backlog)).GetByEpicId("epic-1", ct),
            "activeSprint" => Anonymous(Sprints(backlog)).GetActiveSprint("epic-1", ct),
            "feature" => Anonymous(new FeaturesController(backlog.Features, backlog.Sprints, Authorization(), backlog.Visibility)).GetById("feature-1", ct),
            "sprintFeatures" => Anonymous(new SprintFeaturesController(backlog.Features, backlog.Sprints, backlog.Visibility)).GetBySprintId("sprint-1", ct),
            "pbi" => Anonymous(Pbis(backlog)).GetById("pbi-1", ct),
            "featurePbis" => Anonymous(Pbis(backlog)).GetByFeatureId("feature-1", ct),
            "projectPbis" => Anonymous(Pbis(backlog)).GetByProjectId(ProjectId, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(read)),
        };
    }

    private static SprintsController Sprints(Backlog backlog)
        => new(backlog.Sprints, backlog.Epics, Authorization(), backlog.Visibility);

    private static ProductBacklogItemsController Pbis(Backlog backlog)
        => new(
            backlog.Pbis,
            new Mock<IProjectService>(MockBehavior.Strict).Object,
            backlog.Milestones,
            backlog.Epics,
            backlog.Sprints,
            backlog.Features,
            Authorization(),
            backlog.Visibility);

    private static IHierarchyAuthorizationService Authorization()
        => new Mock<IHierarchyAuthorizationService>(MockBehavior.Strict).Object;

    private static T Anonymous<T>(T controller) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    /// <summary>One milestone > epic > sprint > feature > PBI chain in project-1, present or not, visible or not.</summary>
    private sealed class Backlog
    {
        public IProjectVisibility Visibility { get; }
        public IProjectMilestoneService Milestones { get; }
        public IEpicService Epics { get; }
        public ISprintService Sprints { get; }
        public IFeatureService Features { get; }
        public IProductBacklogItemService Pbis { get; }

        public Backlog(bool projectVisible, bool exists)
        {
            var visibility = new Mock<IProjectVisibility>();
            visibility.Setup(v => v.GetVisibleByIdAsync(It.IsAny<ProjectViewer>(), ProjectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(projectVisible
                    ? Result<ProjectDto>.Success(new ProjectDto { Id = ProjectId, CreatedById = "founder", Title = "Draft", Slug = "draft", Description = "" })
                    : Result<ProjectDto>.NotFound($"Project with id {ProjectId} not found"));
            Visibility = visibility.Object;

            var milestones = new Mock<IProjectMilestoneService>();
            milestones.Setup(s => s.GetByIdAsync("milestone-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Found(exists, new ProjectMilestoneDto { Id = "milestone-1", ProjectId = ProjectId, Title = "M" }, "Milestone with id milestone-1 not found"));
            Milestones = milestones.Object;

            var epics = new Mock<IEpicService>();
            epics.Setup(s => s.GetByIdAsync("epic-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Found(exists, new EpicDto { Id = "epic-1", ProjectId = ProjectId, MilestoneId = "milestone-1", Title = "secret epic" }, "Epic with id epic-1 not found"));
            epics.Setup(s => s.GetByProjectIdAsync(ProjectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<EpicDto>>.Success([]));
            epics.Setup(s => s.GetByMilestoneIdAsync("milestone-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<EpicDto>>.Success([]));
            Epics = epics.Object;

            var sprint = new SprintDto { Id = "sprint-1", ProjectId = ProjectId, EpicId = "epic-1", Name = "secret sprint" };
            var sprints = new Mock<ISprintService>();
            sprints.Setup(s => s.GetByIdAsync("sprint-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Found(exists, sprint, "Sprint with id sprint-1 not found"));
            sprints.Setup(s => s.GetByEpicIdAsync("epic-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<SprintDto>>.Success([]));
            sprints.Setup(s => s.GetActiveByEpicIdAsync("epic-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SprintDto>.Success(sprint));
            Sprints = sprints.Object;

            var features = new Mock<IFeatureService>();
            features.Setup(s => s.GetByIdAsync("feature-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Found(exists, new FeatureDto { Id = "feature-1", ProjectId = ProjectId, SprintId = "sprint-1", Title = "secret feature" }, "Feature with id feature-1 not found"));
            features.Setup(s => s.GetBySprintIdAsync("sprint-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<FeatureDto>>.Success([]));
            Features = features.Object;

            var pbis = new Mock<IProductBacklogItemService>();
            pbis.Setup(s => s.GetByIdAsync("pbi-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Found(exists, new ProductBacklogItemDto { Id = "pbi-1", ProjectId = ProjectId, Title = "secret pbi" }, "Product backlog item with id pbi-1 not found"));
            pbis.Setup(s => s.GetByFeatureIdAsync("feature-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<ProductBacklogItemDto>>.Success([]));
            pbis.Setup(s => s.GetByProjectIdAsync(ProjectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<ProductBacklogItemDto>>.Success([]));
            Pbis = pbis.Object;
        }

        private static Result<T> Found<T>(bool exists, T value, string missing)
            => exists ? Result<T>.Success(value) : Result<T>.NotFound(missing);
    }
}
