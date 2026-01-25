namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using AutoMapper;
using FluentAssertions;
using Moq;

public class DelegatedVoteServiceTests
{
    private readonly Mock<IRepository<DelegatedVote>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly DelegatedVoteService _sut;

    public DelegatedVoteServiceTests()
    {
        _repositoryMock = new Mock<IRepository<DelegatedVote>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new DelegatedVoteService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenDelegationExists_ReturnsSuccessResult()
    {
        // Arrange
        var delegationId = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid().ToString();
        var delegatorId = Guid.NewGuid().ToString();
        var delegateeId = Guid.NewGuid().ToString();
        var tokenId = Guid.NewGuid().ToString();
        var delegation = new DelegatedVote
        {
            id = delegationId,
            projectId = projectId,
            delegatorId = delegatorId,
            delegateeId = delegateeId,
            tokenId = tokenId,
            amount = 100m,
            isActive = true,
            createdAt = DateTime.UtcNow
        };
        var delegationDto = new DelegatedVoteDto { Id = delegationId, Amount = 100m };

        _repositoryMock.Setup(r => r.GetByIdAsync(delegationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delegation);
        _mapperMock.Setup(m => m.Map<DelegatedVoteDto>(delegation)).Returns(delegationDto);

        // Act
        var result = await _sut.GetByIdAsync(delegationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Amount.Should().Be(100m);
    }

    [Fact]
    public async Task GetByIdAsync_WhenDelegationNotExists_ReturnsNotFound()
    {
        // Arrange
        var delegationId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(delegationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DelegatedVote?)null);

        // Act
        var result = await _sut.GetByIdAsync(delegationId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetByDelegatorIdAsync_ReturnsDelegationsForDelegator()
    {
        // Arrange
        var delegatorId = Guid.NewGuid().ToString();
        var delegations = new List<DelegatedVote>
        {
            new DelegatedVote { id = Guid.NewGuid().ToString(), projectId = Guid.NewGuid().ToString(), delegatorId = delegatorId, delegateeId = Guid.NewGuid().ToString(), tokenId = Guid.NewGuid().ToString(), amount = 100m, isActive = true, createdAt = DateTime.UtcNow },
            new DelegatedVote { id = Guid.NewGuid().ToString(), projectId = Guid.NewGuid().ToString(), delegatorId = delegatorId, delegateeId = Guid.NewGuid().ToString(), tokenId = Guid.NewGuid().ToString(), amount = 200m, isActive = true, createdAt = DateTime.UtcNow }
        };
        var delegationDtos = new List<DelegatedVoteDto>
        {
            new DelegatedVoteDto { DelegatorId = delegatorId, Amount = 100m },
            new DelegatedVoteDto { DelegatorId = delegatorId, Amount = 200m }
        };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<DelegatedVote, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(delegations);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<DelegatedVoteDto>>(delegations)).Returns(delegationDtos);

        // Act
        var result = await _sut.GetByDelegatorIdAsync(delegatorId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByDelegateeIdAsync_ReturnsDelegationsToDelegatee()
    {
        // Arrange
        var delegateeId = Guid.NewGuid().ToString();
        var delegations = new List<DelegatedVote>
        {
            new DelegatedVote { id = Guid.NewGuid().ToString(), projectId = Guid.NewGuid().ToString(), delegatorId = Guid.NewGuid().ToString(), delegateeId = delegateeId, tokenId = Guid.NewGuid().ToString(), amount = 150m, isActive = true, createdAt = DateTime.UtcNow }
        };
        var delegationDtos = new List<DelegatedVoteDto>
        {
            new DelegatedVoteDto { DelegateeId = delegateeId, Amount = 150m }
        };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<DelegatedVote, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(delegations);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<DelegatedVoteDto>>(delegations)).Returns(delegationDtos);

        // Act
        var result = await _sut.GetByDelegateeIdAsync(delegateeId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetActiveByProjectIdAsync_ReturnsOnlyActiveAndNonExpiredDelegations()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var delegations = new List<DelegatedVote>
        {
            new DelegatedVote { id = Guid.NewGuid().ToString(), projectId = projectId, delegatorId = Guid.NewGuid().ToString(), delegateeId = Guid.NewGuid().ToString(), tokenId = Guid.NewGuid().ToString(), amount = 100m, isActive = true, createdAt = DateTime.UtcNow }
        };
        var delegationDtos = new List<DelegatedVoteDto>
        {
            new DelegatedVoteDto { ProjectId = projectId, Amount = 100m, IsActive = true }
        };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<DelegatedVote, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(delegations);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<DelegatedVoteDto>>(delegations)).Returns(delegationDtos);

        // Act
        var result = await _sut.GetActiveByProjectIdAsync(projectId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTotalDelegatedPowerAsync_ReturnsSumOfActiveDelegations()
    {
        // Arrange
        var delegateeId = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid().ToString();
        var delegations = new List<DelegatedVote>
        {
            new DelegatedVote { id = Guid.NewGuid().ToString(), projectId = projectId, delegatorId = Guid.NewGuid().ToString(), delegateeId = delegateeId, tokenId = Guid.NewGuid().ToString(), amount = 100m, isActive = true, createdAt = DateTime.UtcNow },
            new DelegatedVote { id = Guid.NewGuid().ToString(), projectId = projectId, delegatorId = Guid.NewGuid().ToString(), delegateeId = delegateeId, tokenId = Guid.NewGuid().ToString(), amount = 200m, isActive = true, createdAt = DateTime.UtcNow }
        };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<DelegatedVote, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(delegations);

        // Act
        var result = await _sut.GetTotalDelegatedPowerAsync(delegateeId, projectId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(300m);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedDelegation()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var delegatorId = Guid.NewGuid().ToString();
        var delegateeId = Guid.NewGuid().ToString();
        var tokenId = Guid.NewGuid().ToString();
        var dto = new CreateDelegatedVoteDto
        {
            ProjectId = projectId,
            DelegatorId = delegatorId,
            DelegateeId = delegateeId,
            TokenId = tokenId,
            Amount = 500m
        };
        var delegationDto = new DelegatedVoteDto { Amount = 500m, IsActive = true };

        _repositoryMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<DelegatedVote, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<DelegatedVote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DelegatedVote d, CancellationToken _) => d);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<DelegatedVoteDto>(It.IsAny<DelegatedVote>())).Returns(delegationDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Amount.Should().Be(500m);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateDelegation_ReturnsValidationError()
    {
        // Arrange
        var dto = new CreateDelegatedVoteDto
        {
            ProjectId = Guid.NewGuid().ToString(),
            DelegatorId = Guid.NewGuid().ToString(),
            DelegateeId = Guid.NewGuid().ToString(),
            TokenId = Guid.NewGuid().ToString(),
            Amount = 100m
        };

        _repositoryMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<DelegatedVote, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    [Fact]
    public async Task RevokeAsync_WhenDelegationExists_RevokesDelegation()
    {
        // Arrange
        var delegationId = Guid.NewGuid().ToString();
        var delegation = new DelegatedVote
        {
            id = delegationId,
            projectId = Guid.NewGuid().ToString(),
            delegatorId = Guid.NewGuid().ToString(),
            delegateeId = Guid.NewGuid().ToString(),
            tokenId = Guid.NewGuid().ToString(),
            amount = 100m,
            isActive = true,
            createdAt = DateTime.UtcNow
        };
        var delegationDto = new DelegatedVoteDto { Id = delegationId, IsActive = false };

        _repositoryMock.Setup(r => r.GetByIdAsync(delegationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(delegation);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<DelegatedVote>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<DelegatedVoteDto>(It.IsAny<DelegatedVote>())).Returns(delegationDto);

        // Act
        var result = await _sut.RevokeAsync(delegationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeFalse();
    }
}
