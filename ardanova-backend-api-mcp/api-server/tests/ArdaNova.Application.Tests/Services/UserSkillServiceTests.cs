namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using AutoMapper;
using FluentAssertions;
using Moq;

public class UserSkillServiceTests
{
    private readonly Mock<IRepository<UserSkill>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly UserSkillService _sut;

    public UserSkillServiceTests()
    {
        _repositoryMock = new Mock<IRepository<UserSkill>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new UserSkillService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenSkillExists_ReturnsSuccessResult()
    {
        // Arrange
        var skillId = Guid.NewGuid().ToString();
        var skill = new UserSkill { id = skillId, userId = "user-1", skill = "C#", level = 5 };
        var skillDto = new UserSkillDto { Id = skillId, UserId = "user-1", Skill = "C#", Level = 5 };

        _repositoryMock.Setup(r => r.GetByIdAsync(skillId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(skill);
        _mapperMock.Setup(m => m.Map<UserSkillDto>(skill)).Returns(skillDto);

        // Act
        var result = await _sut.GetByIdAsync(skillId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Skill.Should().Be("C#");
        result.Value!.Level.Should().Be(5);
    }

    [Fact]
    public async Task GetByIdAsync_WhenSkillNotExists_ReturnsNotFound()
    {
        // Arrange
        var skillId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(skillId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSkill?)null);

        // Act
        var result = await _sut.GetByIdAsync(skillId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsSkillsForUser()
    {
        // Arrange
        var userId = "user-1";
        var skills = new List<UserSkill>
        {
            new UserSkill { id = Guid.NewGuid().ToString(), userId = userId, skill = "C#", level = 5 },
            new UserSkill { id = Guid.NewGuid().ToString(), userId = userId, skill = "TypeScript", level = 4 }
        };
        var skillDtos = new List<UserSkillDto>
        {
            new UserSkillDto { UserId = userId, Skill = "C#", Level = 5 },
            new UserSkillDto { UserId = userId, Skill = "TypeScript", Level = 4 }
        };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserSkill, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(skills);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<UserSkillDto>>(skills)).Returns(skillDtos);

        // Act
        var result = await _sut.GetByUserIdAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedSkill()
    {
        // Arrange
        var dto = new CreateUserSkillDto { UserId = "user-1", Skill = "Rust", Level = 3 };
        var skillDto = new UserSkillDto { UserId = "user-1", Skill = "Rust", Level = 3 };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<UserSkill>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSkill s, CancellationToken _) => s);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<UserSkillDto>(It.IsAny<UserSkill>())).Returns(skillDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Skill.Should().Be("Rust");
        result.Value!.Level.Should().Be(3);
    }

    [Fact]
    public async Task UpdateAsync_WhenSkillExists_ReturnsUpdatedSkill()
    {
        // Arrange
        var skillId = Guid.NewGuid().ToString();
        var skill = new UserSkill { id = skillId, userId = "user-1", skill = "C#", level = 5 };
        var dto = new UpdateUserSkillDto { Level = 8 };
        var updatedDto = new UserSkillDto { Id = skillId, UserId = "user-1", Skill = "C#", Level = 8 };

        _repositoryMock.Setup(r => r.GetByIdAsync(skillId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(skill);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<UserSkill>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<UserSkillDto>(It.IsAny<UserSkill>())).Returns(updatedDto);

        // Act
        var result = await _sut.UpdateAsync(skillId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Level.Should().Be(8);
    }

    [Fact]
    public async Task UpdateAsync_WhenSkillNotExists_ReturnsNotFound()
    {
        // Arrange
        var skillId = Guid.NewGuid().ToString();
        var dto = new UpdateUserSkillDto { Level = 8 };

        _repositoryMock.Setup(r => r.GetByIdAsync(skillId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSkill?)null);

        // Act
        var result = await _sut.UpdateAsync(skillId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_WhenSkillExists_ReturnsSuccess()
    {
        // Arrange
        var skillId = Guid.NewGuid().ToString();
        var skill = new UserSkill { id = skillId, userId = "user-1", skill = "C#", level = 5 };

        _repositoryMock.Setup(r => r.GetByIdAsync(skillId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(skill);
        _repositoryMock.Setup(r => r.DeleteAsync(skill, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(skillId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WhenSkillNotExists_ReturnsNotFound()
    {
        // Arrange
        var skillId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(skillId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSkill?)null);

        // Act
        var result = await _sut.DeleteAsync(skillId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }
}
