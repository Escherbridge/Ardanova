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

public class TokenSwapServiceTests
{
    private readonly Mock<IRepository<ShareSwap>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly TokenSwapService _sut;

    public TokenSwapServiceTests()
    {
        _repositoryMock = new Mock<IRepository<ShareSwap>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new TokenSwapService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenSwapExists_ReturnsSuccessResult()
    {
        // Arrange
        var swapId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var fromShareId = Guid.NewGuid().ToString();
        var toShareId = Guid.NewGuid().ToString();
        var swap = new ShareSwap
        {
            id = swapId,
            userId = userId,
            fromShareId = fromShareId,
            toShareId = toShareId,
            fromAmount = 100m,
            toAmount = 95m,
            exchangeRate = 0.95m,
            fee = 0m,
            status = SwapStatus.PENDING,
            createdAt = DateTime.UtcNow
        };
        var swapDto = new TokenSwapDto { Id = swapId, FromAmount = 100m, ToAmount = 95m };

        _repositoryMock.Setup(r => r.GetByIdAsync(swapId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(swap);
        _mapperMock.Setup(m => m.Map<TokenSwapDto>(swap)).Returns(swapDto);

        // Act
        var result = await _sut.GetByIdAsync(swapId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.FromAmount.Should().Be(100m);
    }

    [Fact]
    public async Task GetByIdAsync_WhenSwapNotExists_ReturnsNotFound()
    {
        // Arrange
        var swapId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(swapId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShareSwap?)null);

        // Act
        var result = await _sut.GetByIdAsync(swapId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsSwapsForUser()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var swaps = new List<ShareSwap>
        {
            new ShareSwap { id = Guid.NewGuid().ToString(), userId = userId, fromShareId = Guid.NewGuid().ToString(), toShareId = Guid.NewGuid().ToString(), fromAmount = 100m, toAmount = 95m, exchangeRate = 0.95m, fee = 0m, status = SwapStatus.PENDING, createdAt = DateTime.UtcNow },
            new ShareSwap { id = Guid.NewGuid().ToString(), userId = userId, fromShareId = Guid.NewGuid().ToString(), toShareId = Guid.NewGuid().ToString(), fromAmount = 200m, toAmount = 190m, exchangeRate = 0.95m, fee = 0m, status = SwapStatus.PENDING, createdAt = DateTime.UtcNow }
        };
        var swapDtos = new List<TokenSwapDto>
        {
            new TokenSwapDto { UserId = userId, FromAmount = 100m },
            new TokenSwapDto { UserId = userId, FromAmount = 200m }
        };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ShareSwap, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(swaps);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<TokenSwapDto>>(It.IsAny<IEnumerable<ShareSwap>>())).Returns(swapDtos);

        // Act
        var result = await _sut.GetByUserIdAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedSwap()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var fromShareId = Guid.NewGuid().ToString();
        var toShareId = Guid.NewGuid().ToString();
        var dto = new CreateTokenSwapDto
        {
            UserId = userId,
            FromShareId = fromShareId,
            ToShareId = toShareId,
            FromAmount = 1000m,
            ToAmount = 950m,
            ExchangeRate = 0.95m,
            Fee = 5m
        };
        var swapDto = new TokenSwapDto { UserId = userId, FromAmount = 1000m, Status = SwapStatus.PENDING };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<ShareSwap>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShareSwap s, CancellationToken _) => s);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<TokenSwapDto>(It.IsAny<ShareSwap>())).Returns(swapDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.FromAmount.Should().Be(1000m);
    }

    [Fact]
    public async Task CompleteAsync_WhenSwapExists_CompletesSwap()
    {
        // Arrange
        var swapId = Guid.NewGuid().ToString();
        var swap = new ShareSwap
        {
            id = swapId,
            userId = Guid.NewGuid().ToString(),
            fromShareId = Guid.NewGuid().ToString(),
            toShareId = Guid.NewGuid().ToString(),
            fromAmount = 100m,
            toAmount = 95m,
            exchangeRate = 0.95m,
            fee = 0m,
            status = SwapStatus.PENDING,
            createdAt = DateTime.UtcNow
        };
        var swapDto = new TokenSwapDto { Id = swapId, Status = SwapStatus.COMPLETED };

        _repositoryMock.Setup(r => r.GetByIdAsync(swapId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(swap);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ShareSwap>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<TokenSwapDto>(It.IsAny<ShareSwap>())).Returns(swapDto);

        // Act
        var result = await _sut.CompleteAsync(swapId, new CompleteSwapDto { TxHash = "TX123" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(SwapStatus.COMPLETED);
    }

    [Fact]
    public async Task CancelAsync_WhenSwapExists_CancelsSwap()
    {
        // Arrange
        var swapId = Guid.NewGuid().ToString();
        var swap = new ShareSwap
        {
            id = swapId,
            userId = Guid.NewGuid().ToString(),
            fromShareId = Guid.NewGuid().ToString(),
            toShareId = Guid.NewGuid().ToString(),
            fromAmount = 100m,
            toAmount = 95m,
            exchangeRate = 0.95m,
            fee = 0m,
            status = SwapStatus.PENDING,
            createdAt = DateTime.UtcNow
        };
        var swapDto = new TokenSwapDto { Id = swapId, Status = SwapStatus.CANCELLED };

        _repositoryMock.Setup(r => r.GetByIdAsync(swapId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(swap);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ShareSwap>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<TokenSwapDto>(It.IsAny<ShareSwap>())).Returns(swapDto);

        // Act
        var result = await _sut.CancelAsync(swapId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(SwapStatus.CANCELLED);
    }
}

public class LiquidityPoolServiceTests
{
    private readonly Mock<IRepository<LiquidityPool>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly LiquidityPoolService _sut;

    public LiquidityPoolServiceTests()
    {
        _repositoryMock = new Mock<IRepository<LiquidityPool>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new LiquidityPoolService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPoolExists_ReturnsSuccessResult()
    {
        // Arrange
        var poolId = Guid.NewGuid().ToString();
        var token1Id = Guid.NewGuid().ToString();
        var token2Id = Guid.NewGuid().ToString();
        var pool = new LiquidityPool
        {
            id = poolId,
            share1Id = token1Id,
            share2Id = token2Id,
            reserve1 = 0m,
            reserve2 = 0m,
            totalShares = 0m,
            feePercent = 0.003m,
            isActive = true,
            createdAt = DateTime.UtcNow
        };
        var poolDto = new LiquidityPoolDto { Id = poolId, Share1Id = token1Id, Share2Id = token2Id };

        _repositoryMock.Setup(r => r.GetByIdAsync(poolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pool);
        _mapperMock.Setup(m => m.Map<LiquidityPoolDto>(pool)).Returns(poolDto);

        // Act
        var result = await _sut.GetByIdAsync(poolId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenPoolNotExists_ReturnsNotFound()
    {
        // Arrange
        var poolId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(poolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LiquidityPool?)null);

        // Act
        var result = await _sut.GetByIdAsync(poolId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllPools()
    {
        // Arrange
        var pools = new List<LiquidityPool>
        {
            new LiquidityPool { id = Guid.NewGuid().ToString(), share1Id = Guid.NewGuid().ToString(), share2Id = Guid.NewGuid().ToString(), reserve1 = 0m, reserve2 = 0m, totalShares = 0m, feePercent = 0.003m, isActive = true, createdAt = DateTime.UtcNow },
            new LiquidityPool { id = Guid.NewGuid().ToString(), share1Id = Guid.NewGuid().ToString(), share2Id = Guid.NewGuid().ToString(), reserve1 = 0m, reserve2 = 0m, totalShares = 0m, feePercent = 0.003m, isActive = true, createdAt = DateTime.UtcNow }
        };
        var poolDtos = new List<LiquidityPoolDto>
        {
            new LiquidityPoolDto(),
            new LiquidityPoolDto()
        };

        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pools);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<LiquidityPoolDto>>(It.IsAny<IEnumerable<LiquidityPool>>())).Returns(poolDtos);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedPool()
    {
        // Arrange
        var share1Id = Guid.NewGuid().ToString();
        var share2Id = Guid.NewGuid().ToString();
        var dto = new CreateLiquidityPoolDto
        {
            Share1Id = share1Id,
            Share2Id = share2Id,
            FeePercent = 0.003m
        };
        var poolDto = new LiquidityPoolDto { Share1Id = share1Id, Share2Id = share2Id, FeePercent = 0.003m };

        _repositoryMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<LiquidityPool, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<LiquidityPool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LiquidityPool p, CancellationToken _) => p);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<LiquidityPoolDto>(It.IsAny<LiquidityPool>())).Returns(poolDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.FeePercent.Should().Be(0.003m);
    }
}

public class LiquidityProviderServiceTests
{
    private readonly Mock<IRepository<LiquidityProvider>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly LiquidityProviderService _sut;

    public LiquidityProviderServiceTests()
    {
        _repositoryMock = new Mock<IRepository<LiquidityProvider>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new LiquidityProviderService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProviderExists_ReturnsSuccessResult()
    {
        // Arrange
        var providerId = Guid.NewGuid().ToString();
        var poolId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var provider = new LiquidityProvider
        {
            id = providerId,
            poolId = poolId,
            userId = userId,
            shares = 100m,
            share1In = 100m,
            share2In = 100m,
            createdAt = DateTime.UtcNow
        };
        var providerDto = new LiquidityProviderDto { Id = providerId, PoolId = poolId, UserId = userId };

        _repositoryMock.Setup(r => r.GetByIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);
        _mapperMock.Setup(m => m.Map<LiquidityProviderDto>(provider)).Returns(providerDto);

        // Act
        var result = await _sut.GetByIdAsync(providerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenProviderNotExists_ReturnsNotFound()
    {
        // Arrange
        var providerId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LiquidityProvider?)null);

        // Act
        var result = await _sut.GetByIdAsync(providerId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetByPoolIdAsync_ReturnsProvidersForPool()
    {
        // Arrange
        var poolId = Guid.NewGuid().ToString();
        var providers = new List<LiquidityProvider>
        {
            new LiquidityProvider { id = Guid.NewGuid().ToString(), poolId = poolId, userId = Guid.NewGuid().ToString(), shares = 100m, share1In = 100m, share2In = 100m, createdAt = DateTime.UtcNow },
            new LiquidityProvider { id = Guid.NewGuid().ToString(), poolId = poolId, userId = Guid.NewGuid().ToString(), shares = 200m, share1In = 200m, share2In = 200m, createdAt = DateTime.UtcNow }
        };
        var providerDtos = new List<LiquidityProviderDto>
        {
            new LiquidityProviderDto { PoolId = poolId },
            new LiquidityProviderDto { PoolId = poolId }
        };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<LiquidityProvider, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<LiquidityProviderDto>>(It.IsAny<IEnumerable<LiquidityProvider>>())).Returns(providerDtos);

        // Act
        var result = await _sut.GetByPoolIdAsync(poolId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedProvider()
    {
        // Arrange
        var poolId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var dto = new CreateLiquidityProviderDto
        {
            PoolId = poolId,
            UserId = userId,
            Shares = 500m,
            Share1In = 500m,
            Share2In = 500m
        };
        var providerDto = new LiquidityProviderDto { PoolId = poolId, UserId = userId, Shares = 500m };

        _repositoryMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<LiquidityProvider, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<LiquidityProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LiquidityProvider p, CancellationToken _) => p);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<LiquidityProviderDto>(It.IsAny<LiquidityProvider>())).Returns(providerDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Shares.Should().Be(500m);
    }
}
