namespace ArdaNova.API.Tests.Controllers;

using System.Security.Claims;
using System.Reflection;
using System.Linq;
using ArdaNova.API.Controllers;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
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

    /// <summary>
    /// Selling is live now, so the boundary this test guards moved: the seller is the VERIFIED
    /// actor claim, never a body field, and the sale reaches the service instead of a 503.
    /// </summary>
    [Fact]
    public async Task PayoutRequest_SellsForTheActorNotACallerSuppliedIdentity()
    {
        var payouts = new Mock<IPayoutService>(MockBehavior.Strict);
        payouts.Setup(service => service.RequestPayoutAsync(
                "actor-1",
                It.Is<CreatePayoutRequestDto>(dto =>
                    dto.SourceProjectTokenConfigId == "config-1" && dto.SourceTokenAmount == 10),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PayoutRequestDto>.Success(new PayoutRequestDto
            {
                Id = "payout-1",
                UserId = "actor-1",
            }));
        var controller = WithActor(new PayoutsController(payouts.Object), "actor-1");

        var result = await controller.RequestPayout(new CreatePayoutRequestDto
        {
            SourceProjectTokenConfigId = "config-1",
            SourceTokenAmount = 10,
            HolderClass = TokenHolderClass.CONTRIBUTOR
        }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        payouts.VerifyAll();
    }

    /// <summary>
    /// Reconciling a parked sale is live now: an operator's call reaches the service, which
    /// re-reads the node under the key the sale was submitted with and moves nothing new.
    /// </summary>
    [Fact]
    public async Task PayoutProcessing_ReconcilesThroughTheService()
    {
        var payouts = new Mock<IPayoutService>(MockBehavior.Strict);
        payouts.Setup(service => service.ProcessPayoutAsync("payout-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PayoutRequestDto>.Success(new PayoutRequestDto { Id = "payout-1" }));
        var controller = new PayoutsController(payouts.Object);

        var result = await controller.ProcessPayout("payout-1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        payouts.VerifyAll();
    }

    /// <summary>
    /// Swapping is live now, so the boundary this test guards moved: the swapper is the VERIFIED
    /// actor claim, never a body field, and the swap reaches the service instead of a 503.
    /// </summary>
    [Fact]
    public async Task SwapExecution_SwapsForTheActorNotACallerSuppliedIdentity()
    {
        var swaps = new Mock<ISwapService>(MockBehavior.Strict);
        swaps.Setup(service => service.ExecuteSwapAsync(
                "actor-1",
                It.Is<SwapRequestDto>(dto =>
                    dto.SourceConfigId == "source-config" && dto.TargetConfigId == "target-config"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SwapResultDto>.Success(new SwapResultDto { Id = "swap-1" }));
        var controller = WithActor(new SwapsController(swaps.Object), "actor-1");

        var result = await controller.ExecuteSwap(new SwapRequestDto
        {
            SourceConfigId = "source-config",
            TargetConfigId = "target-config",
            SourceTokenAmount = 1,
        }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        swaps.VerifyAll();
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

    /// <summary>
    /// Release is live now, so the boundary this test guards moved: the funder never supplies
    /// release evidence, and the acting identity comes off the verified claim, never the body.
    /// </summary>
    [Fact]
    public async Task TaskEscrowRelease_UsesActorAndSuppliesNoCallerEvidence()
    {
        var escrows = new Mock<ITaskEscrowService>();
        escrows.Setup(service => service.GetByIdAsync("escrow-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskEscrowDto>.Success(new TaskEscrowDto { Id = "escrow-1", FunderId = "actor-1" }));
        escrows.Setup(service => service.ReleaseAsync(
                "escrow-1",
                It.Is<ReleaseEscrowDto>(dto => dto.ReleasedByUserId == "actor-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskEscrowDto>.Success(new TaskEscrowDto
            {
                Id = "escrow-1",
                FunderId = "actor-1",
                Status = EscrowStatus.RELEASED
            }));
        var controller = WithActor(new TaskEscrowsController(escrows.Object), "actor-1");

        var result = await controller.Release(
            "escrow-1",
            new TaskEscrowsController.ReleaseEscrowRequest(),
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        escrows.VerifyAll();
        typeof(TaskEscrowsController.ReleaseEscrowRequest).GetProperties().Should().BeEmpty(
            "release evidence is the hold's own confirmed state on the node, never a form field");
    }

    /// <summary>
    /// Refund keeps the funder's free-text audit note but derives the actor from the claim, and
    /// the request shape carries no settlement reference for a caller to forge.
    /// </summary>
    [Fact]
    public async Task TaskEscrowRefund_UsesActorAndPreservesOnlyTheAuditNote()
    {
        var escrows = new Mock<ITaskEscrowService>();
        escrows.Setup(service => service.GetByIdAsync("escrow-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskEscrowDto>.Success(new TaskEscrowDto { Id = "escrow-1", FunderId = "actor-1" }));
        escrows.Setup(service => service.RefundAsync(
                "escrow-1",
                It.Is<RefundEscrowDto>(dto =>
                    dto.RefundedByUserId == "actor-1" &&
                    dto.Reason == "The contributor withdrew before starting."),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskEscrowDto>.Success(new TaskEscrowDto
            {
                Id = "escrow-1",
                FunderId = "actor-1",
                Status = EscrowStatus.REFUNDED
            }));
        var controller = WithActor(new TaskEscrowsController(escrows.Object), "actor-1");

        var result = await controller.Refund(
            "escrow-1",
            new TaskEscrowsController.RefundEscrowRequest("The contributor withdrew before starting."),
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        escrows.VerifyAll();
        typeof(TaskEscrowsController.RefundEscrowRequest).GetProperties()
            .Select(property => property.Name)
            .Should().BeEquivalentTo(["Reason"]);
    }

    /// <summary>
    /// Escrow creation is live now, so the boundary this test guards moved: the request names the
    /// TASK and nothing else, the funder is the verified actor claim, and the amount and share of
    /// record are derived by the service - there is no body field a caller could inflate.
    /// </summary>
    [Fact]
    public async Task TaskEscrowCreate_UsesActorAndCarriesNoCallerSuppliedValue()
    {
        var escrows = new Mock<ITaskEscrowService>();
        escrows.Setup(service => service.CreateAsync(
                It.Is<CreateTaskEscrowDto>(dto =>
                    dto.TaskId == "task-1" &&
                    dto.FunderId == "actor-1" &&
                    dto.ShareId == "" &&
                    dto.Amount == 0m),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskEscrowDto>.Success(new TaskEscrowDto
            {
                Id = "escrow-1",
                TaskId = "task-1",
                FunderId = "actor-1",
                Status = EscrowStatus.NONE
            }));
        var controller = WithActor(new TaskEscrowsController(escrows.Object), "actor-1");

        var result = await controller.Create(
            new TaskEscrowsController.CreateTaskEscrowRequest("task-1"),
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        escrows.VerifyAll();
        typeof(TaskEscrowsController.CreateTaskEscrowRequest).GetProperties()
            .Select(property => property.Name)
            .Should().BeEquivalentTo(["TaskId"]);
    }

    /// <summary>
    /// The entry to FUNDED. It takes no body at all: the confirmation is the shared node's own
    /// record of the reservation, and the acting identity is the verified claim.
    /// </summary>
    [Fact]
    public async Task TaskEscrowFund_UsesActorAndSuppliesNoCallerEvidence()
    {
        var escrows = new Mock<ITaskEscrowService>();
        escrows.Setup(service => service.GetByIdAsync("escrow-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskEscrowDto>.Success(new TaskEscrowDto { Id = "escrow-1", FunderId = "actor-1" }));
        escrows.As<ITaskEscrowFunding>()
            .Setup(funding => funding.FundAsync("escrow-1", "actor-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskEscrowDto>.Success(new TaskEscrowDto
            {
                Id = "escrow-1",
                FunderId = "actor-1",
                Status = EscrowStatus.FUNDED
            }));
        var controller = WithActor(new TaskEscrowsController(escrows.Object), "actor-1");

        var result = await controller.Fund("escrow-1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        escrows.VerifyAll();
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
            new OpportunityBidsController.CreateOpportunityBidRequest("opportunity-1", null, null, null, "I can help", null, null, null),
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

    [Fact]
    public async Task BidAccept_ReturnsAccountSetupGuidanceWhenPaidAssignmentIsBlocked()
    {
        const string message = "Complete your account setup before you can accept paid project work.";
        var commerce = new Mock<ITaskCommerceService>();
        commerce.Setup(service => service.AcceptBidAsync("bid-1", "owner-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaskCommerceAcceptanceDto>.Forbidden(message));
        var controller = WithActor(
            new OpportunityBidsController(
                new Mock<IOpportunityBidService>(MockBehavior.Strict).Object,
                new Mock<IOpportunityService>(MockBehavior.Strict).Object,
                new Mock<IProjectService>(MockBehavior.Strict).Object,
                commerce.Object),
            "owner-1");

        var result = await controller.Accept("bid-1", CancellationToken.None);

        var response = result.Should().BeOfType<ObjectResult>().Subject;
        response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        response.Value.Should().BeEquivalentTo(new { error = message });
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
