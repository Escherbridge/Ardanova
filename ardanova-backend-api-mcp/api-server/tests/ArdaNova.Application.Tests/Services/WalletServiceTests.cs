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

public class WalletServiceTests
{
    private readonly Mock<IRepository<Wallet>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly WalletService _sut;

    public WalletServiceTests()
    {
        _repositoryMock = new Mock<IRepository<Wallet>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new WalletService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenWalletExists_ReturnsSuccessResult()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var wallet = Wallet.Create(userId, "ALGO123456789", WalletProvider.PERA);
        var walletDto = new WalletDto { Id = walletId, UserId = userId, Address = "ALGO123456789", Provider = WalletProvider.PERA };

        _repositoryMock.Setup(r => r.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);
        _mapperMock.Setup(m => m.Map<WalletDto>(wallet)).Returns(walletDto);

        // Act
        var result = await _sut.GetByIdAsync(walletId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Address.Should().Be("ALGO123456789");
    }

    [Fact]
    public async Task GetByIdAsync_WhenWalletNotExists_ReturnsNotFound()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet?)null);

        // Act
        var result = await _sut.GetByIdAsync(walletId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsWalletsForUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var wallets = new List<Wallet>
        {
            Wallet.Create(userId, "ALGO111111111", WalletProvider.PERA),
            Wallet.Create(userId, "ALGO222222222", WalletProvider.DEFLY)
        };
        var walletDtos = new List<WalletDto>
        {
            new WalletDto { UserId = userId, Address = "ALGO111111111", Provider = WalletProvider.PERA },
            new WalletDto { UserId = userId, Address = "ALGO222222222", Provider = WalletProvider.DEFLY }
        };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wallet, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallets);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<WalletDto>>(wallets)).Returns(walletDtos);

        // Act
        var result = await _sut.GetByUserIdAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedWallet()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new CreateWalletDto
        {
            UserId = userId,
            Address = "ALGO999999999",
            Provider = WalletProvider.PERA
        };
        var walletDto = new WalletDto { UserId = userId, Address = "ALGO999999999", Provider = WalletProvider.PERA };

        _repositoryMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wallet, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet w, CancellationToken _) => w);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<WalletDto>(It.IsAny<Wallet>())).Returns(walletDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Address.Should().Be("ALGO999999999");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateAddress_ReturnsValidationError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new CreateWalletDto
        {
            UserId = userId,
            Address = "ALGO_EXISTING",
            Provider = WalletProvider.PERA
        };

        _repositoryMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Wallet, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    [Fact]
    public async Task VerifyAsync_WhenWalletExists_VerifiesWallet()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var wallet = Wallet.Create(userId, "ALGO123456789", WalletProvider.PERA);
        var walletDto = new WalletDto { Id = walletId, IsVerified = true };

        _repositoryMock.Setup(r => r.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<WalletDto>(It.IsAny<Wallet>())).Returns(walletDto);

        // Act
        var result = await _sut.VerifyAsync(walletId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsVerified.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WhenWalletExists_ReturnsSuccess()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var wallet = Wallet.Create(userId, "ALGO123456789", WalletProvider.PERA);

        _repositoryMock.Setup(r => r.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _repositoryMock.Setup(r => r.DeleteAsync(wallet, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(walletId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WhenWalletNotExists_ReturnsNotFound()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet?)null);

        // Act
        var result = await _sut.DeleteAsync(walletId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }
}
