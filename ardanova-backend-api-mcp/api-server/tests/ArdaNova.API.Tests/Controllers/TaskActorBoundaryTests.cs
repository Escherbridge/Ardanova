namespace ArdaNova.API.Tests.Controllers;

using System.Security.Claims;
using System.Reflection;
using ArdaNova.API.Controllers;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
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
        var controller = WithActor(new TasksController(tasks.Object, new Mock<IProjectService>(MockBehavior.Strict).Object), "actor-1");

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
        var controller = WithActor(new TasksController(tasks.Object, projects.Object), "actor-1");

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
        var controller = WithActor(new TasksController(tasks.Object, projects.Object), "actor-1");

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
        var controller = WithActor(new TasksController(tasks.Object, projects.Object), "actor-1");

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
        var controller = WithActor(new TasksController(tasks.Object, projects.Object), "actor-1");

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
        var controller = WithActor(new TasksController(tasks.Object, new Mock<IProjectService>(MockBehavior.Strict).Object), "actor-1");

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
        var controller = WithActor(new TasksController(tasks.Object, new Mock<IProjectService>(MockBehavior.Strict).Object), "actor-1", UserRole.ADMIN);

        var result = await controller.Update("task-1", new UpdateTaskDto { Title = "Admin edit" }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        tasks.VerifyAll();
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
