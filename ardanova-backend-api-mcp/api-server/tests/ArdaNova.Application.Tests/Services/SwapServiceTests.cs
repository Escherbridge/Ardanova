namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using FluentAssertions;
using Moq;

public sealed class SwapServiceTests
{
    [Fact]
    public async Task ExecuteSwapAsync_FailsClosedWithoutReadingOrMutatingBalances()
    {
        var balances = new Mock<IRepository<TokenBalance>>(MockBehavior.Strict);
        var configs = new Mock<IRepository<ProjectTokenConfig>>(MockBehavior.Strict);
        var treasury = new Mock<IRepository<PlatformTreasury>>(MockBehavior.Strict);
        var swaps = new Mock<IRepository<ShareSwap>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var service = new SwapService(
            balances.Object,
            configs.Object,
            treasury.Object,
            swaps.Object,
            unitOfWork.Object);

        var result = await service.ExecuteSwapAsync("actor-1", new SwapRequestDto
        {
            SourceConfigId = "source",
            TargetConfigId = "target",
            SourceTokenAmount = 1,
        });

        result.IsFailure.Should().BeTrue();
        balances.VerifyNoOtherCalls();
        configs.VerifyNoOtherCalls();
        treasury.VerifyNoOtherCalls();
        swaps.VerifyNoOtherCalls();
        unitOfWork.VerifyNoOtherCalls();
    }
}
