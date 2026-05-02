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

public class PayoutServiceTests
{
    private readonly Mock<IRepository<PayoutRequest>> _payoutRepo;
    private readonly Mock<IRepository<ProjectTokenConfig>> _projectTokenConfigRepo;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IMapper> _mapper;
    private readonly Mock<ITokenBalanceService> _tokenBalanceService;
    private readonly Mock<IExchangeService> _exchangeService;
    private readonly Mock<ITreasuryService> _treasuryService;
    private readonly PayoutService _sut;

    public PayoutServiceTests()
    {
        _payoutRepo = new Mock<IRepository<PayoutRequest>>();
        _projectTokenConfigRepo = new Mock<IRepository<ProjectTokenConfig>>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _mapper = new Mock<IMapper>();
        _tokenBalanceService = new Mock<ITokenBalanceService>();
        _exchangeService = new Mock<IExchangeService>();
        _treasuryService = new Mock<ITreasuryService>();

        _sut = new PayoutService(
            _payoutRepo.Object,
            _projectTokenConfigRepo.Object,
            _unitOfWork.Object,
            _mapper.Object,
            _tokenBalanceService.Object,
            _exchangeService.Object,
            _treasuryService.Object
        );
    }

    [Fact]
    public async Task RequestPayoutAsync_HappyPath_CreatesPayoutRequest()
    {
        // Arrange
        var userId = "user-1";
        var configId = "config-1";
        var sourceAmount = 1000;
        var holderClass = TokenHolderClass.CONTRIBUTOR;

        var config = new ProjectTokenConfig
        {
            id = configId,
            projectId = "proj-1",
            gateStatus = ProjectGateStatus.ACTIVE,
            totalSupply = 10000,
            fundingGoal = 50000,
            fundingRaised = 60000
        };

        var balanceDto = new TokenBalanceDto
        {
            Id = "bal-1",
            UserId = userId,
            ProjectTokenConfigId = configId,
            HolderClass = holderClass,
            IsLiquid = true,
            Balance = 1000,
            LockedBalance = 1000
        };

        var conversionPreview = new ConversionPreviewDto
        {
            ProjectTokenValueUsd = 0.1,
            ArdaValueUsd = 0.2,
            SourceTokenAmount = sourceAmount,
            ArdaAmount = 500,
            UsdValue = 100.0
        };

        var createdPayout = new PayoutRequest
        {
            id = "payout-1",
            userId = userId,
            sourceProjectTokenConfigId = configId,
            sourceTokenAmount = sourceAmount,
            ardaTokenAmount = 500,
            usdAmount = 100.0,
            status = PayoutStatus.PENDING,
            holderClass = holderClass,
            gateStatusAtRequest = ProjectGateStatus.ACTIVE,
            requestedAt = DateTime.UtcNow
        };

        var payoutDto = new PayoutRequestDto
        {
            Id = "payout-1",
            UserId = userId,
            SourceProjectTokenConfigId = configId,
            SourceTokenAmount = sourceAmount,
            Status = PayoutStatus.PENDING
        };

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _tokenBalanceService.Setup(s => s.IsBalanceLiquidAsync(userId, configId, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        _tokenBalanceService.Setup(s => s.LockAsync(userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(balanceDto));

        _exchangeService.Setup(s => s.CalculateConversionAsync(configId, sourceAmount, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversionPreviewDto>.Success(conversionPreview));

        _payoutRepo.Setup(r => r.AddAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdPayout);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapper.Setup(m => m.Map<PayoutRequestDto>(It.IsAny<PayoutRequest>()))
            .Returns(payoutDto);

        // Act
        var dto = new CreatePayoutRequestDto
        {
            SourceProjectTokenConfigId = configId,
            SourceTokenAmount = sourceAmount,
            HolderClass = holderClass
        };
        var result = await _sut.RequestPayoutAsync(userId, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be(PayoutStatus.PENDING);
        result.Value.SourceTokenAmount.Should().Be(sourceAmount);

        _payoutRepo.Verify(r => r.AddAsync(It.Is<PayoutRequest>(p =>
            p.userId == userId &&
            p.sourceProjectTokenConfigId == configId &&
            p.sourceTokenAmount == sourceAmount &&
            p.ardaTokenAmount == 500 &&
            p.usdAmount == 100.0 &&
            p.status == PayoutStatus.PENDING &&
            p.holderClass == holderClass &&
            p.gateStatusAtRequest == ProjectGateStatus.ACTIVE
        ), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestPayoutAsync_ConfigNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = "user-1";
        var configId = "nonexistent";
        var sourceAmount = 1000;
        var holderClass = TokenHolderClass.CONTRIBUTOR;

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTokenConfig?)null);

        // Act
        var dto = new CreatePayoutRequestDto
        {
            SourceProjectTokenConfigId = configId,
            SourceTokenAmount = sourceAmount,
            HolderClass = holderClass
        };
        var result = await _sut.RequestPayoutAsync(userId, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");

        _tokenBalanceService.Verify(s => s.LockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestPayoutAsync_TokensNotLiquid_ContributorFunding_ReturnsGateMessage()
    {
        // Arrange
        var userId = "user-1";
        var configId = "config-1";
        var sourceAmount = 1000;
        var holderClass = TokenHolderClass.CONTRIBUTOR;

        var config = new ProjectTokenConfig
        {
            id = configId,
            projectId = "proj-1",
            gateStatus = ProjectGateStatus.FUNDING,
            totalSupply = 10000,
            fundingGoal = 50000,
            fundingRaised = 30000
        };

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _tokenBalanceService.Setup(s => s.IsBalanceLiquidAsync(userId, configId, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        // Act
        var dto = new CreatePayoutRequestDto
        {
            SourceProjectTokenConfigId = configId,
            SourceTokenAmount = sourceAmount,
            HolderClass = holderClass
        };
        var result = await _sut.RequestPayoutAsync(userId, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("hasn't reached funding goal");

        _tokenBalanceService.Verify(s => s.LockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestPayoutAsync_TokensNotLiquid_InvestorActive_ReturnsGateMessage()
    {
        // Arrange
        var userId = "user-1";
        var configId = "config-1";
        var sourceAmount = 1000;
        var holderClass = TokenHolderClass.INVESTOR;

        var config = new ProjectTokenConfig
        {
            id = configId,
            projectId = "proj-1",
            gateStatus = ProjectGateStatus.ACTIVE,
            totalSupply = 10000,
            fundingGoal = 50000,
            fundingRaised = 60000
        };

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _tokenBalanceService.Setup(s => s.IsBalanceLiquidAsync(userId, configId, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        // Act
        var dto = new CreatePayoutRequestDto
        {
            SourceProjectTokenConfigId = configId,
            SourceTokenAmount = sourceAmount,
            HolderClass = holderClass
        };
        var result = await _sut.RequestPayoutAsync(userId, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("hasn't reached success milestone");

        _tokenBalanceService.Verify(s => s.LockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestPayoutAsync_LockFails_ReturnsFailure()
    {
        // Arrange
        var userId = "user-1";
        var configId = "config-1";
        var sourceAmount = 1000;
        var holderClass = TokenHolderClass.CONTRIBUTOR;

        var config = new ProjectTokenConfig
        {
            id = configId,
            projectId = "proj-1",
            gateStatus = ProjectGateStatus.ACTIVE,
            totalSupply = 10000,
            fundingGoal = 50000,
            fundingRaised = 60000
        };

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _tokenBalanceService.Setup(s => s.IsBalanceLiquidAsync(userId, configId, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        _tokenBalanceService.Setup(s => s.LockAsync(userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Failure("Insufficient balance"));

        // Act
        var dto = new CreatePayoutRequestDto
        {
            SourceProjectTokenConfigId = configId,
            SourceTokenAmount = sourceAmount,
            HolderClass = holderClass
        };
        var result = await _sut.RequestPayoutAsync(userId, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Insufficient balance");

        _exchangeService.Verify(s => s.CalculateConversionAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestPayoutAsync_ConversionFails_UnlocksTokens()
    {
        // Arrange
        var userId = "user-1";
        var configId = "config-1";
        var sourceAmount = 1000;
        var holderClass = TokenHolderClass.CONTRIBUTOR;

        var config = new ProjectTokenConfig
        {
            id = configId,
            projectId = "proj-1",
            gateStatus = ProjectGateStatus.ACTIVE,
            totalSupply = 10000,
            fundingGoal = 50000,
            fundingRaised = 60000
        };

        var balanceDto = new TokenBalanceDto
        {
            Id = "bal-1",
            UserId = userId,
            ProjectTokenConfigId = configId,
            HolderClass = holderClass,
            IsLiquid = true,
            Balance = 1000,
            LockedBalance = 1000
        };

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _tokenBalanceService.Setup(s => s.IsBalanceLiquidAsync(userId, configId, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        _tokenBalanceService.Setup(s => s.LockAsync(userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(balanceDto));

        _exchangeService.Setup(s => s.CalculateConversionAsync(configId, sourceAmount, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversionPreviewDto>.Failure("Conversion service unavailable"));

        _tokenBalanceService.Setup(s => s.UnlockAsync(userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(balanceDto));

        // Act
        var dto = new CreatePayoutRequestDto
        {
            SourceProjectTokenConfigId = configId,
            SourceTokenAmount = sourceAmount,
            HolderClass = holderClass
        };
        var result = await _sut.RequestPayoutAsync(userId, dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Conversion service unavailable");

        _tokenBalanceService.Verify(s => s.UnlockAsync(userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()), Times.Once);
        _payoutRepo.Verify(r => r.AddAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPayoutAsync_HappyPath_CompletesPayment()
    {
        // Arrange
        var payoutId = "payout-1";
        var userId = "user-1";
        var configId = "config-1";
        var sourceAmount = 1000;
        var ardaAmount = 500;
        var usdAmount = 100.0;
        var holderClass = TokenHolderClass.CONTRIBUTOR;

        var payout = new PayoutRequest
        {
            id = payoutId,
            userId = userId,
            sourceProjectTokenConfigId = configId,
            sourceTokenAmount = sourceAmount,
            ardaTokenAmount = ardaAmount,
            usdAmount = usdAmount,
            status = PayoutStatus.PENDING,
            holderClass = holderClass,
            gateStatusAtRequest = ProjectGateStatus.ACTIVE,
            requestedAt = DateTime.UtcNow
        };

        var payoutDto = new PayoutRequestDto
        {
            Id = payoutId,
            UserId = userId,
            Status = PayoutStatus.COMPLETED
        };

        _payoutRepo.Setup(r => r.GetByIdAsync(payoutId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payout);

        _payoutRepo.Setup(r => r.UpdateAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _treasuryService.Setup(s => s.RebalanceIfNeededAsync(usdAmount, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<double>.Success(0));

        _tokenBalanceService.Setup(s => s.DebitAsync(userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto()));

        _mapper.Setup(m => m.Map<PayoutRequestDto>(It.IsAny<PayoutRequest>()))
            .Returns(payoutDto);

        // Act
        var result = await _sut.ProcessPayoutAsync(payoutId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be(PayoutStatus.COMPLETED);

        _payoutRepo.Verify(r => r.UpdateAsync(It.Is<PayoutRequest>(p =>
            p.status == PayoutStatus.COMPLETED &&
            p.processedAt != null &&
            p.completedAt != null
        ), It.IsAny<CancellationToken>()), Times.Exactly(2));

        _treasuryService.Verify(s => s.RebalanceIfNeededAsync(usdAmount, It.IsAny<CancellationToken>()), Times.Once);
        _tokenBalanceService.Verify(s => s.DebitAsync(userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPayoutAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        var payoutId = "nonexistent";

        _payoutRepo.Setup(r => r.GetByIdAsync(payoutId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PayoutRequest?)null);

        // Act
        var result = await _sut.ProcessPayoutAsync(payoutId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");

        _treasuryService.Verify(s => s.RebalanceIfNeededAsync(It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPayoutAsync_NotPending_ReturnsFailure()
    {
        // Arrange
        var payoutId = "payout-1";

        var payout = new PayoutRequest
        {
            id = payoutId,
            userId = "user-1",
            sourceProjectTokenConfigId = "config-1",
            sourceTokenAmount = 1000,
            status = PayoutStatus.COMPLETED,
            holderClass = TokenHolderClass.CONTRIBUTOR,
            gateStatusAtRequest = ProjectGateStatus.ACTIVE,
            requestedAt = DateTime.UtcNow
        };

        _payoutRepo.Setup(r => r.GetByIdAsync(payoutId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payout);

        // Act
        var result = await _sut.ProcessPayoutAsync(payoutId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Only PENDING payouts can be processed");

        _treasuryService.Verify(s => s.RebalanceIfNeededAsync(It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPayoutAsync_RebalanceFails_RevertsTooPending()
    {
        // Arrange
        var payoutId = "payout-1";
        var userId = "user-1";
        var configId = "config-1";
        var usdAmount = 100.0;

        var payout = new PayoutRequest
        {
            id = payoutId,
            userId = userId,
            sourceProjectTokenConfigId = configId,
            sourceTokenAmount = 1000,
            ardaTokenAmount = 500,
            usdAmount = usdAmount,
            status = PayoutStatus.PENDING,
            holderClass = TokenHolderClass.CONTRIBUTOR,
            gateStatusAtRequest = ProjectGateStatus.ACTIVE,
            requestedAt = DateTime.UtcNow
        };

        _payoutRepo.Setup(r => r.GetByIdAsync(payoutId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payout);

        _payoutRepo.Setup(r => r.UpdateAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _treasuryService.Setup(s => s.RebalanceIfNeededAsync(usdAmount, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<double>.Failure("Rebalance failed"));

        // Act
        var result = await _sut.ProcessPayoutAsync(payoutId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Rebalance failed");

        _payoutRepo.Verify(r => r.UpdateAsync(It.Is<PayoutRequest>(p =>
            p.status == PayoutStatus.PENDING
        ), It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        _tokenBalanceService.Verify(s => s.DebitAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelPayoutAsync_HappyPath_CancelsAndUnlocks()
    {
        // Arrange
        var payoutId = "payout-1";
        var userId = "user-1";
        var configId = "config-1";
        var sourceAmount = 1000;
        var holderClass = TokenHolderClass.CONTRIBUTOR;

        var payout = new PayoutRequest
        {
            id = payoutId,
            userId = userId,
            sourceProjectTokenConfigId = configId,
            sourceTokenAmount = sourceAmount,
            status = PayoutStatus.PENDING,
            holderClass = holderClass,
            gateStatusAtRequest = ProjectGateStatus.ACTIVE,
            requestedAt = DateTime.UtcNow
        };

        var payoutDto = new PayoutRequestDto
        {
            Id = payoutId,
            UserId = userId,
            Status = PayoutStatus.CANCELLED
        };

        _payoutRepo.Setup(r => r.GetByIdAsync(payoutId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payout);

        _payoutRepo.Setup(r => r.UpdateAsync(It.IsAny<PayoutRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _tokenBalanceService.Setup(s => s.UnlockAsync(userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto()));

        _mapper.Setup(m => m.Map<PayoutRequestDto>(It.IsAny<PayoutRequest>()))
            .Returns(payoutDto);

        // Act
        var result = await _sut.CancelPayoutAsync(payoutId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be(PayoutStatus.CANCELLED);

        _tokenBalanceService.Verify(s => s.UnlockAsync(userId, configId, sourceAmount, holderClass, It.IsAny<CancellationToken>()), Times.Once);

        _payoutRepo.Verify(r => r.UpdateAsync(It.Is<PayoutRequest>(p =>
            p.status == PayoutStatus.CANCELLED
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelPayoutAsync_NotPending_ReturnsFailure()
    {
        // Arrange
        var payoutId = "payout-1";

        var payout = new PayoutRequest
        {
            id = payoutId,
            userId = "user-1",
            sourceProjectTokenConfigId = "config-1",
            sourceTokenAmount = 1000,
            status = PayoutStatus.COMPLETED,
            holderClass = TokenHolderClass.CONTRIBUTOR,
            gateStatusAtRequest = ProjectGateStatus.ACTIVE,
            requestedAt = DateTime.UtcNow
        };

        _payoutRepo.Setup(r => r.GetByIdAsync(payoutId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payout);

        // Act
        var result = await _sut.CancelPayoutAsync(payoutId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Only PENDING payouts can be cancelled");

        _tokenBalanceService.Verify(s => s.UnlockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TokenHolderClass>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPayoutsByUserAsync_ReturnsList()
    {
        // Arrange
        var userId = "user-1";

        var payouts = new List<PayoutRequest>
        {
            new PayoutRequest
            {
                id = "payout-1",
                userId = userId,
                sourceProjectTokenConfigId = "config-1",
                sourceTokenAmount = 1000,
                status = PayoutStatus.COMPLETED,
                holderClass = TokenHolderClass.CONTRIBUTOR,
                gateStatusAtRequest = ProjectGateStatus.ACTIVE,
                requestedAt = DateTime.UtcNow
            },
            new PayoutRequest
            {
                id = "payout-2",
                userId = userId,
                sourceProjectTokenConfigId = "config-2",
                sourceTokenAmount = 500,
                status = PayoutStatus.PENDING,
                holderClass = TokenHolderClass.INVESTOR,
                gateStatusAtRequest = ProjectGateStatus.SUCCEEDED,
                requestedAt = DateTime.UtcNow
            }
        };

        var payoutDtos = new List<PayoutRequestDto>
        {
            new PayoutRequestDto { Id = "payout-1", UserId = userId, Status = PayoutStatus.COMPLETED },
            new PayoutRequestDto { Id = "payout-2", UserId = userId, Status = PayoutStatus.PENDING }
        };

        _payoutRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PayoutRequest, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payouts);

        _mapper.Setup(m => m.Map<IEnumerable<PayoutRequestDto>>(It.IsAny<IEnumerable<PayoutRequest>>()))
            .Returns(payoutDtos);

        // Act
        var result = await _sut.GetPayoutsByUserAsync(userId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(p => p.Id == "payout-1");
        result.Value.Should().Contain(p => p.Id == "payout-2");
    }

    [Fact]
    public async Task GetPendingPayoutsAsync_ReturnsList()
    {
        // Arrange
        var payouts = new List<PayoutRequest>
        {
            new PayoutRequest
            {
                id = "payout-1",
                userId = "user-1",
                sourceProjectTokenConfigId = "config-1",
                sourceTokenAmount = 1000,
                status = PayoutStatus.PENDING,
                holderClass = TokenHolderClass.CONTRIBUTOR,
                gateStatusAtRequest = ProjectGateStatus.ACTIVE,
                requestedAt = DateTime.UtcNow
            },
            new PayoutRequest
            {
                id = "payout-2",
                userId = "user-2",
                sourceProjectTokenConfigId = "config-2",
                sourceTokenAmount = 500,
                status = PayoutStatus.PENDING,
                holderClass = TokenHolderClass.INVESTOR,
                gateStatusAtRequest = ProjectGateStatus.SUCCEEDED,
                requestedAt = DateTime.UtcNow
            }
        };

        var payoutDtos = new List<PayoutRequestDto>
        {
            new PayoutRequestDto { Id = "payout-1", UserId = "user-1", Status = PayoutStatus.PENDING },
            new PayoutRequestDto { Id = "payout-2", UserId = "user-2", Status = PayoutStatus.PENDING }
        };

        _payoutRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PayoutRequest, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payouts);

        _mapper.Setup(m => m.Map<IEnumerable<PayoutRequestDto>>(It.IsAny<IEnumerable<PayoutRequest>>()))
            .Returns(payoutDtos);

        // Act
        var result = await _sut.GetPendingPayoutsAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(p => p.Status == PayoutStatus.PENDING);
    }
}
