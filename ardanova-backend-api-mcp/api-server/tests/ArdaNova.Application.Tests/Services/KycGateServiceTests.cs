namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using FluentAssertions;
using Moq;

public class KycGateServiceTests
{
    private readonly Mock<IRepository<User>> _userRepositoryMock;
    private readonly KycGateService _sut;

    public KycGateServiceTests()
    {
        _userRepositoryMock = new Mock<IRepository<User>>();
        _sut = new KycGateService(_userRepositoryMock.Object);
    }

    #region RequireProAsync

    [Fact]
    public async Task RequireProAsync_WhenUserIsPro_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User { id = userId, verificationLevel = VerificationLevel.PRO };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.RequireProAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task RequireProAsync_WhenUserIsExpert_ReturnsSuccess()
    {
        // Arrange — EXPERT is higher than PRO, should also pass
        var userId = Guid.NewGuid().ToString();
        var user = new User { id = userId, verificationLevel = VerificationLevel.EXPERT };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.RequireProAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task RequireProAsync_WhenUserIsVerified_ReturnsForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User { id = userId, verificationLevel = VerificationLevel.VERIFIED };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.RequireProAsync(userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.Forbidden);
        result.Error.Should().Contain("KYC verification required");
        result.Error.Should().Contain("/settings/verification");
    }

    [Fact]
    public async Task RequireProAsync_WhenUserIsAnonymous_ReturnsForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User { id = userId, verificationLevel = VerificationLevel.ANONYMOUS };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.RequireProAsync(userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.Forbidden);
        result.Error.Should().Contain("KYC verification required");
    }

    [Fact]
    public async Task RequireProAsync_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.RequireProAsync(userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.NotFound);
    }

    #endregion

    #region RequireVerifiedAsync

    [Fact]
    public async Task RequireVerifiedAsync_WhenUserIsVerified_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User { id = userId, verificationLevel = VerificationLevel.VERIFIED };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.RequireVerifiedAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RequireVerifiedAsync_WhenUserIsPro_ReturnsSuccess()
    {
        // Arrange — PRO is higher than VERIFIED
        var userId = Guid.NewGuid().ToString();
        var user = new User { id = userId, verificationLevel = VerificationLevel.PRO };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.RequireVerifiedAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RequireVerifiedAsync_WhenUserIsExpert_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User { id = userId, verificationLevel = VerificationLevel.EXPERT };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.RequireVerifiedAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RequireVerifiedAsync_WhenUserIsAnonymous_ReturnsForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User { id = userId, verificationLevel = VerificationLevel.ANONYMOUS };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.RequireVerifiedAsync(userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.Forbidden);
        result.Error.Should().Contain("Email verification required");
    }

    [Fact]
    public async Task RequireVerifiedAsync_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.RequireVerifiedAsync(userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.NotFound);
    }

    #endregion

    #region GetVerificationLevelAsync

    [Theory]
    [InlineData(VerificationLevel.ANONYMOUS)]
    [InlineData(VerificationLevel.VERIFIED)]
    [InlineData(VerificationLevel.PRO)]
    [InlineData(VerificationLevel.EXPERT)]
    public async Task GetVerificationLevelAsync_ReturnsCurrentLevel(VerificationLevel level)
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User { id = userId, verificationLevel = level };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.GetVerificationLevelAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(level);
    }

    [Fact]
    public async Task GetVerificationLevelAsync_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.GetVerificationLevelAsync(userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.NotFound);
    }

    #endregion
}
