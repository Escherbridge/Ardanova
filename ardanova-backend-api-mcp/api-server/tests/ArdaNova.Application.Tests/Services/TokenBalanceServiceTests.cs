namespace ArdaNova.Application.Tests.Services;

using System.Linq.Expressions;
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

public class TokenBalanceServiceTests
{
    private readonly Mock<IRepository<TokenBalance>> _tokenBalanceRepo;
    private readonly Mock<IRepository<ProjectTokenConfig>> _projectTokenConfigRepo;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IMapper> _mapper;
    private readonly TokenBalanceService _sut;

    public TokenBalanceServiceTests()
    {
        _tokenBalanceRepo = new Mock<IRepository<TokenBalance>>();
        _projectTokenConfigRepo = new Mock<IRepository<ProjectTokenConfig>>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _mapper = new Mock<IMapper>();
        _sut = new TokenBalanceService(
            _tokenBalanceRepo.Object,
            _projectTokenConfigRepo.Object,
            _unitOfWork.Object,
            _mapper.Object
        );
    }

    [Fact]
    public async Task GetBalanceAsync_Found_ReturnsSuccess()
    {
        // Arrange
        var userId = "user1";
        var projectTokenConfigId = "ptc1";
        var balance = new TokenBalance
        {
            id = "bal1",
            userId = userId,
            projectTokenConfigId = projectTokenConfigId,
            isPlatformToken = false,
            holderClass = TokenHolderClass.CONTRIBUTOR,
            isLiquid = true,
            balance = 100,
            lockedBalance = 10,
            updatedAt = DateTime.UtcNow
        };
        var balanceDto = new TokenBalanceDto
        {
            Id = balance.id,
            UserId = balance.userId,
            Balance = balance.balance,
            LockedBalance = balance.lockedBalance
        };

        _tokenBalanceRepo.Setup(r => r.FindOneAsync(
            It.IsAny<Expression<Func<TokenBalance, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(balance);

        _mapper.Setup(m => m.Map<TokenBalanceDto>(balance))
            .Returns(balanceDto);

        // Act
        var result = await _sut.GetBalanceAsync(userId, projectTokenConfigId, TokenHolderClass.CONTRIBUTOR, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(balanceDto);
        _tokenBalanceRepo.Verify(r => r.FindOneAsync(
            It.IsAny<Expression<Func<TokenBalance, bool>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBalanceAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        var userId = "user1";
        var projectTokenConfigId = "ptc1";

        _tokenBalanceRepo.Setup(r => r.FindOneAsync(
            It.IsAny<Expression<Func<TokenBalance, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((TokenBalance?)null);

        // Act
        var result = await _sut.GetBalanceAsync(userId, projectTokenConfigId, TokenHolderClass.CONTRIBUTOR, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task GetArdaBalanceAsync_Found_ReturnsSuccess()
    {
        // Arrange
        var userId = "user1";
        var balance = new TokenBalance
        {
            id = "bal1",
            userId = userId,
            projectTokenConfigId = null,
            isPlatformToken = true,
            holderClass = null,
            isLiquid = true,
            balance = 500,
            lockedBalance = 50,
            updatedAt = DateTime.UtcNow
        };
        var balanceDto = new TokenBalanceDto
        {
            Id = balance.id,
            UserId = balance.userId,
            Balance = balance.balance,
            LockedBalance = balance.lockedBalance
        };

        _tokenBalanceRepo.Setup(r => r.FindOneAsync(
            It.IsAny<Expression<Func<TokenBalance, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(balance);

        _mapper.Setup(m => m.Map<TokenBalanceDto>(balance))
            .Returns(balanceDto);

        // Act
        var result = await _sut.GetArdaBalanceAsync(userId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(balanceDto);
    }

    [Fact]
    public async Task GetArdaBalanceAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        var userId = "user1";

        _tokenBalanceRepo.Setup(r => r.FindOneAsync(
            It.IsAny<Expression<Func<TokenBalance, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((TokenBalance?)null);

        // Act
        var result = await _sut.GetArdaBalanceAsync(userId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task GetPortfolioAsync_ReturnsPortfolio()
    {
        // Arrange
        var userId = "user1";
        var balances = new List<TokenBalance>
        {
            new TokenBalance
            {
                id = "bal1",
                userId = userId,
                projectTokenConfigId = "ptc1",
                isPlatformToken = false,
                holderClass = TokenHolderClass.CONTRIBUTOR,
                isLiquid = true,
                balance = 100,
                lockedBalance = 10,
                updatedAt = DateTime.UtcNow
            },
            new TokenBalance
            {
                id = "bal2",
                userId = userId,
                projectTokenConfigId = null,
                isPlatformToken = true,
                holderClass = null,
                isLiquid = true,
                balance = 500,
                lockedBalance = 50,
                updatedAt = DateTime.UtcNow
            }
        };
        var holdingDtos = new List<TokenBalanceDto>
        {
            new TokenBalanceDto { Id = "bal1", Balance = 100 },
            new TokenBalanceDto { Id = "bal2", Balance = 500 }
        };

        _tokenBalanceRepo.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<TokenBalance, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(balances);

        _mapper.Setup(m => m.Map<List<TokenBalanceDto>>(It.IsAny<IEnumerable<TokenBalance>>()))
            .Returns(holdingDtos);

        // Act
        var result = await _sut.GetPortfolioAsync(userId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.UserId.Should().Be(userId);
        result.Value.Holdings.Should().NotBeNull();
        result.Value.Holdings.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreditAsync_NewBalance_CreatesAndCredits()
    {
        // Arrange
        var userId = "user1";
        var projectTokenConfigId = "ptc1";
        var amount = 100;
        var holderClass = TokenHolderClass.CONTRIBUTOR;
        var isPlatformToken = false;

        var projectTokenConfig = new ProjectTokenConfig
        {
            id = projectTokenConfigId,
            gateStatus = ProjectGateStatus.ACTIVE
        };

        _tokenBalanceRepo.Setup(r => r.FindOneAsync(
            It.IsAny<Expression<Func<TokenBalance, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((TokenBalance?)null);

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(projectTokenConfigId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectTokenConfig);

        _tokenBalanceRepo.Setup(r => r.AddAsync(It.IsAny<TokenBalance>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TokenBalance tb, CancellationToken ct) => tb);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _tokenBalanceRepo.Setup(r => r.UpdateAsync(It.IsAny<TokenBalance>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mapper.Setup(m => m.Map<TokenBalanceDto>(It.IsAny<TokenBalance>()))
            .Returns((TokenBalance tb) => new TokenBalanceDto { Balance = tb.balance });

        // Act
        var result = await _sut.CreditAsync(userId, projectTokenConfigId, amount, holderClass, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Balance.Should().Be(amount);
        _tokenBalanceRepo.Verify(r => r.AddAsync(
            It.Is<TokenBalance>(tb =>
                tb.userId == userId &&
                tb.projectTokenConfigId == projectTokenConfigId &&
                tb.balance == amount &&
                tb.holderClass == holderClass &&
                tb.isPlatformToken == isPlatformToken &&
                tb.isLiquid == true),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditAsync_ExistingBalance_Increments()
    {
        // Arrange
        var userId = "user1";
        var projectTokenConfigId = "ptc1";
        var amount = 100;
        var holderClass = TokenHolderClass.CONTRIBUTOR;
        var isPlatformToken = false;

        var existingBalance = new TokenBalance
        {
            id = "bal1",
            userId = userId,
            projectTokenConfigId = projectTokenConfigId,
            isPlatformToken = isPlatformToken,
            holderClass = holderClass,
            isLiquid = true,
            balance = 50,
            lockedBalance = 0,
            updatedAt = DateTime.UtcNow
        };

        var projectTokenConfig = new ProjectTokenConfig
        {
            id = projectTokenConfigId,
            gateStatus = ProjectGateStatus.ACTIVE
        };

        _tokenBalanceRepo.Setup(r => r.FindOneAsync(
            It.IsAny<Expression<Func<TokenBalance, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBalance);

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(projectTokenConfigId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectTokenConfig);

        _tokenBalanceRepo.Setup(r => r.UpdateAsync(It.IsAny<TokenBalance>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapper.Setup(m => m.Map<TokenBalanceDto>(It.IsAny<TokenBalance>()))
            .Returns((TokenBalance tb) => new TokenBalanceDto { Balance = tb.balance });

        // Act
        var result = await _sut.CreditAsync(userId, projectTokenConfigId, amount, holderClass, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Balance.Should().Be(150);
        existingBalance.balance.Should().Be(150);
        _tokenBalanceRepo.Verify(r => r.UpdateAsync(existingBalance, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditAsync_NegativeAmount_ReturnsFailure()
    {
        // Arrange
        var userId = "user1";
        var projectTokenConfigId = "ptc1";
        var amount = -100;
        var holderClass = TokenHolderClass.CONTRIBUTOR;
        var isPlatformToken = false;

        // Act
        var result = await _sut.CreditAsync(userId, projectTokenConfigId, amount, holderClass, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("must be positive");
    }

    [Fact]
    public async Task DebitAsync_SufficientBalance_Debits()
    {
        // Arrange
        var userId = "user1";
        var projectTokenConfigId = "ptc1";
        var amount = 30;

        var existingBalance = new TokenBalance
        {
            id = "bal1",
            userId = userId,
            projectTokenConfigId = projectTokenConfigId,
            isPlatformToken = false,
            holderClass = TokenHolderClass.CONTRIBUTOR,
            isLiquid = true,
            balance = 100,
            lockedBalance = 10,
            updatedAt = DateTime.UtcNow
        };

        _tokenBalanceRepo.Setup(r => r.FindOneAsync(
            It.IsAny<Expression<Func<TokenBalance, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBalance);

        _tokenBalanceRepo.Setup(r => r.UpdateAsync(It.IsAny<TokenBalance>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapper.Setup(m => m.Map<TokenBalanceDto>(It.IsAny<TokenBalance>()))
            .Returns((TokenBalance tb) => new TokenBalanceDto { Balance = tb.balance });

        // Act
        var result = await _sut.DebitAsync(userId, projectTokenConfigId, amount, TokenHolderClass.CONTRIBUTOR, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Balance.Should().Be(70);
        existingBalance.balance.Should().Be(70);
        _tokenBalanceRepo.Verify(r => r.UpdateAsync(existingBalance, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DebitAsync_InsufficientBalance_ReturnsFailure()
    {
        // Arrange
        var userId = "user1";
        var projectTokenConfigId = "ptc1";
        var amount = 150;

        var existingBalance = new TokenBalance
        {
            id = "bal1",
            userId = userId,
            projectTokenConfigId = projectTokenConfigId,
            isPlatformToken = false,
            holderClass = TokenHolderClass.CONTRIBUTOR,
            isLiquid = true,
            balance = 100,
            lockedBalance = 10,
            updatedAt = DateTime.UtcNow
        };

        _tokenBalanceRepo.Setup(r => r.FindOneAsync(
            It.IsAny<Expression<Func<TokenBalance, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBalance);

        // Act
        var result = await _sut.DebitAsync(userId, projectTokenConfigId, amount, TokenHolderClass.CONTRIBUTOR, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Insufficient");
    }

    [Fact]
    public async Task LockAsync_SufficientUnlocked_Locks()
    {
        // Arrange
        var userId = "user1";
        var projectTokenConfigId = "ptc1";
        var amount = 30;

        var existingBalance = new TokenBalance
        {
            id = "bal1",
            userId = userId,
            projectTokenConfigId = projectTokenConfigId,
            isPlatformToken = false,
            holderClass = TokenHolderClass.CONTRIBUTOR,
            isLiquid = true,
            balance = 100,
            lockedBalance = 10,
            updatedAt = DateTime.UtcNow
        };

        _tokenBalanceRepo.Setup(r => r.FindOneAsync(
            It.IsAny<Expression<Func<TokenBalance, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBalance);

        _tokenBalanceRepo.Setup(r => r.UpdateAsync(It.IsAny<TokenBalance>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapper.Setup(m => m.Map<TokenBalanceDto>(It.IsAny<TokenBalance>()))
            .Returns((TokenBalance tb) => new TokenBalanceDto { Balance = tb.balance, LockedBalance = tb.lockedBalance });

        // Act
        var result = await _sut.LockAsync(userId, projectTokenConfigId, amount, TokenHolderClass.CONTRIBUTOR, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.LockedBalance.Should().Be(40);
        existingBalance.lockedBalance.Should().Be(40);
        _tokenBalanceRepo.Verify(r => r.UpdateAsync(existingBalance, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LockAsync_InsufficientUnlocked_ReturnsFailure()
    {
        // Arrange
        var userId = "user1";
        var projectTokenConfigId = "ptc1";
        var amount = 100;

        var existingBalance = new TokenBalance
        {
            id = "bal1",
            userId = userId,
            projectTokenConfigId = projectTokenConfigId,
            isPlatformToken = false,
            holderClass = TokenHolderClass.CONTRIBUTOR,
            isLiquid = true,
            balance = 100,
            lockedBalance = 10,
            updatedAt = DateTime.UtcNow
        };

        _tokenBalanceRepo.Setup(r => r.FindOneAsync(
            It.IsAny<Expression<Func<TokenBalance, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBalance);

        // Act
        var result = await _sut.LockAsync(userId, projectTokenConfigId, amount, TokenHolderClass.CONTRIBUTOR, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Insufficient unlocked");
    }

    [Fact]
    public async Task UnlockAsync_SufficientLocked_Unlocks()
    {
        // Arrange
        var userId = "user1";
        var projectTokenConfigId = "ptc1";
        var amount = 5;

        var existingBalance = new TokenBalance
        {
            id = "bal1",
            userId = userId,
            projectTokenConfigId = projectTokenConfigId,
            isPlatformToken = false,
            holderClass = TokenHolderClass.CONTRIBUTOR,
            isLiquid = true,
            balance = 100,
            lockedBalance = 10,
            updatedAt = DateTime.UtcNow
        };

        _tokenBalanceRepo.Setup(r => r.FindOneAsync(
            It.IsAny<Expression<Func<TokenBalance, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBalance);

        _tokenBalanceRepo.Setup(r => r.UpdateAsync(It.IsAny<TokenBalance>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapper.Setup(m => m.Map<TokenBalanceDto>(It.IsAny<TokenBalance>()))
            .Returns((TokenBalance tb) => new TokenBalanceDto { Balance = tb.balance, LockedBalance = tb.lockedBalance });

        // Act
        var result = await _sut.UnlockAsync(userId, projectTokenConfigId, amount, TokenHolderClass.CONTRIBUTOR, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.LockedBalance.Should().Be(5);
        existingBalance.lockedBalance.Should().Be(5);
        _tokenBalanceRepo.Verify(r => r.UpdateAsync(existingBalance, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnlockAsync_InsufficientLocked_ReturnsFailure()
    {
        // Arrange
        var userId = "user1";
        var projectTokenConfigId = "ptc1";
        var amount = 20;

        var existingBalance = new TokenBalance
        {
            id = "bal1",
            userId = userId,
            projectTokenConfigId = projectTokenConfigId,
            isPlatformToken = false,
            holderClass = TokenHolderClass.CONTRIBUTOR,
            isLiquid = true,
            balance = 100,
            lockedBalance = 10,
            updatedAt = DateTime.UtcNow
        };

        _tokenBalanceRepo.Setup(r => r.FindOneAsync(
            It.IsAny<Expression<Func<TokenBalance, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBalance);

        // Act
        var result = await _sut.UnlockAsync(userId, projectTokenConfigId, amount, TokenHolderClass.CONTRIBUTOR, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Insufficient locked");
    }

    [Fact]
    public async Task IsBalanceLiquidAsync_ContributorActive_ReturnsTrue()
    {
        // Arrange
        var userId = "user1";
        var projectTokenConfigId = "ptc1";

        var balance = new TokenBalance
        {
            id = "bal1",
            userId = userId,
            projectTokenConfigId = projectTokenConfigId,
            isPlatformToken = false,
            holderClass = TokenHolderClass.CONTRIBUTOR,
            isLiquid = false,
            balance = 100,
            lockedBalance = 10,
            updatedAt = DateTime.UtcNow
        };

        var projectTokenConfig = new ProjectTokenConfig
        {
            id = projectTokenConfigId,
            gateStatus = ProjectGateStatus.ACTIVE
        };

        _tokenBalanceRepo.Setup(r => r.FindOneAsync(
            It.IsAny<Expression<Func<TokenBalance, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(balance);

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(projectTokenConfigId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectTokenConfig);

        // Act
        var result = await _sut.IsBalanceLiquidAsync(userId, projectTokenConfigId, TokenHolderClass.CONTRIBUTOR, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task IsBalanceLiquidAsync_ContributorFunding_ReturnsFalse()
    {
        // Arrange
        var userId = "user1";
        var projectTokenConfigId = "ptc1";

        var balance = new TokenBalance
        {
            id = "bal1",
            userId = userId,
            projectTokenConfigId = projectTokenConfigId,
            isPlatformToken = false,
            holderClass = TokenHolderClass.CONTRIBUTOR,
            isLiquid = false,
            balance = 100,
            lockedBalance = 10,
            updatedAt = DateTime.UtcNow
        };

        var projectTokenConfig = new ProjectTokenConfig
        {
            id = projectTokenConfigId,
            gateStatus = ProjectGateStatus.FUNDING
        };

        _tokenBalanceRepo.Setup(r => r.FindOneAsync(
            It.IsAny<Expression<Func<TokenBalance, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(balance);

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(projectTokenConfigId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectTokenConfig);

        // Act
        var result = await _sut.IsBalanceLiquidAsync(userId, projectTokenConfigId, TokenHolderClass.CONTRIBUTOR, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task IsBalanceLiquidAsync_InvestorSucceeded_ReturnsTrue()
    {
        // Arrange
        var userId = "user1";
        var projectTokenConfigId = "ptc1";

        var balance = new TokenBalance
        {
            id = "bal1",
            userId = userId,
            projectTokenConfigId = projectTokenConfigId,
            isPlatformToken = false,
            holderClass = TokenHolderClass.INVESTOR,
            isLiquid = false,
            balance = 100,
            lockedBalance = 10,
            updatedAt = DateTime.UtcNow
        };

        var projectTokenConfig = new ProjectTokenConfig
        {
            id = projectTokenConfigId,
            gateStatus = ProjectGateStatus.SUCCEEDED
        };

        _tokenBalanceRepo.Setup(r => r.FindOneAsync(
            It.IsAny<Expression<Func<TokenBalance, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(balance);

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(projectTokenConfigId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectTokenConfig);

        // Act
        var result = await _sut.IsBalanceLiquidAsync(userId, projectTokenConfigId, TokenHolderClass.CONTRIBUTOR, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task IsBalanceLiquidAsync_PlatformToken_ReturnsTrue()
    {
        // Arrange
        var userId = "user1";

        var balance = new TokenBalance
        {
            id = "bal1",
            userId = userId,
            projectTokenConfigId = null,
            isPlatformToken = true,
            holderClass = null,
            isLiquid = true,
            balance = 100,
            lockedBalance = 10,
            updatedAt = DateTime.UtcNow
        };

        _tokenBalanceRepo.Setup(r => r.FindOneAsync(
            It.IsAny<Expression<Func<TokenBalance, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(balance);

        // Act
        var result = await _sut.IsBalanceLiquidAsync(userId, null, TokenHolderClass.CONTRIBUTOR, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }
}
