namespace ArdaNova.Application.Tests.Services;

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

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _repositoryMock;
    private readonly Mock<IRepository<User>> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IKycGateService> _kycGateServiceMock;
    private readonly ProjectService _sut;

    public ProjectServiceTests()
    {
        _repositoryMock = new Mock<IProjectRepository>();
        _userRepositoryMock = new Mock<IRepository<User>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _kycGateServiceMock = new Mock<IKycGateService>();
        _kycGateServiceMock.Setup(x => x.RequireProAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
        _sut = new ProjectService(_repositoryMock.Object, _userRepositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object, _kycGateServiceMock.Object);
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
            categories = "TECHNOLOGY",
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
        var projectId1 = Guid.NewGuid().ToString();
        var projectId2 = Guid.NewGuid().ToString();
        var projects = new List<Project>
        {
            new Project { id = projectId1, createdById = userId, title = "Project 1", description = "Desc 1", problemStatement = "Problem 1", solution = "Solution 1", categories = "TECHNOLOGY", status = ProjectStatus.DRAFT, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow },
            new Project { id = projectId2, createdById = userId, title = "Project 2", description = "Desc 2", problemStatement = "Problem 2", solution = "Solution 2", categories = "HEALTHCARE", status = ProjectStatus.DRAFT, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow }
        };
        var projectDtos = new List<ProjectDto>
        {
            new ProjectDto { Id = projectId1, Title = "Project 1" },
            new ProjectDto { Id = projectId2, Title = "Project 2" }
        };

        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);
        _mapperMock.Setup(m => m.Map<List<ProjectDto>>(It.IsAny<IEnumerable<Project>>())).Returns(projectDtos);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

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
            Categories = new List<string> { "TECHNOLOGY" },
            CreatedById = userId,
            ProjectType = ProjectType.TEMPORARY
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
        var project = new Project { id = Guid.NewGuid().ToString(), createdById = userId, title = "Test Project", description = "Description", problemStatement = "Problem", solution = "Solution", categories = "TECHNOLOGY", slug = slug, status = ProjectStatus.DRAFT, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow };
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
        var projectId = Guid.NewGuid().ToString();
        var projects = new List<Project>
        {
            new Project { id = projectId, createdById = userId, title = "Draft Project", description = "Desc", problemStatement = "Problem", solution = "Solution", categories = "TECHNOLOGY", status = ProjectStatus.DRAFT, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow }
        };
        var projectDtos = new List<ProjectDto> { new ProjectDto { Id = projectId, Title = "Draft Project" } };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Project, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);
        _mapperMock.Setup(m => m.Map<List<ProjectDto>>(It.IsAny<IEnumerable<Project>>())).Returns(projectDtos);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

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
        var project = new Project { id = projectId, createdById = userId, title = "Test Project", description = "Desc", problemStatement = "Problem", solution = "Solution", categories = "TECHNOLOGY", status = ProjectStatus.DRAFT, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow };

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
        var project = new Project { id = projectId, createdById = userId, title = "Test Project", description = "Desc", problemStatement = "Problem", solution = "Solution", categories = "TECHNOLOGY", status = ProjectStatus.DRAFT, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow };
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

    [Fact]
    public async Task GetByProjectTypeAsync_ReturnsProjectsWithMatchingType()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid().ToString();
        var projects = new List<Project>
        {
            new Project { id = projectId, createdById = userId, title = "Foundation Project", description = "Desc", problemStatement = "Problem", solution = "Solution", categories = "TECHNOLOGY", projectType = ProjectType.FOUNDATION, status = ProjectStatus.DRAFT, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow }
        };
        var projectDtos = new List<ProjectDto> { new ProjectDto { Id = projectId, Title = "Foundation Project", ProjectType = ProjectType.FOUNDATION } };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Project, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);
        _mapperMock.Setup(m => m.Map<List<ProjectDto>>(It.IsAny<IEnumerable<Project>>())).Returns(projectDtos);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.GetByProjectTypeAsync(ProjectType.FOUNDATION);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        result.Value!.First().ProjectType.Should().Be(ProjectType.FOUNDATION);
    }

    [Fact]
    public async Task SearchAsync_WithProjectTypeFilter_ReturnsFilteredResults()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid().ToString();
        var projects = new List<Project>
        {
            new Project { id = projectId, createdById = userId, title = "Business Project", description = "Desc", problemStatement = "Problem", solution = "Solution", categories = "TECHNOLOGY", projectType = ProjectType.BUSINESS, status = ProjectStatus.PUBLISHED, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow }
        };
        var pagedResult = new PagedResult<Project>(projects, 1, 1, 10);
        var projectDtos = new List<ProjectDto> { new ProjectDto { Id = projectId, Title = "Business Project", ProjectType = ProjectType.BUSINESS } };

        _repositoryMock.Setup(r => r.SearchAsync(null, null, null, ProjectType.BUSINESS, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);
        _mapperMock.Setup(m => m.Map<List<ProjectDto>>(It.IsAny<IEnumerable<Project>>())).Returns(projectDtos);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.SearchAsync(null, null, null, ProjectType.BUSINESS, 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithProjectTypeAndDuration_SetsFieldsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var dto = new CreateProjectDto
        {
            Title = "Long Term Foundation",
            Description = "A foundation project",
            ProblemStatement = "The problem",
            Solution = "The solution",
            Categories = new List<string> { "TECHNOLOGY" },
            CreatedById = userId,
            ProjectType = ProjectType.FOUNDATION,
            Duration = ProjectDuration.ONGOING
        };
        Project? capturedProject = null;

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((p, _) => capturedProject = p)
            .ReturnsAsync((Project p, CancellationToken _) => p);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<ProjectDto>(It.IsAny<Project>()))
            .Returns(new ProjectDto { Title = "Long Term Foundation", ProjectType = ProjectType.FOUNDATION, Duration = ProjectDuration.ONGOING });

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedProject.Should().NotBeNull();
        capturedProject!.projectType.Should().Be(ProjectType.FOUNDATION);
        capturedProject.duration.Should().Be(ProjectDuration.ONGOING);
    }

    [Fact]
    public async Task CreateAsync_WithMultipleCategories_StoresCommaSeparated()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var dto = new CreateProjectDto
        {
            Title = "Multi Category Project",
            Description = "A project with multiple categories",
            ProblemStatement = "The problem",
            Solution = "The solution",
            Categories = new List<string> { "TECHNOLOGY", "HEALTHCARE", "EDUCATION" },
            CreatedById = userId,
            ProjectType = ProjectType.TEMPORARY
        };
        Project? capturedProject = null;

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((p, _) => capturedProject = p)
            .ReturnsAsync((Project p, CancellationToken _) => p);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<ProjectDto>(It.IsAny<Project>()))
            .Returns(new ProjectDto { Title = "Multi Category Project" });

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedProject.Should().NotBeNull();
        capturedProject!.categories.Should().Be("TECHNOLOGY,HEALTHCARE,EDUCATION");
    }

    [Fact]
    public async Task CreateAsync_WithEmptyCategory_ReturnsValidationError()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var dto = new CreateProjectDto
        {
            Title = "Invalid Project",
            Description = "A project with empty category",
            ProblemStatement = "The problem",
            Solution = "The solution",
            Categories = new List<string> { "TECHNOLOGY", "", "EDUCATION" },
            CreatedById = userId,
            ProjectType = ProjectType.TEMPORARY
        };

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    [Fact]
    public async Task CreateAsync_WithCustomCategoryOver50Chars_ReturnsValidationError()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var longCustomCategory = new string('A', 51);
        var dto = new CreateProjectDto
        {
            Title = "Invalid Project",
            Description = "A project with overly long custom category",
            ProblemStatement = "The problem",
            Solution = "The solution",
            Categories = new List<string> { "TECHNOLOGY", longCustomCategory },
            CreatedById = userId,
            ProjectType = ProjectType.TEMPORARY
        };

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    [Fact]
    public async Task CreateAsync_WithValidCustomCategory_Succeeds()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var dto = new CreateProjectDto
        {
            Title = "Custom Category Project",
            Description = "A project with a valid custom category",
            ProblemStatement = "The problem",
            Solution = "The solution",
            Categories = new List<string> { "TECHNOLOGY", "MyCustomCategory" },
            CreatedById = userId,
            ProjectType = ProjectType.TEMPORARY
        };
        Project? capturedProject = null;

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((p, _) => capturedProject = p)
            .ReturnsAsync((Project p, CancellationToken _) => p);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<ProjectDto>(It.IsAny<Project>()))
            .Returns(new ProjectDto { Title = "Custom Category Project" });

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedProject.Should().NotBeNull();
        capturedProject!.categories.Should().Be("TECHNOLOGY,MyCustomCategory");
    }

    [Fact]
    public async Task UpdateAsync_WithCategories_UpdatesCategoriesField()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var project = new Project
        {
            id = projectId,
            createdById = userId,
            title = "Test Project",
            description = "Desc",
            problemStatement = "Problem",
            solution = "Solution",
            categories = "TECHNOLOGY",
            status = ProjectStatus.DRAFT,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var dto = new UpdateProjectDto
        {
            Categories = new List<string> { "HEALTHCARE", "EDUCATION" }
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<ProjectDto>(It.IsAny<Project>()))
            .Returns(new ProjectDto { Title = "Test Project" });

        // Act
        var result = await _sut.UpdateAsync(projectId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        project.categories.Should().Be("HEALTHCARE,EDUCATION");
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidCategories_ReturnsValidationError()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var dto = new UpdateProjectDto
        {
            Categories = new List<string> { "TECHNOLOGY", "" }
        };

        // Act
        var result = await _sut.UpdateAsync(projectId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    [Fact]
    public async Task GetByCategory_WithStringCategory_ReturnsMatchingProjects()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid().ToString();
        var projects = new List<Project>
        {
            new Project { id = projectId, createdById = userId, title = "Tech Project", description = "Desc", problemStatement = "Problem", solution = "Solution", categories = "TECHNOLOGY,HEALTHCARE", status = ProjectStatus.DRAFT, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow }
        };
        var projectDtos = new List<ProjectDto> { new ProjectDto { Id = projectId, Title = "Tech Project" } };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Project, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);
        _mapperMock.Setup(m => m.Map<List<ProjectDto>>(It.IsAny<IEnumerable<Project>>())).Returns(projectDtos);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.GetByCategory("TECHNOLOGY");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WhenKycGateBlocksNonPro_ReturnsForbidden()
    {
        // Arrange
        var dto = new CreateProjectDto
        {
            CreatedById = Guid.NewGuid().ToString(),
            Title = "Test",
            Description = "Desc",
            ProblemStatement = "Problem",
            Solution = "Solution",
            Categories = new List<string> { "TECHNOLOGY" }
        };
        _kycGateServiceMock.Setup(x => x.RequireProAsync(dto.CreatedById, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Forbidden("KYC verification required"));

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.Forbidden);
        result.Error.Should().Contain("KYC verification required");
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchAsync_WithCategoryFilter_ReturnsFilteredResults()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid().ToString();
        var projects = new List<Project>
        {
            new Project { id = projectId, createdById = userId, title = "Health Project", description = "Desc", problemStatement = "Problem", solution = "Solution", categories = "HEALTHCARE,EDUCATION", status = ProjectStatus.PUBLISHED, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow }
        };
        var pagedResult = new PagedResult<Project>(projects, 1, 1, 10);
        var projectDtos = new List<ProjectDto> { new ProjectDto { Id = projectId, Title = "Health Project" } };

        _repositoryMock.Setup(r => r.SearchAsync(null, null, "HEALTHCARE", null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);
        _mapperMock.Setup(m => m.Map<List<ProjectDto>>(It.IsAny<IEnumerable<Project>>())).Returns(projectDtos);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.SearchAsync(null, null, "HEALTHCARE", null, 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().NotBeEmpty();
    }
}
