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

public class ProjectServiceTests
{
    private readonly Mock<IRepository<Project>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly ProjectService _sut;

    public ProjectServiceTests()
    {
        _repositoryMock = new Mock<IRepository<Project>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new ProjectService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProjectExists_ReturnsSuccessResult()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var project = new Project
        {
            id = projectId,
            createdById = userId,
            title = "Test Project",
            description = "A test project description",
            problemStatement = "Problem statement",
            solution = "Solution description",
            category = ProjectCategory.TECHNOLOGY,
            status = ProjectStatus.DRAFT,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var projectDto = new ProjectDto { Id = projectId, Title = "Test Project" };

        _repositoryMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _mapperMock.Setup(m => m.Map<ProjectDto>(project)).Returns(projectDto);

        // Act
        var result = await _sut.GetByIdAsync(projectId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Test Project");
    }

    [Fact]
    public async Task GetByIdAsync_WhenProjectNotExists_ReturnsNotFound()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        // Act
        var result = await _sut.GetByIdAsync(projectId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllProjects()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var projects = new List<Project>
        {
            new Project { id = Guid.NewGuid().ToString(), createdById = userId, title = "Project 1", description = "Desc 1", problemStatement = "Problem 1", solution = "Solution 1", category = ProjectCategory.TECHNOLOGY, status = ProjectStatus.DRAFT, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow },
            new Project { id = Guid.NewGuid().ToString(), createdById = userId, title = "Project 2", description = "Desc 2", problemStatement = "Problem 2", solution = "Solution 2", category = ProjectCategory.HEALTHCARE, status = ProjectStatus.DRAFT, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow }
        };
        var projectDtos = new List<ProjectDto>
        {
            new ProjectDto { Title = "Project 1" },
            new ProjectDto { Title = "Project 2" }
        };

        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<ProjectDto>>(projects)).Returns(projectDtos);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedProject()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var dto = new CreateProjectDto
        {
            Title = "New Project",
            Description = "New project description",
            ProblemStatement = "The problem",
            Solution = "The solution",
            Category = ProjectCategory.TECHNOLOGY,
            CreatedById = userId
        };
        var projectDto = new ProjectDto { Title = "New Project" };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project p, CancellationToken _) => p);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<ProjectDto>(It.IsAny<Project>())).Returns(projectDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("New Project");
    }

    [Fact]
    public async Task GetBySlugAsync_WhenProjectExists_ReturnsProject()
    {
        // Arrange
        var slug = "test-project";
        var userId = Guid.NewGuid().ToString();
        var project = new Project { id = Guid.NewGuid().ToString(), createdById = userId, title = "Test Project", description = "Description", problemStatement = "Problem", solution = "Solution", category = ProjectCategory.TECHNOLOGY, slug = slug, status = ProjectStatus.DRAFT, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow };
        var projectDto = new ProjectDto { Title = "Test Project" };

        _repositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Project, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _mapperMock.Setup(m => m.Map<ProjectDto>(project)).Returns(projectDto);

        // Act
        var result = await _sut.GetBySlugAsync(slug);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByStatusAsync_ReturnsProjectsWithMatchingStatus()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var projects = new List<Project>
        {
            new Project { id = Guid.NewGuid().ToString(), createdById = userId, title = "Draft Project", description = "Desc", problemStatement = "Problem", solution = "Solution", category = ProjectCategory.TECHNOLOGY, status = ProjectStatus.DRAFT, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow }
        };
        var projectDtos = new List<ProjectDto> { new ProjectDto { Title = "Draft Project" } };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Project, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<ProjectDto>>(projects)).Returns(projectDtos);

        // Act
        var result = await _sut.GetByStatusAsync(ProjectStatus.DRAFT);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_WhenProjectExists_ReturnsSuccess()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var project = new Project { id = projectId, createdById = userId, title = "Test Project", description = "Desc", problemStatement = "Problem", solution = "Solution", category = ProjectCategory.TECHNOLOGY, status = ProjectStatus.DRAFT, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow };

        _repositoryMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _repositoryMock.Setup(r => r.DeleteAsync(project, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(projectId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task PublishAsync_WhenProjectExists_PublishesProject()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var project = new Project { id = projectId, createdById = userId, title = "Test Project", description = "Desc", problemStatement = "Problem", solution = "Solution", category = ProjectCategory.TECHNOLOGY, status = ProjectStatus.DRAFT, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow };
        var projectDto = new ProjectDto { Title = "Test Project", Status = ProjectStatus.PUBLISHED };

        _repositoryMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<ProjectDto>(It.IsAny<Project>())).Returns(projectDto);

        // Act
        var result = await _sut.PublishAsync(projectId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
