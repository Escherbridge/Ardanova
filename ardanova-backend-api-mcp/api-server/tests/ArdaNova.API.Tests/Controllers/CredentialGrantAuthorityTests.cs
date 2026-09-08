namespace ArdaNova.API.Tests.Controllers;

using System.Security.Claims;
using ArdaNova.API.Controllers;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Who may issue a membership credential, to whom, and on whose say-so.
/// </summary>
/// <remarks>
/// The scope-scoped grant carries NO request body, so every one of these assertions is about a
/// fact the server looked up for itself. The last two are the ones that matter most: the grant
/// reason recorded against the credential is derived from the recipient's standing, and a caller
/// has no way to influence it.
/// </remarks>
public sealed class CredentialGrantAuthorityTests
{
    private const string ProjectId = "project-1";
    private const string OwnerId = "owner-1";
    private const string MemberId = "member-1";
    private const string OutsiderId = "outsider-1";

    private readonly Mock<IMembershipCredentialService> _credentials = new(MockBehavior.Strict);
    private readonly Mock<ICredentialUtilityService> _utility = new(MockBehavior.Strict);
    private readonly Mock<IProjectService> _projects = new();
    private readonly Mock<IHierarchyAuthorizationService> _hierarchy = new();
    private readonly Mock<IGuildService> _guilds = new();
    private readonly Mock<IGuildMemberService> _guildMembers = new();

    public CredentialGrantAuthorityTests()
    {
        _projects.Setup(service => service.GetByIdAsync(ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectDto>.Success(new ProjectDto { Id = ProjectId, CreatedById = OwnerId }));
    }

    [Fact]
    public async Task GrantWithoutAuthorizationCollaborators_FailsClosed()
    {
        // The pre-DI shape: no authorizer, so the question cannot be asked and nothing proceeds.
        var controller = new MembershipCredentialsController(_credentials.Object);

        var result = await controller.GrantForProject(ProjectId, MemberId, CancellationToken.None);

        var response = result.Should().BeOfType<ObjectResult>().Subject;
        response.StatusCode.Should().Be(StatusCodes.Status501NotImplemented);
        _utility.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GrantBySomeoneWhoDoesNotManageTheProject_IsRefusedBeforeTheServiceIsCalled()
    {
        _hierarchy.Setup(auth => auth.CanManageProjectAsync(OutsiderId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = CreateController(actorId: OutsiderId);

        var result = await controller.GrantForProject(ProjectId, MemberId, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        _utility.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GrantToSomeoneWhoIsNotOnTheProject_IsRefusedBeforeTheServiceIsCalled()
    {
        _hierarchy.Setup(auth => auth.CanManageProjectAsync(OwnerId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _hierarchy.Setup(auth => auth.IsProjectMemberAsync(OutsiderId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = CreateController(actorId: OwnerId);

        var result = await controller.GrantForProject(ProjectId, OutsiderId, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        _utility.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GrantToTheProjectCreator_RecordsFounderAuthority()
    {
        _hierarchy.Setup(auth => auth.CanManageProjectAsync(OwnerId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var captured = CaptureGrant();
        var controller = CreateController(actorId: OwnerId);

        var result = await controller.GrantForProject(ProjectId, OwnerId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        captured().GrantedVia.Should().Be(nameof(MembershipGrantType.FOUNDER));
    }

    [Fact]
    public async Task GrantToAnExistingMember_RecordsApplicationApprovedAuthorityAndRouteScope()
    {
        _hierarchy.Setup(auth => auth.CanManageProjectAsync(OwnerId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _hierarchy.Setup(auth => auth.IsProjectMemberAsync(MemberId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var captured = CaptureGrant();
        var controller = CreateController(actorId: OwnerId);

        await controller.GrantForProject(ProjectId, MemberId, CancellationToken.None);

        var grant = captured();
        grant.GrantedVia.Should().Be(nameof(MembershipGrantType.APPLICATION_APPROVED));
        grant.ProjectId.Should().Be(ProjectId, "the scope comes from the route");
        grant.GuildId.Should().BeNull();
        grant.UserId.Should().Be(MemberId, "the recipient comes from the route");
        grant.GrantedByProposalId.Should().BeNull(
            "no proposal outcome can be quoted at a member until governance can produce one");
    }

    [Fact]
    public void TheBodyScopedGrantStaysRefused()
    {
        var controller = CreateController(actorId: OwnerId);

        var result = controller.Grant(null!, CancellationToken.None);

        var response = result.Should().BeOfType<ObjectResult>().Subject;
        response.StatusCode.Should().Be(StatusCodes.Status501NotImplemented);
        _utility.VerifyNoOtherCalls();
    }

    /// <summary>Records the DTO the controller builds, and echoes a credential back.</summary>
    private Func<GrantMembershipCredentialDto> CaptureGrant()
    {
        GrantMembershipCredentialDto? seen = null;
        _utility.Setup(service => service.GrantAndMintAsync(
                It.IsAny<GrantMembershipCredentialDto>(), It.IsAny<CancellationToken>()))
            .Callback((GrantMembershipCredentialDto dto, CancellationToken _) => seen = dto)
            .ReturnsAsync(() => Result<MembershipCredentialDto>.Success(new MembershipCredentialDto
            {
                Id = "credential-1",
                ProjectId = seen!.ProjectId,
                UserId = seen!.UserId,
                Status = nameof(MembershipCredentialStatus.ACTIVE),
                GrantedVia = seen!.GrantedVia
            }));

        return () =>
        {
            seen.Should().NotBeNull("the controller must reach the grant seam");
            return seen!;
        };
    }

    private MembershipCredentialsController CreateController(string actorId)
    {
        var controller = new MembershipCredentialsController(
            _credentials.Object,
            _utility.Object,
            _projects.Object,
            _hierarchy.Object,
            _guilds.Object,
            _guildMembers.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, actorId) },
                        authenticationType: "Test"))
                }
            }
        };

        return controller;
    }
}
