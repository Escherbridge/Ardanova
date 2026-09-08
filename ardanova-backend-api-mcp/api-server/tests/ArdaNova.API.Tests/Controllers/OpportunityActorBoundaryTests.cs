namespace ArdaNova.API.Tests.Controllers;

using System.Reflection;
using System.Security.Claims;
using ArdaNova.API.Controllers;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class OpportunityActorBoundaryTests
{
    public static TheoryData<string> ActorBoundRoutes =>
    [
        nameof(OpportunitiesController.Create),
        nameof(OpportunitiesController.Update),
        nameof(OpportunitiesController.Close),
        nameof(OpportunitiesController.Delete),
        nameof(OpportunitiesController.Apply),
        nameof(OpportunitiesController.GetApplications),
        nameof(OpportunitiesController.UpdateApplicationStatus),
        nameof(OpportunitiesController.CreateUpdate),
        nameof(OpportunitiesController.DeleteUpdate),
        nameof(OpportunitiesController.AddComment),
        nameof(OpportunitiesController.DeleteComment),
    ];

    [Theory]
    [MemberData(nameof(ActorBoundRoutes))]
    public void SensitiveOpportunityRoutes_RequireActorAssertion(string actionName)
    {
        var action = typeof(OpportunitiesController)
            .GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public)!;

        action.GetCustomAttribute<AuthorizeAttribute>()?.Policy
            .Should().Be(AuthorizationPolicies.ActorAssertion);
    }

    [Fact]
    public async Task Create_UsesSignedActorAsPosterAfterProjectRoleCheck()
    {
        var opportunities = new Mock<IOpportunityService>();
        opportunities.Setup(service => service.CreateAsync(
                It.Is<CreateOpportunityDto>(dto =>
                    dto.PosterId == "actor-1" && dto.ProjectId == "project-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpportunityDto>.Success(Opportunity("opportunity-1", "actor-1")));
        var hierarchy = new Mock<IHierarchyAuthorizationService>();
        hierarchy.Setup(service => service.CanManageProjectAsync(
                "actor-1", "project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = WithActor(CreateController(opportunities.Object, hierarchy.Object), "actor-1");

        var result = await controller.Create(
            new CreateOpportunityDto
            {
                PosterId = "other-user",
                ProjectId = "project-1",
                Title = "Research role",
                Description = "Help organize research for the project.",
                Type = OpportunityType.TASK_BOUNTY,
                ExperienceLevel = ExperienceLevel.MID,
            },
            CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        opportunities.VerifyAll();
        hierarchy.VerifyAll();
    }

    [Fact]
    public async Task Create_ForForeignProjectIsForbiddenBeforeMutation()
    {
        var opportunities = new Mock<IOpportunityService>(MockBehavior.Strict);
        var hierarchy = new Mock<IHierarchyAuthorizationService>();
        hierarchy.Setup(service => service.CanManageProjectAsync(
                "actor-1", "project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = WithActor(CreateController(opportunities.Object, hierarchy.Object), "actor-1");

        var result = await controller.Create(
            new CreateOpportunityDto
            {
                ProjectId = "project-1",
                Title = "Unauthorized role",
                Description = "This must not create a project opportunity.",
                Type = OpportunityType.TASK_BOUNTY,
                ExperienceLevel = ExperienceLevel.MID,
            },
            CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        opportunities.VerifyNoOtherCalls();
        hierarchy.VerifyAll();
    }

    [Fact]
    public async Task Create_ValueBearingListingRequiresAccountSetupBeforeMutation()
    {
        var opportunities = new Mock<IOpportunityService>(MockBehavior.Strict);
        var hierarchy = new Mock<IHierarchyAuthorizationService>();
        hierarchy.Setup(service => service.CanManageProjectAsync(
                "actor-1", "project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var eligibility = new Mock<IAzoaValueEligibilityService>();
        eligibility.Setup(service => service.RequireReadyAsync(
                "actor-1",
                "publish a paid or project-unit opportunity",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Forbidden(
                "Complete account setup before publishing paid opportunities."));
        var controller = WithActor(
            CreateController(opportunities.Object, hierarchy.Object, valueEligibility: eligibility.Object),
            "actor-1");

        var result = await controller.Create(
            new CreateOpportunityDto
            {
                ProjectId = "project-1",
                Title = "Paid research role",
                Description = "Help organize research for the project.",
                Type = OpportunityType.TASK_BOUNTY,
                ExperienceLevel = ExperienceLevel.MID,
                Compensation = 100m,
            },
            CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
        opportunities.VerifyNoOtherCalls();
        hierarchy.VerifyAll();
        eligibility.VerifyAll();
    }

    [Fact]
    public async Task Create_AllowsGuildManagersAndStillDerivesPoster()
    {
        var opportunities = new Mock<IOpportunityService>();
        opportunities.Setup(service => service.CreateAsync(
                It.Is<CreateOpportunityDto>(dto =>
                    dto.PosterId == "actor-1" && dto.GuildId == "guild-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpportunityDto>.Success(Opportunity("opportunity-1", "actor-1")));
        var guilds = new Mock<IGuildService>();
        guilds.Setup(service => service.GetByIdAsync("guild-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GuildDto>.Success(new GuildDto
            {
                Id = "guild-1",
                Name = "Guild",
                Slug = "guild",
                Description = "",
                Email = "guild@example.test",
                OwnerId = "other-user",
            }));
        var members = new Mock<IGuildMemberService>();
        members.Setup(service => service.GetByGuildIdAsync("guild-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<GuildMemberDto>>.Success(
            [new GuildMemberDto
            {
                Id = "membership-1",
                GuildId = "guild-1",
                UserId = "actor-1",
                Role = GuildMemberRole.MANAGER,
            }]));
        var controller = WithActor(
            CreateController(opportunities.Object, guilds: guilds.Object, guildMembers: members.Object),
            "actor-1");

        var result = await controller.Create(
            new CreateOpportunityDto
            {
                PosterId = "other-user",
                GuildId = "guild-1",
                Title = "Guild research role",
                Description = "Help organize research for the guild.",
                Type = OpportunityType.TASK_BOUNTY,
                ExperienceLevel = ExperienceLevel.MID,
            },
            CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        opportunities.VerifyAll();
        guilds.VerifyAll();
        members.VerifyAll();
    }

    [Fact]
    public async Task Apply_UsesSignedActorAsApplicant()
    {
        var opportunities = new Mock<IOpportunityService>();
        opportunities.Setup(service => service.GetByIdAsync("opportunity-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpportunityDto>.Success(Opportunity("opportunity-1", "other-user")));
        opportunities.Setup(service => service.ApplyAsync(
                "opportunity-1",
                It.Is<ApplyToOpportunityDto>(dto => dto.ApplicantId == "actor-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpportunityApplicationDto>.Success(new OpportunityApplicationDto
            {
                Id = "application-1",
                OpportunityId = "opportunity-1",
                ApplicantId = "actor-1",
                CoverLetter = "I can help with this work.",
            }));
        var controller = WithActor(CreateController(opportunities.Object), "actor-1");

        var result = await controller.Apply(
            "opportunity-1",
            new ApplyToOpportunityDto
            {
                ApplicantId = "other-user",
                CoverLetter = "I can help with this work.",
            },
            CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        opportunities.VerifyAll();
    }

    [Fact]
    public async Task GetApplications_ForeignPosterIsForbiddenBeforeRead()
    {
        var opportunities = new Mock<IOpportunityService>();
        opportunities.Setup(service => service.GetByIdAsync("opportunity-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpportunityDto>.Success(Opportunity("opportunity-1", "other-user")));
        var controller = WithActor(CreateController(opportunities.Object), "actor-1");

        var result = await controller.GetApplications("opportunity-1", CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        opportunities.Verify(service => service.GetApplicationsAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_HistoricalPosterWithoutCurrentProjectRoleIsForbiddenBeforeMutation()
    {
        var opportunities = new Mock<IOpportunityService>();
        opportunities.Setup(service => service.GetByIdAsync("opportunity-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpportunityDto>.Success(Opportunity(
                "opportunity-1",
                "actor-1",
                projectId: "project-1")));
        var hierarchy = new Mock<IHierarchyAuthorizationService>();
        hierarchy.Setup(service => service.CanManageProjectAsync(
                "actor-1", "project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = WithActor(CreateController(opportunities.Object, hierarchy.Object), "actor-1");

        var result = await controller.Update(
            "opportunity-1",
            new UpdateOpportunityDto { Title = "Unauthorized change" },
            CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        opportunities.Verify(service => service.UpdateAsync(
            It.IsAny<string>(), It.IsAny<UpdateOpportunityDto>(), It.IsAny<CancellationToken>()), Times.Never);
        hierarchy.VerifyAll();
    }

    [Fact]
    public async Task Update_CurrentProjectManagerCanManageAnOpportunityPostedByAnotherMember()
    {
        var opportunities = new Mock<IOpportunityService>();
        opportunities.Setup(service => service.GetByIdAsync("opportunity-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpportunityDto>.Success(Opportunity(
                "opportunity-1",
                "former-manager",
                projectId: "project-1")));
        opportunities.Setup(service => service.UpdateAsync(
                "opportunity-1",
                It.IsAny<UpdateOpportunityDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpportunityDto>.Success(Opportunity(
                "opportunity-1",
                "former-manager",
                projectId: "project-1")));
        var hierarchy = new Mock<IHierarchyAuthorizationService>();
        hierarchy.Setup(service => service.CanManageProjectAsync(
                "actor-1", "project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = WithActor(CreateController(opportunities.Object, hierarchy.Object), "actor-1");

        var result = await controller.Update(
            "opportunity-1",
            new UpdateOpportunityDto { Title = "Current manager change" },
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        opportunities.VerifyAll();
        hierarchy.VerifyAll();
    }

    [Fact]
    public async Task Update_ExistingValueBearingListingRequiresAccountSetupBeforeMutation()
    {
        var opportunities = new Mock<IOpportunityService>();
        opportunities.Setup(service => service.GetByIdAsync("opportunity-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpportunityDto>.Success(Opportunity(
                "opportunity-1",
                "manager-1",
                projectId: "project-1",
                equityPercent: 2.5m)));
        var hierarchy = new Mock<IHierarchyAuthorizationService>();
        hierarchy.Setup(service => service.CanManageProjectAsync(
                "actor-1", "project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var eligibility = new Mock<IAzoaValueEligibilityService>();
        eligibility.Setup(service => service.RequireReadyAsync(
                "actor-1",
                "update a paid or project-unit opportunity",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaCustodialAccountStatusDto>.Forbidden(
                "Complete account setup before updating paid opportunities."));
        var controller = WithActor(
            CreateController(opportunities.Object, hierarchy.Object, valueEligibility: eligibility.Object),
            "actor-1");

        var result = await controller.Update(
            "opportunity-1",
            new UpdateOpportunityDto { Title = "Clarified project-unit role" },
            CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
        opportunities.Verify(service => service.UpdateAsync(
            It.IsAny<string>(), It.IsAny<UpdateOpportunityDto>(), It.IsAny<CancellationToken>()), Times.Never);
        opportunities.VerifyAll();
        hierarchy.VerifyAll();
        eligibility.VerifyAll();
    }

    [Fact]
    public async Task UpdateApplicationStatus_ForeignPosterIsForbiddenBeforeMutation()
    {
        var opportunities = new Mock<IOpportunityService>();
        opportunities.Setup(service => service.GetApplicationByIdAsync("application-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpportunityApplicationDto>.Success(new OpportunityApplicationDto
            {
                Id = "application-1",
                OpportunityId = "opportunity-1",
                ApplicantId = "applicant-1",
                CoverLetter = "I can help with this work.",
            }));
        opportunities.Setup(service => service.GetByIdAsync("opportunity-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpportunityDto>.Success(Opportunity("opportunity-1", "other-user")));
        var controller = WithActor(CreateController(opportunities.Object), "actor-1");

        var result = await controller.UpdateApplicationStatus(
            "application-1",
            new UpdateApplicationStatusDto { Status = ApplicationStatus.ACCEPTED },
            CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        opportunities.Verify(service => service.UpdateApplicationStatusAsync(
            It.IsAny<string>(), It.IsAny<UpdateApplicationStatusDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateUpdate_UsesSignedActorAsAuthor()
    {
        var opportunities = new Mock<IOpportunityService>();
        opportunities.Setup(service => service.GetByIdAsync("opportunity-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpportunityDto>.Success(Opportunity(
                "opportunity-1",
                "actor-1",
                projectId: "project-1")));
        var hierarchy = new Mock<IHierarchyAuthorizationService>();
        hierarchy.Setup(service => service.CanManageProjectAsync(
                "actor-1", "project-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        opportunities.Setup(service => service.CreateUpdateAsync(
                It.Is<CreateOpportunityUpdateDto>(dto =>
                    dto.OpportunityId == "opportunity-1" && dto.UserId == "actor-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpportunityUpdateDto>.Success(new OpportunityUpdateDto
            {
                Id = "update-1",
                OpportunityId = "opportunity-1",
                UserId = "actor-1",
                Title = "Progress",
                Content = "Research is underway.",
            }));
        var controller = WithActor(CreateController(opportunities.Object, hierarchy.Object), "actor-1");

        var result = await controller.CreateUpdate(
            "opportunity-1",
            new CreateOpportunityUpdateDto
            {
                OpportunityId = "other-opportunity",
                UserId = "other-user",
                Title = "Progress",
                Content = "Research is underway.",
            },
            CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        opportunities.VerifyAll();
        hierarchy.VerifyAll();
    }

    [Fact]
    public async Task DeleteComment_AllowsTheSignedCommentAuthor()
    {
        var opportunities = new Mock<IOpportunityService>();
        opportunities.Setup(service => service.GetCommentByIdAsync("comment-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpportunityCommentDto>.Success(new OpportunityCommentDto
            {
                Id = "comment-1",
                OpportunityId = "opportunity-1",
                UserId = "actor-1",
                Content = "I can review this.",
            }));
        opportunities.Setup(service => service.DeleteCommentAsync("comment-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
        var controller = WithActor(CreateController(opportunities.Object), "actor-1");

        var result = await controller.DeleteComment("comment-1", CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        opportunities.VerifyAll();
    }

    [Fact]
    public async Task AddComment_UsesSignedActorAsAuthor()
    {
        var opportunities = new Mock<IOpportunityService>();
        opportunities.Setup(service => service.AddCommentAsync(
                It.Is<CreateOpportunityCommentDto>(dto =>
                    dto.OpportunityId == "opportunity-1" && dto.UserId == "actor-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpportunityCommentDto>.Success(new OpportunityCommentDto
            {
                Id = "comment-1",
                OpportunityId = "opportunity-1",
                UserId = "actor-1",
                Content = "I can review this.",
            }));
        var controller = WithActor(CreateController(opportunities.Object), "actor-1");

        var result = await controller.AddComment(
            "opportunity-1",
            new CreateOpportunityCommentDto
            {
                OpportunityId = "other-opportunity",
                UserId = "other-user",
                Content = "I can review this.",
            },
            CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        opportunities.VerifyAll();
    }

    [Fact]
    public async Task AddComment_RejectsAParentFromAnotherOpportunityBeforeMutation()
    {
        var opportunities = new Mock<IOpportunityService>();
        opportunities.Setup(service => service.GetCommentByIdAsync("parent-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OpportunityCommentDto>.Success(new OpportunityCommentDto
            {
                Id = "parent-1",
                OpportunityId = "other-opportunity",
                UserId = "other-user",
                Content = "A different discussion.",
            }));
        var controller = WithActor(CreateController(opportunities.Object), "actor-1");

        var result = await controller.AddComment(
            "opportunity-1",
            new CreateOpportunityCommentDto
            {
                ParentId = "parent-1",
                Content = "This must not cross opportunity threads.",
            },
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        opportunities.Verify(service => service.AddCommentAsync(
            It.IsAny<CreateOpportunityCommentDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static OpportunityDto Opportunity(
        string id,
        string posterId,
        string? projectId = null,
        string? guildId = null,
        decimal? compensation = null,
        decimal? equityPercent = null)
        => new()
        {
            Id = id,
            PosterId = posterId,
            ProjectId = projectId,
            GuildId = guildId,
            Compensation = compensation,
            EquityPercent = equityPercent,
            Title = "Opportunity",
            Slug = "opportunity",
            Description = "",
        };

    private static OpportunitiesController CreateController(
        IOpportunityService opportunities,
        IHierarchyAuthorizationService? hierarchy = null,
        IGuildService? guilds = null,
        IGuildMemberService? guildMembers = null,
        IAzoaValueEligibilityService? valueEligibility = null)
        => new(
            opportunities,
            hierarchy ?? new Mock<IHierarchyAuthorizationService>(MockBehavior.Strict).Object,
            guilds ?? new Mock<IGuildService>(MockBehavior.Strict).Object,
            guildMembers ?? new Mock<IGuildMemberService>(MockBehavior.Strict).Object,
            valueEligibility ?? new Mock<IAzoaValueEligibilityService>(MockBehavior.Strict).Object);

    private static T WithActor<T>(T controller, string actorId)
        where T : ControllerBase
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, actorId)],
                ActorAssertionMiddleware.AuthenticationType))
        };
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }
}
