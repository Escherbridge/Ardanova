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

public class ReferralServiceTests
{
    private readonly Mock<IRepository<Referral>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly ReferralService _sut;

    public ReferralServiceTests()
    {
        _repositoryMock = new Mock<IRepository<Referral>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new ReferralService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenReferralExists_ReturnsSuccessResult()
    {
        // Arrange
        var referralId = Guid.NewGuid().ToString();
        var referrerId = Guid.NewGuid().ToString();
        var referredId = Guid.NewGuid().ToString();
        var referral = new Referral
        {
            id = referralId,
            referrerId = referrerId,
            referredId = referredId,
            referralCode = "REF123",
            status = ReferralStatus.PENDING,
            rewardClaimed = false,
            createdAt = DateTime.UtcNow
        };
        var referralDto = new ReferralDto { Id = referralId, ReferrerId = referrerId, ReferredId = referredId };

        _repositoryMock.Setup(r => r.GetByIdAsync(referralId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(referral);
        _mapperMock.Setup(m => m.Map<ReferralDto>(referral)).Returns(referralDto);

        // Act
        var result = await _sut.GetByIdAsync(referralId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenReferralNotExists_ReturnsNotFound()
    {
        // Arrange
        var referralId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(referralId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Referral?)null);

        // Act
        var result = await _sut.GetByIdAsync(referralId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetByReferrerIdAsync_ReturnsReferralsForReferrer()
    {
        // Arrange
        var referrerId = Guid.NewGuid().ToString();
        var referrals = new List<Referral>
        {
            new Referral { id = Guid.NewGuid().ToString(), referrerId = referrerId, referredId = Guid.NewGuid().ToString(), referralCode = "REF1", status = ReferralStatus.PENDING, rewardClaimed = false, createdAt = DateTime.UtcNow },
            new Referral { id = Guid.NewGuid().ToString(), referrerId = referrerId, referredId = Guid.NewGuid().ToString(), referralCode = "REF2", status = ReferralStatus.PENDING, rewardClaimed = false, createdAt = DateTime.UtcNow }
        };
        var referralDtos = new List<ReferralDto>
        {
            new ReferralDto { ReferrerId = referrerId },
            new ReferralDto { ReferrerId = referrerId }
        };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Referral, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(referrals);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<ReferralDto>>(It.IsAny<IEnumerable<Referral>>())).Returns(referralDtos);

        // Act
        var result = await _sut.GetByReferrerIdAsync(referrerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedReferral()
    {
        // Arrange
        var referrerId = Guid.NewGuid().ToString();
        var referredId = Guid.NewGuid().ToString();
        var dto = new CreateReferralDto
        {
            ReferrerId = referrerId,
            ReferredId = referredId
        };
        var referralDto = new ReferralDto { ReferrerId = referrerId, ReferredId = referredId, Status = ReferralStatus.PENDING };

        _repositoryMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Referral, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Referral>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Referral ref_, CancellationToken _) => ref_);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<ReferralDto>(It.IsAny<Referral>())).Returns(referralDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be(ReferralStatus.PENDING);
    }

    [Fact]
    public async Task CompleteAsync_WhenReferralExists_CompletesReferral()
    {
        // Arrange
        var referralId = Guid.NewGuid().ToString();
        var referrerId = Guid.NewGuid().ToString();
        var referredId = Guid.NewGuid().ToString();
        var referral = new Referral
        {
            id = referralId,
            referrerId = referrerId,
            referredId = referredId,
            referralCode = "REF123",
            status = ReferralStatus.PENDING,
            rewardClaimed = false,
            createdAt = DateTime.UtcNow
        };
        var referralDto = new ReferralDto { Id = referralId, Status = ReferralStatus.COMPLETED };

        _repositoryMock.Setup(r => r.GetByIdAsync(referralId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(referral);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Referral>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<ReferralDto>(It.IsAny<Referral>())).Returns(referralDto);

        // Act
        var result = await _sut.CompleteAsync(referralId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ReferralStatus.COMPLETED);
    }

    [Fact]
    public async Task ClaimRewardAsync_WhenReferralCompleted_ClaimsReward()
    {
        // Arrange
        var referralId = Guid.NewGuid().ToString();
        var referrerId = Guid.NewGuid().ToString();
        var referredId = Guid.NewGuid().ToString();
        var referral = new Referral
        {
            id = referralId,
            referrerId = referrerId,
            referredId = referredId,
            referralCode = "REF123",
            status = ReferralStatus.COMPLETED,
            rewardClaimed = false,
            createdAt = DateTime.UtcNow
        };
        var referralDto = new ReferralDto { Id = referralId, RewardClaimed = true };

        _repositoryMock.Setup(r => r.GetByIdAsync(referralId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(referral);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Referral>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<ReferralDto>(It.IsAny<Referral>())).Returns(referralDto);

        var dto = new ClaimReferralRewardDto { XpAmount = 100, TokenAmount = 50m };

        // Act
        var result = await _sut.ClaimRewardAsync(referralId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.RewardClaimed.Should().BeTrue();
    }
}
