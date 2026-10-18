namespace ArdaNova.API.Tests.Controllers;

using System.Security.Claims;
using System.Reflection;
using ArdaNova.API.Controllers;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class TaskActorBoundaryTests
{
    [Fact]
    public async Task GetMine_UsesActorInsteadOfCallerSuppliedIdentity()
    {
        var tasks = new Mock<ITaskService>();
        tasks.Setup(service => service.GetByUserIdAsync("actor-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<TaskDto>>.Success([]));
        var controller = WithActor(CreateController(tasks.Object), "actor-1");

        var result = await controller.GetMine(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        tasks.VerifyAll();
    }

    [Fact]
    public async Task UpdateStatus_ForeignTaskIsForbiddenBeforeMutation()
    {
        var tasks = new Mock<ITaskService>();
        tasks.Setup(service => service.GetByIdAsync("task-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDto>.Success(new TaskDto
            {
                Id = "task-1",
                ProjectId = "project-1",
                AssignedToId = "other-user"
            }));
        var projects = new Mock<IProjectService>();
        projects.Setup(service => service.GetByIdAsync("project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectDto>.Success(new ProjectDto
            {
                Id = "project-1",
                CreatedById = "other-user",
                Title = "Foreign project",
                Slug = "foreign-project",
                Description = ""
            }));
        var controller = WithActor(CreateController(tasks.Object, projects.Object), "actor-1");

        var result = await controller.UpdateStatus("task-1", new UpdateTaskStatusDto { Status = TaskStatus.IN_PROGRESS }, CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        tasks.Verify(service => service.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<TaskStatus>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_ForeignTaskIsForbiddenBeforeMutation()
    {
        var tasks = new Mock<ITaskService>();
        tasks.Setup(service => service.GetByIdAsync("task-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDto>.Success(new TaskDto { Id = "task-1", ProjectId = "project-1" }));
        var projects = new Mock<IProjectService>();
        projects.Setup(service => service.GetByIdAsync("project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectDto>.Success(new ProjectDto
            {
                Id = "project-1",
                CreatedById = "other-user",
                Title = "Foreign project",
                Slug = "foreign-project",
                Description = ""
            }));
        var controller = WithActor(CreateController(tasks.Object, projects.Object), "actor-1");

        var result = await controller.Update("task-1", new UpdateTaskDto { Title = "Unauthorized edit" }, CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        tasks.Verify(service => service.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdateTaskDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_ForAnotherActorsProjectIsForbiddenBeforeMutation()
    {
        var tasks = new Mock<ITaskService>();
        var projects = new Mock<IProjectService>();
        projects.Setup(service => service.GetByIdAsync("project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectDto>.Success(new ProjectDto
            {
                Id = "project-1",
                CreatedById = "other-user",
                Title = "Foreign project",
                Slug = "foreign-project",
                Description = ""
            }));
        var controller = WithActor(CreateController(tasks.Object, projects.Object), "actor-1");

        var result = await controller.Create(new CreateTaskDto { ProjectId = "project-1", Title = "Unauthorized task" }, CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        tasks.Verify(service => service.CreateAsync(It.IsAny<CreateTaskDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_ForeignTaskIsForbiddenBeforeMutation()
    {
        var tasks = new Mock<ITaskService>();
        tasks.Setup(service => service.GetByIdAsync("task-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDto>.Success(new TaskDto { Id = "task-1", ProjectId = "project-1" }));
        var projects = new Mock<IProjectService>();
        projects.Setup(service => service.GetByIdAsync("project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectDto>.Success(new ProjectDto
            {
                Id = "project-1",
                CreatedById = "other-user",
                Title = "Foreign project",
                Slug = "foreign-project",
                Description = ""
            }));
        var controller = WithActor(CreateController(tasks.Object, projects.Object), "actor-1");

        var result = await controller.Delete("task-1", CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        tasks.Verify(service => service.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatus_AssigneeMayUpdateOnlyTheirTask()
    {
        var tasks = new Mock<ITaskService>();
        tasks.Setup(service => service.GetByIdAsync("task-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDto>.Success(new TaskDto { Id = "task-1", ProjectId = "project-1", AssignedToId = "actor-1" }));
        tasks.Setup(service => service.UpdateStatusAsync("task-1", TaskStatus.IN_PROGRESS, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDto>.Success(new TaskDto { Id = "task-1", ProjectId = "project-1", AssignedToId = "actor-1", Status = TaskStatus.IN_PROGRESS }));
        var controller = WithActor(CreateController(tasks.Object), "actor-1");

        var result = await controller.UpdateStatus("task-1", new UpdateTaskStatusDto { Status = TaskStatus.IN_PROGRESS }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        tasks.VerifyAll();
    }

    [Fact]
    public async Task Update_AdminMayManageAnyProjectTask()
    {
        var tasks = new Mock<ITaskService>();
        tasks.Setup(service => service.GetByIdAsync("task-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDto>.Success(new TaskDto { Id = "task-1", ProjectId = "project-1" }));
        tasks.Setup(service => service.UpdateAsync("task-1", It.IsAny<UpdateTaskDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDto>.Success(new TaskDto { Id = "task-1", ProjectId = "project-1", Title = "Admin edit" }));
        var controller = WithActor(CreateController(tasks.Object), "actor-1", UserRole.ADMIN);

        var result = await controller.Update("task-1", new UpdateTaskDto { Title = "Admin edit" }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        tasks.VerifyAll();
    }

    [Fact]
    public async Task Create_CompensatedTaskRequiresAccountSetupBeforeMutation()
    {
        var tasks = new Mock<ITaskService>();
        var authorization = new Mock<IHierarchyAuthorizationService>();
        authorization
            .Setup(service => service.CanManageProjectAsync("actor-1", "project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var eligibility = new Mock<IAzoaValueEligibilityService>();
        eligibility
            .Setup(service => service.RequireReadyAsync(
                "actor-1",
                "create a paid task",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Forbidden("Complete your account setup before creating paid work."));
        var controller = WithActor(
            CreateController(tasks.Object, authorization: authorization.Object, valueEligibility: eligibility.Object),
            "actor-1");

        var result = await controller.Create(
            new CreateTaskDto { ProjectId = "project-1", Title = "Paid task", EquityReward = 10m },
            CancellationToken.None);

        var response = result.Should().BeOfType<ObjectResult>().Subject;
        response.StatusCode.Should().Be(403);
        tasks.Verify(service => service.CreateAsync(It.IsAny<CreateTaskDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_CompensatedTaskRequiresAccountSetupBeforeMutation()
    {
        var tasks = new Mock<ITaskService>();
        tasks.Setup(service => service.GetByIdAsync("task-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDto>.Success(new TaskDto
            {
                Id = "task-1",
                ProjectId = "project-1",
                AssignedToId = "contributor-1",
            }));
        var authorization = new Mock<IHierarchyAuthorizationService>();
        authorization
            .Setup(service => service.CanManageProjectAsync("actor-1", "project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var eligibility = new Mock<IAzoaValueEligibilityService>();
        eligibility
            .Setup(service => service.RequireReadyAsync(
                "actor-1",
                "update a paid task",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Forbidden("Complete your account setup before updating paid work."));
        var controller = WithActor(
            CreateController(tasks.Object, authorization: authorization.Object, valueEligibility: eligibility.Object),
            "actor-1");

        var result = await controller.Update(
            "task-1",
            new UpdateTaskDto { EquityReward = 10m },
            CancellationToken.None);

        var response = result.Should().BeOfType<ObjectResult>().Subject;
        response.StatusCode.Should().Be(403);
        tasks.Verify(service => service.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdateTaskDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void CallerControlledTaskUserRoute_IsNotExposed()
    {
        typeof(TasksController).GetMethod("GetByUserId", BindingFlags.Instance | BindingFlags.Public).Should().BeNull();
    }

    [Theory]
    [InlineData(nameof(TasksController.GetMine))]
    [InlineData(nameof(TasksController.Create))]
    [InlineData(nameof(TasksController.Update))]
    [InlineData(nameof(TasksController.UpdateStatus))]
    [InlineData(nameof(TasksController.Delete))]
    public void SelfAndMutationRoutes_RequireActorAssertion(string actionName)
    {
        var action = typeof(TasksController).GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public)!;

        action.GetCustomAttribute<AuthorizeAttribute>()?.Policy.Should().Be(AuthorizationPolicies.ActorAssertion);
    }

    [Fact]
    public async Task GetById_ATaskOnAHiddenDraftProjectReadsAsNotFound()
    {
        var tasks = new Mock<ITaskService>();
        tasks.Setup(service => service.GetByIdAsync("task-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDto>.Success(new TaskDto { Id = "task-1", ProjectId = "project-draft", Title = "secret draft plan" }));
        var visibility = new Mock<IProjectVisibility>();
        visibility.Setup(service => service.GetVisibleByIdAsync(It.IsAny<ProjectViewer>(), "project-draft", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectDto>.NotFound("Project with id project-draft not found"));
        var controller = WithActor(CreateController(tasks.Object, visibility: visibility.Object), "stranger");

        var result = await controller.GetById("task-1", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Assign_AMemberMayClaimAnUnassignedTask()
    {
        var tasks = new Mock<ITaskService>();
        tasks.Setup(service => service.GetByIdAsync("task-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDto>.Success(new TaskDto { Id = "task-1", ProjectId = "project-1" }));
        tasks.Setup(service => service.AssignAsync("task-1", "actor-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDto>.Success(new TaskDto { Id = "task-1", ProjectId = "project-1", AssignedToId = "actor-1" }));
        var controller = WithActor(
            CreateController(tasks.Object, authorization: Membership(manager: false, member: true).Object),
            "actor-1");

        var result = await controller.Assign("task-1", new AssignTaskDto { UserId = "actor-1" }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<TaskDto>()
            .Which.AssignedToId.Should().Be("actor-1");
    }

    [Theory]
    [InlineData("other-user", "actor-1")]
    [InlineData(null, "someone-else")]
    public async Task Assign_ANonManagerCannotTakeOverOrHandOutWork(string? currentAssignee, string requestedAssignee)
    {
        var tasks = new Mock<ITaskService>();
        tasks.Setup(service => service.GetByIdAsync("task-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDto>.Success(new TaskDto { Id = "task-1", ProjectId = "project-1", AssignedToId = currentAssignee }));
        var controller = WithActor(
            CreateController(tasks.Object, authorization: Membership(manager: false, member: true).Object),
            "actor-1");

        var result = await controller.Assign("task-1", new AssignTaskDto { UserId = requestedAssignee }, CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        tasks.Verify(service => service.AssignAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Assign_AManagerCannotHandWorkToSomeoneOffTheTeam()
    {
        var tasks = new Mock<ITaskService>();
        tasks.Setup(service => service.GetByIdAsync("task-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskDto>.Success(new TaskDto { Id = "task-1", ProjectId = "project-1" }));
        var controller = WithActor(
            CreateController(tasks.Object, authorization: Membership(manager: true, member: false).Object),
            "actor-1");

        var result = await controller.Assign("task-1", new AssignTaskDto { UserId = "outsider" }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static Mock<IHierarchyAuthorizationService> Membership(bool manager, bool member)
    {
        var authorization = new Mock<IHierarchyAuthorizationService>();
        authorization.Setup(service => service.CanManageProjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(manager);
        authorization.Setup(service => service.IsProjectMemberAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        return authorization;
    }

    private static TasksController CreateController(
        ITaskService tasks,
        IProjectService? projects = null,
        IHierarchyAuthorizationService? authorization = null,
        IAzoaValueEligibilityService? valueEligibility = null,
        IProjectVisibility? visibility = null)
    {
        if (projects is null)
        {
            var projectService = new Mock<IProjectService>();
            projectService
                .Setup(service => service.GetByIdAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((string projectId, CancellationToken _) =>
                    Result<ProjectDto>.Success(new ProjectDto
                    {
                        Id = projectId,
                        CreatedById = "owner",
                        Title = "Project",
                        Slug = "project",
                        Description = ""
                    }));
            projects = projectService.Object;
        }

        if (authorization is null)
        {
            var authorizationMock = new Mock<IHierarchyAuthorizationService>();
            authorizationMock
                .Setup(service => service.CanWorkOnItemAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .Returns((string actorId, string projectId, string? assigneeId, CancellationToken ct) =>
                    Task.FromResult(string.Equals(actorId, assigneeId, StringComparison.Ordinal)));
            authorization = authorizationMock.Object;
        }

        return new TasksController(
            tasks,
            projects,
            new Mock<IProjectMilestoneService>(MockBehavior.Strict).Object,
            new Mock<IEpicService>(MockBehavior.Strict).Object,
            new Mock<ISprintService>(MockBehavior.Strict).Object,
            new Mock<IFeatureService>(MockBehavior.Strict).Object,
            new Mock<IProductBacklogItemService>(MockBehavior.Strict).Object,
            authorization,
            valueEligibility ?? new Mock<IAzoaValueEligibilityService>().Object,
            visibility ?? new Mock<IProjectVisibility>(MockBehavior.Strict).Object);
    }

    private static T WithActor<T>(T controller, string actorId, UserRole? role = null)
        where T : ControllerBase
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, actorId) };
        if (role.HasValue)
            claims.Add(new Claim(ClaimTypes.Role, role.Value.ToString()));

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                claims,
                ActorAssertionMiddleware.AuthenticationType))
        };
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }
}
