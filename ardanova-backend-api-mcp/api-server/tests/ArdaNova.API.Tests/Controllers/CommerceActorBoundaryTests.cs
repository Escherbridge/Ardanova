namespace ArdaNova.API.Tests.Controllers;

using System.Security.Claims;
using System.Reflection;
using ArdaNova.API.Controllers;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class CommerceActorBoundaryTests
{
    [Fact]
    public async Task WalletCreate_UsesActorInsteadOfCallerSuppliedIdentity()
    {
        var wallets = new Mock<IWalletService>();
        wallets.Setup(service => service.CreateAsync(
                It.Is<CreateWalletDto>(dto => dto.UserId == "actor-1" && dto.Address == "ALGO1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WalletDto>.Success(new WalletDto { Id = "wallet-1", UserId = "actor-1", Address = "ALGO1" }));
        var controller = WithActor(new WalletsController(wallets.Object), "actor-1");

        var result = await controller.Create(
            new WalletsController.CreateWalletRequest("ALGO1", WalletProvider.PERA, null, false),
            CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        wallets.VerifyAll();
    }

    [Fact]
    public async Task PayoutRequest_FailsClosedBeforeInvokingTheService()
    {
        var payouts = new Mock<IPayoutService>(MockBehavior.Strict);
        var controller = WithActor(new PayoutsController(payouts.Object), "actor-1");

        var result = await controller.RequestPayout(new CreatePayoutRequestDto
        {
            SourceProjectTokenConfigId = "config-1",
            SourceTokenAmount = 10,
            HolderClass = TokenHolderClass.CONTRIBUTOR
        }, CancellationToken.None);

        var response = result.Should().BeOfType<ObjectResult>().Subject;
        response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        payouts.Verify(service => service.RequestPayoutAsync(
            It.IsAny<string>(),
            It.IsAny<CreatePayoutRequestDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PayoutProcessing_FailsClosedBeforeInvokingTheService()
    {
        var payouts = new Mock<IPayoutService>(MockBehavior.Strict);
        var controller = new PayoutsController(payouts.Object);

        var result = await controller.ProcessPayout("payout-1", CancellationToken.None);

        var response = result.Should().BeOfType<ObjectResult>().Subject;
        response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        payouts.Verify(service => service.ProcessPayoutAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TokenBalanceReads_UseActorIdentityInsteadOfRouteIdentity()
    {
        var balances = new Mock<ITokenBalanceService>();
        balances.Setup(service => service.GetBalanceAsync(
                "actor-1", "config-1", TokenHolderClass.CONTRIBUTOR, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TokenBalanceDto>.Success(new TokenBalanceDto
            {
                UserId = "actor-1",
                ProjectTokenConfigId = "config-1",
                HolderClass = TokenHolderClass.CONTRIBUTOR
            }));
        var controller = WithActor(
            new TokenBalanceController(balances.Object, new Mock<IExchangeService>(MockBehavior.Strict).Object),
            "actor-1");

        var result = await controller.GetBalance("config-1", TokenHolderClass.CONTRIBUTOR, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        balances.VerifyAll();
    }

    [Fact]
    public async Task TokenBalancePortfolio_UsesActorIdentity()
    {
        var balances = new Mock<ITokenBalanceService>();
        balances.Setup(service => service.GetPortfolioAsync("actor-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserPortfolioDto>.Success(new UserPortfolioDto { UserId = "actor-1" }));
        var controller = WithActor(
            new TokenBalanceController(balances.Object, new Mock<IExchangeService>(MockBehavior.Strict).Object),
            "actor-1");

        var result = await controller.GetPortfolio(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        balances.VerifyAll();
    }

    [Theory]
    [InlineData(nameof(TokenBalanceController.GetBalance))]
    [InlineData(nameof(TokenBalanceController.GetArdaBalance))]
    [InlineData(nameof(TokenBalanceController.GetPortfolio))]
    [InlineData(nameof(TokenBalanceController.IsBalanceLiquid))]
    public void TokenBalanceSelfReads_RequireActorAssertion(string actionName)
    {
        var action = typeof(TokenBalanceController).GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public)!;

        action.GetCustomAttribute<AuthorizeAttribute>()?.Policy.Should().Be(AuthorizationPolicies.ActorAssertion);
    }

    [Fact]
    public async Task TaskEscrowRelease_ForeignEscrowIsForbiddenBeforeMutation()
    {
        var escrows = new Mock<ITaskEscrowService>();
        escrows.Setup(service => service.GetByIdAsync("escrow-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskEscrowDto>.Success(new TaskEscrowDto { Id = "escrow-1", FunderId = "other-user" }));
        var controller = WithActor(new TaskEscrowsController(escrows.Object), "actor-1");

        var result = await controller.Release("escrow-1", new ReleaseEscrowDto(), CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        escrows.Verify(service => service.ReleaseAsync(It.IsAny<string>(), It.IsAny<ReleaseEscrowDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void TaskEscrowCreate_FailsClosedBeforeInvokingTheService()
    {
        var escrows = new Mock<ITaskEscrowService>(MockBehavior.Strict);
        var controller = WithActor(new TaskEscrowsController(escrows.Object), "actor-1");

        var result = controller.Create(new TaskEscrowsController.CreateTaskEscrowRequest(
            "task-1",
            "share-1",
            100,
            "unverified-transaction"));

        var response = result.Should().BeOfType<ObjectResult>().Subject;
        response.StatusCode.Should().Be(StatusCodes.Status501NotImplemented);
        escrows.Verify(service => service.CreateAsync(
            It.IsAny<CreateTaskEscrowDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TaskEscrowDispute_UsesActorAndPreservesSubmittedContext()
    {
        var escrows = new Mock<ITaskEscrowService>();
        escrows.Setup(service => service.GetByIdAsync("escrow-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskEscrowDto>.Success(new TaskEscrowDto { Id = "escrow-1", FunderId = "actor-1" }));
        escrows.Setup(service => service.DisputeAsync(
                "escrow-1",
                It.Is<DisputeEscrowDto>(dto =>
                    dto.Reason == "QUALITY_ISSUE" &&
                    dto.Description == "The delivered work does not meet the documented acceptance criteria." &&
                    dto.DisputedByUserId == "actor-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskEscrowDto>.Success(new TaskEscrowDto
            {
                Id = "escrow-1",
                FunderId = "actor-1",
                Status = EscrowStatus.DISPUTED
            }));
        var controller = WithActor(new TaskEscrowsController(escrows.Object), "actor-1");

        var result = await controller.Dispute(
            "escrow-1",
            new TaskEscrowsController.DisputeEscrowRequest(
                "QUALITY_ISSUE",
                "The delivered work does not meet the documented acceptance criteria."),
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        escrows.VerifyAll();
    }

    [Fact]
    public async Task BidCreate_UsesActorInsteadOfBidderIdFromRequest()
    {
        var bids = new Mock<IOpportunityBidService>();
        bids.Setup(service => service.CreateAsync(
                It.Is<CreateOpportunityBidDto>(dto => dto.BidderId == "actor-1" && dto.OpportunityId == "opportunity-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpportunityBidDto>.Success(new OpportunityBidDto { Id = "bid-1", BidderId = "actor-1", OpportunityId = "opportunity-1", Proposal = "I can help" }));
        var controller = WithActor(
            new OpportunityBidsController(
                bids.Object,
                new Mock<IOpportunityService>(MockBehavior.Strict).Object,
                new Mock<IProjectService>(MockBehavior.Strict).Object,
                new Mock<ITaskCommerceService>(MockBehavior.Strict).Object),
            "actor-1");

        var result = await controller.Create(
            new OpportunityBidsController.CreateOpportunityBidRequest("opportunity-1", null, null, "I can help", null, null, null),
            CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        bids.VerifyAll();
    }

    [Fact]
    public async Task BidAccept_UsesActorDerivedIdentityForCommerceTransition()
    {
        var commerce = new Mock<ITaskCommerceService>();
        commerce.Setup(service => service.AcceptBidAsync("bid-1", "owner-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskCommerceAcceptanceDto>.Success(new TaskCommerceAcceptanceDto
            {
                BidId = "bid-1",
                TaskId = "task-1",
                AgreementId = "agreement-1",
                CommerceUrl = "/tasks/task-1/commerce",
            }));
        var controller = WithActor(
            new OpportunityBidsController(
                new Mock<IOpportunityBidService>(MockBehavior.Strict).Object,
                new Mock<IOpportunityService>(MockBehavior.Strict).Object,
                new Mock<IProjectService>(MockBehavior.Strict).Object,
                commerce.Object),
            "owner-1");

        var result = await controller.Accept("bid-1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        commerce.VerifyAll();
    }

    [Fact]
    public async Task BidAccept_MapsCompetingAssignmentToConflict()
    {
        var commerce = new Mock<ITaskCommerceService>();
        commerce.Setup(service => service.AcceptBidAsync("bid-1", "owner-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskCommerceAcceptanceDto>.Conflict("Already assigned"));
        var controller = WithActor(
            new OpportunityBidsController(
                new Mock<IOpportunityBidService>(MockBehavior.Strict).Object,
                new Mock<IOpportunityService>(MockBehavior.Strict).Object,
                new Mock<IProjectService>(MockBehavior.Strict).Object,
                commerce.Object),
            "owner-1");

        var result = await controller.Accept("bid-1", CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
        commerce.VerifyAll();
    }

    private static T WithActor<T>(T controller, string actorId)
        where T : ControllerBase
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, actorId)],
                "ActorAssertion"))
        };
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }
}
