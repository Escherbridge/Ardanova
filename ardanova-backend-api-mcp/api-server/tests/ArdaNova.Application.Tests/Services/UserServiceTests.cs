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

public class UserServiceTests
{
    private readonly Mock<IRepository<User>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _repositoryMock = new Mock<IRepository<User>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new UserService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ReturnsSuccessResult()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User { id = userId, email = "test@example.com", name = "Test User", verificationLevel = VerificationLevel.ANONYMOUS, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow };
        var userDto = new UserDto { Id = userId, Email = "test@example.com", Name = "Test User" };

        _repositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mapperMock.Setup(m => m.Map<UserDto>(user)).Returns(userDto);

        // Act
        var result = await _sut.GetByIdAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserNotExists_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.GetByIdAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User { id = Guid.NewGuid().ToString(), email = "user1@example.com", name = "User 1", verificationLevel = VerificationLevel.ANONYMOUS, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow },
            new User { id = Guid.NewGuid().ToString(), email = "user2@example.com", name = "User 2", verificationLevel = VerificationLevel.ANONYMOUS, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow }
        };
        var userDtos = new List<UserDto>
        {
            new UserDto { Email = "user1@example.com", Name = "User 1" },
            new UserDto { Email = "user2@example.com", Name = "User 2" }
        };

        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<UserDto>>(users)).Returns(userDtos);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedUser()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            Email = "new@example.com",
            Name = "New User"
        };
        var userDto = new UserDto { Email = "new@example.com", Name = "New User" };

        _repositoryMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<UserDto>(It.IsAny<User>())).Returns(userDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be("new@example.com");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ReturnsValidationError()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            Email = "existing@example.com",
            Name = "New User"
        };

        _repositoryMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserExists_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User { id = userId, email = "test@example.com", name = "Test User", verificationLevel = VerificationLevel.ANONYMOUS, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow };

        _repositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _repositoryMock.Setup(r => r.DeleteAsync(user, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WhenUserNotExists_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.DeleteAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenUserExists_ReturnsUser()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User { id = Guid.NewGuid().ToString(), email = email, name = "Test User", verificationLevel = VerificationLevel.ANONYMOUS, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow };
        var userDto = new UserDto { Email = email, Name = "Test User" };

        _repositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mapperMock.Setup(m => m.Map<UserDto>(user)).Returns(userDto);

        // Act
        var result = await _sut.GetByEmailAsync(email);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(email);
    }

    [Fact]
    public async Task VerifyAsync_WhenUserExists_VerifiesUser()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User { id = userId, email = "test@example.com", name = "Test User", verificationLevel = VerificationLevel.ANONYMOUS, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow };
        var userDto = new UserDto { Email = "test@example.com", IsVerified = true };

        _repositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<UserDto>(It.IsAny<User>())).Returns(userDto);

        // Act
        var result = await _sut.VerifyAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
