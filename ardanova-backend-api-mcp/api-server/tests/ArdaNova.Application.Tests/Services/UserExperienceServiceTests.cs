namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using AutoMapper;
using FluentAssertions;
using Moq;

public class UserExperienceServiceTests
{
    private readonly Mock<IRepository<UserExperience>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly UserExperienceService _sut;

    public UserExperienceServiceTests()
    {
        _repositoryMock = new Mock<IRepository<UserExperience>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new UserExperienceService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExperienceExists_ReturnsSuccessResult()
    {
        // Arrange
        var expId = Guid.NewGuid().ToString();
        var experience = new UserExperience
        {
            id = expId, userId = "user-1", title = "Software Engineer",
            company = "Acme Corp", startDate = new DateTime(2022, 1, 1), isCurrent = true
        };
        var experienceDto = new UserExperienceDto
        {
            Id = expId, UserId = "user-1", Title = "Software Engineer",
            Company = "Acme Corp", StartDate = new DateTime(2022, 1, 1), IsCurrent = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(expId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(experience);
        _mapperMock.Setup(m => m.Map<UserExperienceDto>(experience)).Returns(experienceDto);

        // Act
        var result = await _sut.GetByIdAsync(expId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Software Engineer");
        result.Value!.Company.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExperienceNotExists_ReturnsNotFound()
    {
        // Arrange
        var expId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(expId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserExperience?)null);

        // Act
        var result = await _sut.GetByIdAsync(expId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsExperiencesForUser()
    {
        // Arrange
        var userId = "user-1";
        var experiences = new List<UserExperience>
        {
            new UserExperience { id = Guid.NewGuid().ToString(), userId = userId, title = "Dev", company = "A", startDate = DateTime.UtcNow, isCurrent = true },
            new UserExperience { id = Guid.NewGuid().ToString(), userId = userId, title = "Lead", company = "B", startDate = DateTime.UtcNow, isCurrent = false }
        };
        var experienceDtos = new List<UserExperienceDto>
        {
            new UserExperienceDto { UserId = userId, Title = "Dev", Company = "A" },
            new UserExperienceDto { UserId = userId, Title = "Lead", Company = "B" }
        };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserExperience, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(experiences);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<UserExperienceDto>>(experiences)).Returns(experienceDtos);

        // Act
        var result = await _sut.GetByUserIdAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedExperience()
    {
        // Arrange
        var dto = new CreateUserExperienceDto
        {
            UserId = "user-1", Title = "Senior Engineer", Company = "BigCo",
            Description = "Led a team", StartDate = new DateTime(2023, 6, 1), IsCurrent = true
        };
        var experienceDto = new UserExperienceDto
        {
            UserId = "user-1", Title = "Senior Engineer", Company = "BigCo",
            Description = "Led a team", StartDate = new DateTime(2023, 6, 1), IsCurrent = true
        };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<UserExperience>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserExperience e, CancellationToken _) => e);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<UserExperienceDto>(It.IsAny<UserExperience>())).Returns(experienceDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Senior Engineer");
        result.Value!.Company.Should().Be("BigCo");
        result.Value!.IsCurrent.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_WhenExperienceExists_ReturnsUpdatedExperience()
    {
        // Arrange
        var expId = Guid.NewGuid().ToString();
        var experience = new UserExperience
        {
            id = expId, userId = "user-1", title = "Dev", company = "A",
            startDate = new DateTime(2022, 1, 1), isCurrent = true
        };
        var dto = new UpdateUserExperienceDto { Title = "Senior Dev", IsCurrent = false };
        var updatedDto = new UserExperienceDto
        {
            Id = expId, UserId = "user-1", Title = "Senior Dev", Company = "A",
            StartDate = new DateTime(2022, 1, 1), IsCurrent = false
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(expId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(experience);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserExperience>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<UserExperienceDto>(It.IsAny<UserExperience>())).Returns(updatedDto);

        // Act
        var result = await _sut.UpdateAsync(expId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Senior Dev");
        result.Value!.IsCurrent.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_WhenExperienceNotExists_ReturnsNotFound()
    {
        // Arrange
        var expId = Guid.NewGuid().ToString();
        var dto = new UpdateUserExperienceDto { Title = "Senior Dev" };

        _repositoryMock.Setup(r => r.GetByIdAsync(expId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserExperience?)null);

        // Act
        var result = await _sut.UpdateAsync(expId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_WhenExperienceExists_ReturnsSuccess()
    {
        // Arrange
        var expId = Guid.NewGuid().ToString();
        var experience = new UserExperience
        {
            id = expId, userId = "user-1", title = "Dev", company = "A",
            startDate = DateTime.UtcNow, isCurrent = false
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(expId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(experience);
        _repositoryMock.Setup(r => r.DeleteAsync(experience, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(expId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WhenExperienceNotExists_ReturnsNotFound()
    {
        // Arrange
        var expId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(expId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserExperience?)null);

        // Act
        var result = await _sut.DeleteAsync(expId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }
}
