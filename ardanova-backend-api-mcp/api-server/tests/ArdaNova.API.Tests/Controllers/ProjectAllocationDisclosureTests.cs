namespace ArdaNova.API.Tests.Controllers;

using System.Reflection;
using System.Security.Claims;
using ArdaNova.API.Controllers;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// What a project's allocations disclose, and to whom.
///
/// These two routes carried NO <c>[Authorize]</c> and no membership check at all, so an
/// anonymous caller read every worker's award size, the recipient's internal user id and the
/// settlement reference — the same facts the project ledger withholds from a signed-in
/// stranger. Fixing the ledger and leaving these open made that fix cosmetic.
/// </summary>
public sealed class ProjectAllocationDisclosureTests
{
    private const string ConfigId = "config-1";
    private const string ProjectId = "project-1";

    // ── The route is closed to anonymous callers at all ──────────────────────

    [Theory]
    [InlineData(nameof(ProjectTokensController.GetAllocations))]
    [InlineData(nameof(ProjectTokensController.GetAllocationsByPbi))]
    public void The_allocation_routes_require_a_verified_actor(string methodName)
    {
        var method = typeof(ProjectTokensController).GetMethod(methodName)!;
        var authorize = method.GetCustomAttribute<AuthorizeAttribute>();

        authorize.Should().NotBeNull(
            "an anonymous caller must not read who was awarded what");
        authorize!.Policy.Should().Be(AuthorizationPolicies.ActorAssertion);
    }

    // ── A non-member gets the aggregate, never the rows ──────────────────────

    [Fact]
    public async Task GetAllocations_answers_a_non_member_with_totals_and_no_recipient()
    {
        var tokens = ServiceReturning(Allocations());
        var controller = Controller(tokens, isMember: false);

        var result = await controller.GetAllocations(ConfigId, CancellationToken.None);

        var summary = result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<ProjectAllocationSummaryDto>().Subject;
        summary.AllocationCount.Should().Be(2);
        summary.TotalUnitsAllocated.Should().Be(300);
        summary.DistributedCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAllocations_answers_a_member_with_the_itemised_rows()
    {
        var tokens = ServiceReturning(Allocations());
        var controller = Controller(tokens, isMember: true);

        var result = await controller.GetAllocations(ConfigId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<IReadOnlyList<TokenAllocationDto>>()
            .Which.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllocationsByPbi_answers_a_non_member_with_totals_and_no_recipient()
    {
        var tokens = new Mock<IProjectTokenService>(MockBehavior.Strict);
        tokens
            .Setup(service => service.GetAllocationsByPbiAsync("pbi-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<TokenAllocationDto>>.Success(Allocations()));
        var controller = Controller(tokens, isMember: false);

        var result = await controller.GetAllocationsByPbi("pbi-1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<ProjectAllocationSummaryDto>();
    }

    /// <summary>
    /// The aggregate is the WHOLE payload a non-member receives, so nothing on it may name a
    /// person or a settlement. This asserts the shape itself, not one instance of it.
    /// </summary>
    [Fact]
    public void The_non_member_summary_carries_no_recipient_and_no_settlement_reference()
    {
        var fields = typeof(ProjectAllocationSummaryDto)
            .GetProperties()
            .Select(property => property.Name)
            .ToList();

        fields.Should().NotContain("RecipientUserId");
        fields.Should().NotContain("DistributionTxHash");
        fields.Should().NotContain("PbiId");
        fields.Should().BeEquivalentTo(
            "AllocationCount", "TotalUnitsAllocated", "TotalEquityPercentage", "DistributedCount");
    }

    // ── Wiring ───────────────────────────────────────────────────────────────

    private static IReadOnlyList<TokenAllocationDto> Allocations() =>
    [
        new()
        {
            Id = "allocation-1",
            ProjectTokenConfigId = ConfigId,
            PbiId = "pbi-1",
            RecipientUserId = "worker-1",
            EquityPercentage = 1.5,
            TokenAmount = 100,
            Status = AllocationStatus.DISTRIBUTED,
            HolderClass = TokenHolderClass.CONTRIBUTOR,
            IsLiquid = true,
            DistributedAt = DateTime.UtcNow,
            DistributionTxHash = "settlement-1",
        },
        new()
        {
            Id = "allocation-2",
            ProjectTokenConfigId = ConfigId,
            PbiId = "pbi-1",
            RecipientUserId = "worker-2",
            EquityPercentage = 2.5,
            TokenAmount = 200,
            Status = AllocationStatus.RESERVED,
            HolderClass = TokenHolderClass.CONTRIBUTOR,
            IsLiquid = false,
        },
    ];

    private static Mock<IProjectTokenService> ServiceReturning(IReadOnlyList<TokenAllocationDto> allocations)
    {
        var tokens = new Mock<IProjectTokenService>(MockBehavior.Strict);
        tokens
            .Setup(service => service.GetAllocationsByProjectAsync(ConfigId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<TokenAllocationDto>>.Success(allocations));
        return tokens;
    }

    private static ProjectTokensController Controller(Mock<IProjectTokenService> tokens, bool isMember)
    {
        var configs = new Mock<IRepository<ProjectTokenConfig>>();
        configs
            .Setup(repository => repository.GetByIdAsync(ConfigId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectTokenConfig { id = ConfigId, projectId = ProjectId });

        var hierarchy = new Mock<IHierarchyAuthorizationService>();
        hierarchy
            .Setup(service => service.IsProjectMemberAsync("actor-1", ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(isMember);

        var controller = new ProjectTokensController(
            tokens.Object,
            new Mock<IProjectGateService>(MockBehavior.Strict).Object,
            new Mock<IProjectTokenProvisioning>(MockBehavior.Strict).Object,
            configs.Object,
            new Mock<IRepository<TokenAllocation>>(MockBehavior.Strict).Object,
            new Mock<IRepository<ProjectInvestment>>(MockBehavior.Strict).Object,
            new Mock<IRepository<User>>(MockBehavior.Strict).Object,
            hierarchy.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "actor-1")],
                    ActorAssertionMiddleware.AuthenticationType)),
            },
        };
        return controller;
    }
}
