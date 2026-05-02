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

public class ProjectGateServiceTests
{
    private readonly Mock<IRepository<ProjectTokenConfig>> _projectTokenConfigRepo;
    private readonly Mock<IRepository<TokenAllocation>> _tokenAllocationRepo;
    private readonly Mock<IRepository<TokenBalance>> _tokenBalanceRepo;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IMapper> _mapper;
    private readonly Mock<IProjectTokenService> _projectTokenService;
    private readonly Mock<ITreasuryService> _treasuryService;
    private readonly ProjectGateService _sut;

    public ProjectGateServiceTests()
    {
        _projectTokenConfigRepo = new Mock<IRepository<ProjectTokenConfig>>();
        _tokenAllocationRepo = new Mock<IRepository<TokenAllocation>>();
        _tokenBalanceRepo = new Mock<IRepository<TokenBalance>>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _mapper = new Mock<IMapper>();
        _projectTokenService = new Mock<IProjectTokenService>();
        _treasuryService = new Mock<ITreasuryService>();

        _sut = new ProjectGateService(
            _projectTokenConfigRepo.Object,
            _tokenAllocationRepo.Object,
            _tokenBalanceRepo.Object,
            _unitOfWork.Object,
            _mapper.Object,
            _projectTokenService.Object,
            _treasuryService.Object
        );
    }

    [Fact]
    public async Task EvaluateGate1Async_HappyPath_FundingMet_TransitionsToActive()
    {
        // Arrange
        var configId = "config-1";
        var config = new ProjectTokenConfig
        {
            id = configId,
            projectId = "proj-1",
            totalSupply = 10000,
            fundingGoal = 50000,
            fundingRaised = 60000,
            gateStatus = ProjectGateStatus.FUNDING
        };

        var allocations = new List<TokenAllocation>
        {
            new TokenAllocation
            {
                id = "alloc-1",
                projectTokenConfigId = configId,
                holderClass = TokenHolderClass.CONTRIBUTOR,
                tokenAmount = 2000,
                isLiquid = false,
                status = AllocationStatus.DISTRIBUTED
            },
            new TokenAllocation
            {
                id = "alloc-2",
                projectTokenConfigId = configId,
                holderClass = TokenHolderClass.CONTRIBUTOR,
                tokenAmount = 1500,
                isLiquid = false,
                status = AllocationStatus.DISTRIBUTED
            }
        };

        var balances = new List<TokenBalance>
        {
            new TokenBalance
            {
                id = "bal-1",
                userId = "user-1",
                projectTokenConfigId = configId,
                holderClass = TokenHolderClass.CONTRIBUTOR,
                balance = 500,
                isLiquid = false
            },
            new TokenBalance
            {
                id = "bal-2",
                userId = "user-2",
                projectTokenConfigId = configId,
                holderClass = TokenHolderClass.CONTRIBUTOR,
                balance = 300,
                isLiquid = false
            }
        };

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _projectTokenConfigRepo.Setup(r => r.UpdateAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tokenAllocationRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<TokenAllocation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);

        _tokenAllocationRepo.Setup(r => r.UpdateAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tokenBalanceRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<TokenBalance, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(balances);

        _tokenBalanceRepo.Setup(r => r.UpdateAsync(It.IsAny<TokenBalance>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.EvaluateGate1Async(configId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Transitioned.Should().BeTrue();
        result.Value.PreviousStatus.Should().Be(ProjectGateStatus.FUNDING);
        result.Value.NewStatus.Should().Be(ProjectGateStatus.ACTIVE);
        result.Value.TokensUnlocked.Should().Be(4300); // 2000 + 1500 + 500 + 300

        _projectTokenConfigRepo.Verify(r => r.UpdateAsync(It.Is<ProjectTokenConfig>(c =>
            c.gateStatus == ProjectGateStatus.ACTIVE &&
            c.gate1ClearedAt != null
        ), It.IsAny<CancellationToken>()), Times.Once);

        _tokenAllocationRepo.Verify(r => r.UpdateAsync(It.Is<TokenAllocation>(a =>
            a.isLiquid == true
        ), It.IsAny<CancellationToken>()), Times.Exactly(2));

        _tokenBalanceRepo.Verify(r => r.UpdateAsync(It.Is<TokenBalance>(b =>
            b.isLiquid == true
        ), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task EvaluateGate1Async_FundingNotMet_NoTransition()
    {
        // Arrange
        var configId = "config-1";
        var config = new ProjectTokenConfig
        {
            id = configId,
            projectId = "proj-1",
            totalSupply = 10000,
            fundingGoal = 50000,
            fundingRaised = 30000,
            gateStatus = ProjectGateStatus.FUNDING
        };

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        var result = await _sut.EvaluateGate1Async(configId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Transitioned.Should().BeFalse();
        result.Value.PreviousStatus.Should().Be(ProjectGateStatus.FUNDING);
        result.Value.NewStatus.Should().Be(ProjectGateStatus.FUNDING);
        result.Value.TokensUnlocked.Should().Be(0);

        _projectTokenConfigRepo.Verify(r => r.UpdateAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        _tokenAllocationRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<TokenAllocation, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateGate1Async_ConfigNotFound_ReturnsFailure()
    {
        // Arrange
        var configId = "nonexistent";

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTokenConfig?)null);

        // Act
        var result = await _sut.EvaluateGate1Async(configId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task EvaluateGate1Async_WrongGateStatus_ReturnsFailure()
    {
        // Arrange
        var configId = "config-1";
        var config = new ProjectTokenConfig
        {
            id = configId,
            projectId = "proj-1",
            totalSupply = 10000,
            fundingGoal = 50000,
            fundingRaised = 60000,
            gateStatus = ProjectGateStatus.ACTIVE
        };

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        var result = await _sut.EvaluateGate1Async(configId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Expected FUNDING");
    }

    [Fact]
    public async Task ClearGate2Async_HappyPath_TransitionsToSucceeded()
    {
        // Arrange
        var configId = "config-1";
        var verifiedByUserId = "admin-1";
        var config = new ProjectTokenConfig
        {
            id = configId,
            projectId = "proj-1",
            totalSupply = 10000,
            fundingGoal = 50000,
            fundingRaised = 60000,
            gateStatus = ProjectGateStatus.ACTIVE
        };

        var allocations = new List<TokenAllocation>
        {
            new TokenAllocation
            {
                id = "alloc-1",
                projectTokenConfigId = configId,
                holderClass = TokenHolderClass.INVESTOR,
                tokenAmount = 3000,
                isLiquid = false,
                status = AllocationStatus.DISTRIBUTED
            },
            new TokenAllocation
            {
                id = "alloc-2",
                projectTokenConfigId = configId,
                holderClass = TokenHolderClass.FOUNDER,
                tokenAmount = 2000,
                isLiquid = false,
                status = AllocationStatus.DISTRIBUTED
            }
        };

        var balances = new List<TokenBalance>
        {
            new TokenBalance
            {
                id = "bal-1",
                userId = "user-1",
                projectTokenConfigId = configId,
                holderClass = TokenHolderClass.INVESTOR,
                balance = 800,
                isLiquid = false
            },
            new TokenBalance
            {
                id = "bal-2",
                userId = "user-2",
                projectTokenConfigId = configId,
                holderClass = TokenHolderClass.FOUNDER,
                balance = 500,
                isLiquid = false
            }
        };

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _projectTokenConfigRepo.Setup(r => r.UpdateAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tokenAllocationRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<TokenAllocation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);

        _tokenAllocationRepo.Setup(r => r.UpdateAsync(It.IsAny<TokenAllocation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tokenBalanceRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<TokenBalance, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(balances);

        _tokenBalanceRepo.Setup(r => r.UpdateAsync(It.IsAny<TokenBalance>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.ClearGate2Async(configId, verifiedByUserId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Transitioned.Should().BeTrue();
        result.Value.PreviousStatus.Should().Be(ProjectGateStatus.ACTIVE);
        result.Value.NewStatus.Should().Be(ProjectGateStatus.SUCCEEDED);
        result.Value.TokensUnlocked.Should().Be(6300); // 3000 + 2000 + 800 + 500

        _projectTokenConfigRepo.Verify(r => r.UpdateAsync(It.Is<ProjectTokenConfig>(c =>
            c.gateStatus == ProjectGateStatus.SUCCEEDED &&
            c.gate2ClearedAt != null &&
            c.successVerifiedBy == verifiedByUserId
        ), It.IsAny<CancellationToken>()), Times.Once);

        _tokenAllocationRepo.Verify(r => r.UpdateAsync(It.Is<TokenAllocation>(a =>
            a.isLiquid == true
        ), It.IsAny<CancellationToken>()), Times.Exactly(2));

        _tokenBalanceRepo.Verify(r => r.UpdateAsync(It.Is<TokenBalance>(b =>
            b.isLiquid == true
        ), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ClearGate2Async_ConfigNotFound_ReturnsFailure()
    {
        // Arrange
        var configId = "nonexistent";
        var verifiedByUserId = "admin-1";

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTokenConfig?)null);

        // Act
        var result = await _sut.ClearGate2Async(configId, verifiedByUserId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task ClearGate2Async_WrongGateStatus_ReturnsFailure()
    {
        // Arrange
        var configId = "config-1";
        var verifiedByUserId = "admin-1";
        var config = new ProjectTokenConfig
        {
            id = configId,
            projectId = "proj-1",
            totalSupply = 10000,
            fundingGoal = 50000,
            fundingRaised = 60000,
            gateStatus = ProjectGateStatus.FUNDING
        };

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        var result = await _sut.ClearGate2Async(configId, verifiedByUserId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Expected ACTIVE");
    }

    [Fact]
    public async Task FailProjectAsync_HappyPath_TransitionsToFailed()
    {
        // Arrange
        var configId = "config-1";
        var config = new ProjectTokenConfig
        {
            id = configId,
            projectId = "proj-1",
            totalSupply = 10000,
            fundingGoal = 50000,
            fundingRaised = 60000,
            gateStatus = ProjectGateStatus.ACTIVE
        };

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _projectTokenConfigRepo.Setup(r => r.UpdateAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _projectTokenService.Setup(s => s.BurnFounderTokensAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        _projectTokenService.Setup(s => s.ProcessInvestorTrustProtectionAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        // Act
        var result = await _sut.FailProjectAsync(configId, "Project failed to meet milestones", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Transitioned.Should().BeTrue();
        result.Value.PreviousStatus.Should().Be(ProjectGateStatus.ACTIVE);
        result.Value.NewStatus.Should().Be(ProjectGateStatus.FAILED);

        _projectTokenConfigRepo.Verify(r => r.UpdateAsync(It.Is<ProjectTokenConfig>(c =>
            c.gateStatus == ProjectGateStatus.FAILED &&
            c.failedAt != null
        ), It.IsAny<CancellationToken>()), Times.Once);

        _projectTokenService.Verify(s => s.BurnFounderTokensAsync(configId, It.IsAny<CancellationToken>()), Times.Once);
        _projectTokenService.Verify(s => s.ProcessInvestorTrustProtectionAsync(configId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FailProjectAsync_ConfigNotFound_ReturnsFailure()
    {
        // Arrange
        var configId = "nonexistent";

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTokenConfig?)null);

        // Act
        var result = await _sut.FailProjectAsync(configId, "Test failure", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");

        _projectTokenService.Verify(s => s.BurnFounderTokensAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FailProjectAsync_WrongGateStatus_ReturnsFailure()
    {
        // Arrange
        var configId = "config-1";
        var config = new ProjectTokenConfig
        {
            id = configId,
            projectId = "proj-1",
            totalSupply = 10000,
            fundingGoal = 50000,
            fundingRaised = 60000,
            gateStatus = ProjectGateStatus.FUNDING
        };

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        var result = await _sut.FailProjectAsync(configId, "Test failure", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Can only fail projects with ACTIVE status");

        _projectTokenService.Verify(s => s.BurnFounderTokensAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FailProjectAsync_BurnFails_ReturnsFailure()
    {
        // Arrange
        var configId = "config-1";
        var config = new ProjectTokenConfig
        {
            id = configId,
            projectId = "proj-1",
            totalSupply = 10000,
            fundingGoal = 50000,
            fundingRaised = 60000,
            gateStatus = ProjectGateStatus.ACTIVE
        };

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _projectTokenConfigRepo.Setup(r => r.UpdateAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _projectTokenService.Setup(s => s.BurnFounderTokensAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure("Burn operation failed"));

        // Act
        var result = await _sut.FailProjectAsync(configId, "Test failure", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Burn operation failed");

        _projectTokenService.Verify(s => s.BurnFounderTokensAsync(configId, It.IsAny<CancellationToken>()), Times.Once);
        _projectTokenService.Verify(s => s.ProcessInvestorTrustProtectionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FailProjectAsync_TrustProtectionFails_ReturnsFailure()
    {
        // Arrange
        var configId = "config-1";
        var config = new ProjectTokenConfig
        {
            id = configId,
            projectId = "proj-1",
            totalSupply = 10000,
            fundingGoal = 50000,
            fundingRaised = 60000,
            gateStatus = ProjectGateStatus.ACTIVE
        };

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _projectTokenConfigRepo.Setup(r => r.UpdateAsync(It.IsAny<ProjectTokenConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _projectTokenService.Setup(s => s.BurnFounderTokensAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        _projectTokenService.Setup(s => s.ProcessInvestorTrustProtectionAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure("Trust protection failed"));

        // Act
        var result = await _sut.FailProjectAsync(configId, "Test failure", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Trust protection failed");

        _projectTokenService.Verify(s => s.BurnFounderTokensAsync(configId, It.IsAny<CancellationToken>()), Times.Once);
        _projectTokenService.Verify(s => s.ProcessInvestorTrustProtectionAsync(configId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetGateStatusAsync_ReturnsStatus()
    {
        // Arrange
        var configId = "config-1";
        var config = new ProjectTokenConfig
        {
            id = configId,
            projectId = "proj-1",
            totalSupply = 10000,
            fundingGoal = 50000,
            fundingRaised = 60000,
            gateStatus = ProjectGateStatus.ACTIVE,
            gate1ClearedAt = DateTime.UtcNow.AddDays(-10),
            gate2ClearedAt = null,
            failedAt = null
        };

        var statusDto = new ProjectGateStatusDto
        {
            ProjectTokenConfigId = configId,
            GateStatus = ProjectGateStatus.ACTIVE,
            FundingGoal = 50000,
            FundingRaised = 60000,
            Gate1ClearedAt = config.gate1ClearedAt,
            Gate2ClearedAt = null,
            FailedAt = null
        };

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _mapper.Setup(m => m.Map<ProjectGateStatusDto>(It.IsAny<ProjectTokenConfig>()))
            .Returns(statusDto);

        // Act
        var result = await _sut.GetGateStatusAsync(configId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ProjectTokenConfigId.Should().Be(configId);
        result.Value.GateStatus.Should().Be(ProjectGateStatus.ACTIVE);
        result.Value.FundingGoal.Should().Be(50000);
        result.Value.FundingRaised.Should().Be(60000);
        result.Value.Gate1ClearedAt.Should().NotBeNull();
        result.Value.Gate2ClearedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetGateStatusAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        var configId = "nonexistent";

        _projectTokenConfigRepo.Setup(r => r.GetByIdAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTokenConfig?)null);

        // Act
        var result = await _sut.GetGateStatusAsync(configId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }
}
