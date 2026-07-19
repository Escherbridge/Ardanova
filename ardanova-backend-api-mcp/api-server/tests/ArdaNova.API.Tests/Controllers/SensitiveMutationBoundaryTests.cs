namespace ArdaNova.API.Tests.Controllers;

using ArdaNova.API.Controllers;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public sealed class SensitiveMutationBoundaryTests
{
    [Fact]
    public void ProjectTokenDistribution_FailsClosedBeforeServiceInvocation()
    {
        var tokens = new Mock<IProjectTokenService>(MockBehavior.Strict);
        var controller = new ProjectTokensController(
            tokens.Object,
            new Mock<IProjectGateService>(MockBehavior.Strict).Object);

        var result = controller.Distribute("allocation-1", "recipient-1", CancellationToken.None);

        AssertPaused(result, "Atomic RESERVED-to-DISTRIBUTED");
        tokens.VerifyNoOtherCalls();
    }

    [Fact]
    public void CredentialUtilityMutations_FailClosedBeforeServiceInvocation()
    {
        var credentials = new Mock<ICredentialUtilityService>(MockBehavior.Strict);
        var controller = new CredentialUtilityController(credentials.Object);

        var results = new IActionResult[]
        {
            controller.GrantAndMint(null!, CancellationToken.None),
            controller.RevokeAndBurn("credential-1", CancellationToken.None),
            controller.UpgradeTier("credential-1", null!, CancellationToken.None),
            controller.CheckAutoGrant(null!, CancellationToken.None),
            controller.RetryMint("credential-1", CancellationToken.None),
        };

        results.Should().AllSatisfy(result => AssertPaused(result, "Actor-bound scope"));
        credentials.VerifyNoOtherCalls();
    }

    [Fact]
    public void MembershipCredentialMutations_FailClosedBeforeServiceInvocation()
    {
        var credentials = new Mock<IMembershipCredentialService>(MockBehavior.Strict);
        var controller = new MembershipCredentialsController(credentials.Object);

        var results = new IActionResult[]
        {
            controller.Grant(null!, CancellationToken.None),
            controller.Revoke("credential-1", null, CancellationToken.None),
            controller.Suspend("credential-1", CancellationToken.None),
            controller.Reactivate("credential-1", CancellationToken.None),
            controller.UpdateMintInfo("credential-1", null!, CancellationToken.None),
            controller.UpdateTier("credential-1", null!, CancellationToken.None),
        };

        results.Should().AllSatisfy(result => AssertPaused(result, "server-derived grant authority"));
        credentials.VerifyNoOtherCalls();
    }

    [Fact]
    public void GovernanceMutations_FailClosedBeforeServiceInvocation()
    {
        var governance = new Mock<IGovernanceService>(MockBehavior.Strict);
        var controller = new GovernanceController(governance.Object);

        var results = new IActionResult[]
        {
            controller.CreateProposal(null!, CancellationToken.None),
            controller.UpdateProposal("proposal-1", null!, CancellationToken.None),
            controller.DeleteProposal("proposal-1", CancellationToken.None),
            controller.CastVote("proposal-1", null!, CancellationToken.None),
            controller.ExecuteProposal("proposal-1", CancellationToken.None),
            controller.CancelProposal("proposal-1", CancellationToken.None),
            controller.PublishProposal("proposal-1", CancellationToken.None),
            controller.CreateProposalComment("proposal-1", null!, CancellationToken.None),
            controller.UpdateProposalComment("proposal-1", "comment-1", "caller-selected-user", null!, CancellationToken.None),
            controller.DeleteProposalComment("proposal-1", "comment-1", "caller-selected-user", CancellationToken.None),
        };

        results.Should().AllSatisfy(result => AssertPaused(result, "Actor-bound proposal"));
        governance.VerifyNoOtherCalls();
    }

    [Fact]
    public void DelegatedVoteMutations_FailClosedBeforeServiceInvocation()
    {
        var delegations = new Mock<IDelegatedVoteService>(MockBehavior.Strict);
        var controller = new DelegatedVotesController(delegations.Object);

        var results = new IActionResult[]
        {
            controller.Create(null!, CancellationToken.None),
            controller.Update("delegation-1", null!, CancellationToken.None),
            controller.Revoke("delegation-1", CancellationToken.None),
        };

        results.Should().AllSatisfy(result => AssertPaused(result, "authenticated delegator"));
        delegations.VerifyNoOtherCalls();
    }

    [Fact]
    public void ReferralRewardClaim_FailsClosedBeforeServiceInvocation()
    {
        var referrals = new Mock<IReferralService>(MockBehavior.Strict);
        var controller = new ReferralsController(referrals.Object);

        var result = controller.ClaimReward("referral-1", null!, CancellationToken.None);

        AssertPaused(result, "server-derived");
        referrals.VerifyNoOtherCalls();
    }

    [Fact]
    public void LegacyAzoaCustodyMutations_ReturnGoneBeforeServiceInvocation()
    {
        var avatars = new Mock<IAzoaAvatarService>(MockBehavior.Strict);
        var controller = new AzoaAvatarController(avatars.Object);

        var results = new IActionResult[]
        {
            controller.EnsureAvatar(CancellationToken.None),
            controller.EnsureWallet(CancellationToken.None),
        };

        results.Should().AllSatisfy(result =>
        {
            var response = result.Should().BeOfType<ObjectResult>().Subject;
            response.StatusCode.Should().Be(StatusCodes.Status410Gone);
            response.Value.Should().BeOfType<ProblemDetails>()
                .Which.Detail.Should().Contain("/api/azoa/custodial-account");
        });
        avatars.VerifyNoOtherCalls();
    }

    private static void AssertPaused(IActionResult result, string expectedDetail)
    {
        var response = result.Should().BeOfType<ObjectResult>().Subject;
        response.StatusCode.Should().Be(StatusCodes.Status501NotImplemented);
        response.Value.Should().BeOfType<ProblemDetails>()
            .Which.Detail.Should().Contain(expectedDetail);
    }
}
