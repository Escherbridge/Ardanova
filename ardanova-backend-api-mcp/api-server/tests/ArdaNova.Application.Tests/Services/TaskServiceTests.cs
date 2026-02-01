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
using TaskStatus = ArdaNova.Domain.Models.Enums.TaskStatus;

public class TaskServiceTests
{
    private readonly Mock<IRepository<ProjectTask>> _repositoryMock;
    private readonly Mock<IRepository<User>> _userRepositoryMock;
    private readonly Mock<IRepository<Project>> _projectRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _repositoryMock = new Mock<IRepository<ProjectTask>>();
        _userRepositoryMock = new Mock<IRepository<User>>();
        _projectRepositoryMock = new Mock<IRepository<Project>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new TaskService(
            _repositoryMock.Object,
            _userRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskExists_ReturnsSuccessResult()
    {
        // Arrange
        var taskId = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid().ToString();
        var task = new ProjectTask
        {
            id = taskId,
            projectId = projectId,
            title = "Test Task",
            description = "A test task",
            status = TaskStatus.TODO,
            priority = TaskPriority.MEDIUM,
            taskType = TaskType.FEATURE,
            escrowStatus = EscrowStatus.NONE,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var project = new Project
        {
            id = projectId,
            title = "Test Project",
            slug = "test-project",
            description = "Test",
            problemStatement = "Test",
            solution = "Test",
            categories = "TECHNOLOGY",
            status = ProjectStatus.DRAFT,
            createdById = Guid.NewGuid().ToString(),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var taskDto = new TaskDto { Id = taskId, Title = "Test Task" };
        var taskProjectDto = new TaskProjectDto { Id = projectId, Title = "Test Project", Slug = "test-project" };

        _repositoryMock.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _mapperMock.Setup(m => m.Map<TaskDto>(task)).Returns(taskDto);
        _mapperMock.Setup(m => m.Map<TaskProjectDto>(project)).Returns(taskProjectDto);

        // Act
        var result = await _sut.GetByIdAsync(taskId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Test Task");
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskNotExists_ReturnsNotFound()
    {
        // Arrange
        var taskId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTask?)null);

        // Act
        var result = await _sut.GetByIdAsync(taskId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllTasks()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var tasks = new List<ProjectTask>
        {
            new ProjectTask { id = Guid.NewGuid().ToString(), projectId = projectId, title = "Task 1", status = TaskStatus.TODO, priority = TaskPriority.HIGH, taskType = TaskType.FEATURE, escrowStatus = EscrowStatus.NONE, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow },
            new ProjectTask { id = Guid.NewGuid().ToString(), projectId = projectId, title = "Task 2", status = TaskStatus.IN_PROGRESS, priority = TaskPriority.MEDIUM, taskType = TaskType.BUG, escrowStatus = EscrowStatus.NONE, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow }
        };
        var project = new Project
        {
            id = projectId,
            title = "Test Project",
            slug = "test-project",
            description = "Test",
            problemStatement = "Test",
            solution = "Test",
            categories = "TECHNOLOGY",
            status = ProjectStatus.DRAFT,
            createdById = Guid.NewGuid().ToString(),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _mapperMock.Setup(m => m.Map<TaskDto>(It.IsAny<ProjectTask>()))
            .Returns((ProjectTask t) => new TaskDto { Id = t.id, Title = t.title });
        _mapperMock.Setup(m => m.Map<TaskProjectDto>(project))
            .Returns(new TaskProjectDto { Id = projectId, Title = "Test Project", Slug = "test-project" });

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedTask()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var project = new Project
        {
            id = projectId,
            title = "Test Project",
            slug = "test-project",
            description = "Test",
            problemStatement = "Test",
            solution = "Test",
            categories = "TECHNOLOGY",
            status = ProjectStatus.DRAFT,
            createdById = Guid.NewGuid().ToString(),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var dto = new CreateTaskDto
        {
            ProjectId = projectId,
            Title = "New Task",
            Description = "A new task",
            Priority = TaskPriority.HIGH,
            TaskType = TaskType.FEATURE
        };
        var taskDto = new TaskDto { Title = "New Task" };

        _projectRepositoryMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTask t, CancellationToken _) => t);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<TaskDto>(It.IsAny<ProjectTask>())).Returns(taskDto);
        _mapperMock.Setup(m => m.Map<TaskProjectDto>(project))
            .Returns(new TaskProjectDto { Id = projectId, Title = "Test Project", Slug = "test-project" });

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("New Task");
    }

    [Fact]
    public async Task CreateAsync_WhenProjectNotExists_ReturnsNotFound()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var dto = new CreateTaskDto
        {
            ProjectId = projectId,
            Title = "New Task"
        };

        _projectRepositoryMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetByProjectIdAsync_ReturnsProjectTasks()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var tasks = new List<ProjectTask>
        {
            new ProjectTask { id = Guid.NewGuid().ToString(), projectId = projectId, title = "Project Task", status = TaskStatus.TODO, priority = TaskPriority.MEDIUM, taskType = TaskType.FEATURE, escrowStatus = EscrowStatus.NONE, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow }
        };
        var project = new Project
        {
            id = projectId,
            title = "Test Project",
            slug = "test-project",
            description = "Test",
            problemStatement = "Test",
            solution = "Test",
            categories = "TECHNOLOGY",
            status = ProjectStatus.DRAFT,
            createdById = Guid.NewGuid().ToString(),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProjectTask, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _mapperMock.Setup(m => m.Map<TaskDto>(It.IsAny<ProjectTask>()))
            .Returns((ProjectTask t) => new TaskDto { Id = t.id, Title = t.title });
        _mapperMock.Setup(m => m.Map<TaskProjectDto>(project))
            .Returns(new TaskProjectDto { Id = projectId, Title = "Test Project", Slug = "test-project" });

        // Act
        var result = await _sut.GetByProjectIdAsync(projectId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_WhenTaskExists_ReturnsSuccess()
    {
        // Arrange
        var taskId = Guid.NewGuid().ToString();
        var task = new ProjectTask
        {
            id = taskId,
            projectId = Guid.NewGuid().ToString(),
            title = "Test Task",
            status = TaskStatus.TODO,
            priority = TaskPriority.MEDIUM,
            taskType = TaskType.FEATURE,
            escrowStatus = EscrowStatus.NONE,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _repositoryMock.Setup(r => r.DeleteAsync(task, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(taskId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenTaskExists_UpdatesStatus()
    {
        // Arrange
        var taskId = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid().ToString();
        var task = new ProjectTask
        {
            id = taskId,
            projectId = projectId,
            title = "Test Task",
            status = TaskStatus.TODO,
            priority = TaskPriority.MEDIUM,
            taskType = TaskType.FEATURE,
            escrowStatus = EscrowStatus.NONE,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var project = new Project
        {
            id = projectId,
            title = "Test Project",
            slug = "test-project",
            description = "Test",
            problemStatement = "Test",
            solution = "Test",
            categories = "TECHNOLOGY",
            status = ProjectStatus.DRAFT,
            createdById = Guid.NewGuid().ToString(),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var taskDto = new TaskDto { Id = taskId, Title = "Test Task", Status = TaskStatus.IN_PROGRESS };

        _repositoryMock.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _mapperMock.Setup(m => m.Map<TaskDto>(It.IsAny<ProjectTask>())).Returns(taskDto);
        _mapperMock.Setup(m => m.Map<TaskProjectDto>(project))
            .Returns(new TaskProjectDto { Id = projectId, Title = "Test Project", Slug = "test-project" });

        // Act
        var result = await _sut.UpdateStatusAsync(taskId, TaskStatus.IN_PROGRESS);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenCompleted_SetsCompletedAt()
    {
        // Arrange
        var taskId = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid().ToString();
        var task = new ProjectTask
        {
            id = taskId,
            projectId = projectId,
            title = "Test Task",
            status = TaskStatus.IN_PROGRESS,
            priority = TaskPriority.MEDIUM,
            taskType = TaskType.FEATURE,
            escrowStatus = EscrowStatus.NONE,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var project = new Project
        {
            id = projectId,
            title = "Test Project",
            slug = "test-project",
            description = "Test",
            problemStatement = "Test",
            solution = "Test",
            categories = "TECHNOLOGY",
            status = ProjectStatus.DRAFT,
            createdById = Guid.NewGuid().ToString(),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _mapperMock.Setup(m => m.Map<TaskDto>(It.IsAny<ProjectTask>()))
            .Returns(new TaskDto { Id = taskId, Title = "Test Task", Status = TaskStatus.COMPLETED });
        _mapperMock.Setup(m => m.Map<TaskProjectDto>(project))
            .Returns(new TaskProjectDto { Id = projectId, Title = "Test Project", Slug = "test-project" });

        // Act
        var result = await _sut.UpdateStatusAsync(taskId, TaskStatus.COMPLETED);

        // Assert
        result.IsSuccess.Should().BeTrue();
        task.completedAt.Should().NotBeNull();
    }
}
