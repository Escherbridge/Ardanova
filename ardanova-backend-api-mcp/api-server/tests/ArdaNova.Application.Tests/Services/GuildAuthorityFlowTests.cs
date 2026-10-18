namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Mappings;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Tests.Support;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using FluentAssertions;

/// <summary>Guild flows driven through the services the controller calls, asserting on stored rows. Who acts is always the signed actor.</summary>
public class GuildAuthorityFlowTests
{
    private readonly InMemoryRepository<Guild> _guilds = new();
    private readonly InMemoryRepository<GuildMember> _members = new();
    private readonly InMemoryRepository<GuildInvitation> _invitations = new();
    private readonly InMemoryRepository<GuildApplication> _applications = new();
    private readonly InMemoryRepository<GuildReview> _reviews = new();
    private readonly InMemoryRepository<ProjectTask> _tasks = new();
    private readonly InMemoryRepository<User> _users = new();
    private readonly InMemoryUnitOfWork _unitOfWork = new();

    private readonly GuildService _guildService;
    private readonly GuildMemberService _memberService;
    private readonly GuildInvitationService _invitationService;
    private readonly GuildApplicationService _applicationService;
    private readonly GuildReviewService _reviewService;

    private static readonly GuildActor Alice = new("alice");
    private static readonly GuildActor Bob = new("bob");
    private static readonly GuildActor Carol = new("carol");
    private static readonly GuildActor Mallory = new("mallory");

    public GuildAuthorityFlowTests()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        _guildService = new GuildService(_guilds, _members, _tasks, _unitOfWork, mapper);
        _memberService = new GuildMemberService(_members, _guilds, _users, _unitOfWork);
        _invitationService = new GuildInvitationService(_invitations, _members, _guilds, _users, _unitOfWork);
        _applicationService = new GuildApplicationService(_applications, _members, _guilds, _unitOfWork);
        _reviewService = new GuildReviewService(_reviews, _guilds, _members, _unitOfWork);

        foreach (var id in new[] { "alice", "bob", "carol", "mallory" })
            _users.Rows.Add(new User { id = id, email = $"{id}@example.test", name = char.ToUpper(id[0]) + id[1..] });
    }

    private async Task<GuildDto> Found(GuildActor owner, string name)
    {
        var result = await _guildService.CreateAsync(owner, new CreateGuildDto
        {
            Name = name,
            Description = "We build things together.",
            Email = "hello@guild.test",
            Specialties = "carpentry,plumbing"
        });
        result.IsSuccess.Should().BeTrue(result.Error);
        return result.Value!;
    }

    [Fact]
    public async Task Founding_a_guild_makes_the_signed_actor_its_owner_member_and_keeps_the_specialties()
    {
        var guild = await Found(Alice, "Roofers");

        var members = await _memberService.GetByGuildIdAsync(guild.Id);
        members.Value!.Should().ContainSingle()
            .Which.Should().Match<GuildMemberDto>(m => m.UserId == "alice" && m.Role == GuildMemberRole.OWNER && m.User!.Name == "Alice");
        _guilds.Rows.Single().specialties.Should().Be("carpentry,plumbing");

        var second = await _guildService.CreateAsync(Alice, new CreateGuildDto { Name = "Again", Description = "d", Email = "a@b.c" });
        second.Type.Should().Be(ResultType.Conflict);
        _guilds.Rows.Should().HaveCount(1);
    }

    [Fact]
    public async Task An_owner_cannot_change_or_remove_a_member_of_someone_elses_guild_through_their_own_guild_route()
    {
        var aliceGuild = await Found(Alice, "Alice's guild");
        var bobGuild = await Found(Bob, "Bob's guild");
        var carolInBob = await AddMember(bobGuild.Id, Bob, "carol", GuildMemberRole.MEMBER);

        var promote = await _memberService.UpdateAsync(aliceGuild.Id, carolInBob.Id, Alice, new UpdateGuildMemberDto { Role = GuildMemberRole.ADMIN });
        var remove = await _memberService.DeleteAsync(aliceGuild.Id, carolInBob.Id, Alice);

        promote.Type.Should().Be(ResultType.NotFound);
        remove.Type.Should().Be(ResultType.NotFound);
        _members.Rows.Should().ContainSingle(m => m.id == carolInBob.Id && m.role == GuildMemberRole.MEMBER);
    }

    [Fact]
    public async Task Nobody_can_be_made_owner_and_an_admin_cannot_appoint_admins()
    {
        var guild = await Found(Alice, "Roofers");
        await AddMember(guild.Id, Alice, "bob", GuildMemberRole.ADMIN);
        var carol = await AddMember(guild.Id, Alice, "carol", GuildMemberRole.MEMBER);
        var ownerRow = _members.Rows.Single(m => m.userId == "alice");

        var toOwner = await _memberService.UpdateAsync(guild.Id, carol.Id, Alice, new UpdateGuildMemberDto { Role = GuildMemberRole.OWNER });
        var adminPromotes = await _memberService.UpdateAsync(guild.Id, carol.Id, Bob, new UpdateGuildMemberDto { Role = GuildMemberRole.ADMIN });
        var adminDemotesOwner = await _memberService.UpdateAsync(guild.Id, ownerRow.id, Bob, new UpdateGuildMemberDto { Role = GuildMemberRole.MEMBER });
        var adminRemovesOwner = await _memberService.DeleteAsync(guild.Id, ownerRow.id, Bob);
        var adminSetsManager = await _memberService.UpdateAsync(guild.Id, carol.Id, Bob, new UpdateGuildMemberDto { Role = GuildMemberRole.MANAGER });

        toOwner.Type.Should().Be(ResultType.ValidationError);
        adminPromotes.Type.Should().Be(ResultType.Forbidden);
        adminDemotesOwner.IsSuccess.Should().BeFalse();
        adminRemovesOwner.IsSuccess.Should().BeFalse();
        adminSetsManager.IsSuccess.Should().BeTrue();
        _members.Rows.Single(m => m.userId == "alice").role.Should().Be(GuildMemberRole.OWNER);
        _members.Rows.Single(m => m.userId == "carol").role.Should().Be(GuildMemberRole.MANAGER);
    }

    [Fact]
    public async Task An_outsider_cannot_edit_a_guild_profile()
    {
        var guild = await Found(Alice, "Roofers");

        var result = await _guildService.UpdateAsync(guild.Id, Mallory, new UpdateGuildDto { Name = "Hijacked" });

        result.Type.Should().Be(ResultType.Forbidden);
        _guilds.Rows.Single().name.Should().Be("Roofers");
    }

    [Fact]
    public async Task An_email_invitation_reaches_the_account_owner_who_alone_can_accept_it_once()
    {
        var guild = await Found(Alice, "Roofers");

        var invite = await _invitationService.CreateAsync(guild.Id, Alice, new CreateGuildInvitationDto { InvitedEmail = "  Carol@Example.TEST ", Role = "MANAGER" });
        invite.IsSuccess.Should().BeTrue(invite.Error);

        var inbox = await _invitationService.GetPendingForUserAsync("carol");
        inbox.Value!.Should().ContainSingle().Which.GuildName.Should().Be("Roofers");
        _members.Rows.Should().NotContain(m => m.userId == "carol", "an invitee is not a member until they accept");

        var stolen = await _invitationService.AcceptAsync(guild.Id, invite.Value!.Id, Mallory);
        var accepted = await _invitationService.AcceptAsync(guild.Id, invite.Value!.Id, Carol);
        var replayed = await _invitationService.AcceptAsync(guild.Id, invite.Value!.Id, Carol);

        stolen.Type.Should().Be(ResultType.Forbidden);
        accepted.IsSuccess.Should().BeTrue(accepted.Error);
        replayed.IsSuccess.Should().BeFalse();
        _members.Rows.Where(m => m.userId == "carol").Should().ContainSingle()
            .Which.role.Should().Be(GuildMemberRole.MANAGER);
        _guilds.Rows.Single().membersCount.Should().Be(2);
        (await _invitationService.GetPendingForUserAsync("carol")).Value.Should().BeEmpty();
    }

    [Fact]
    public async Task An_invitation_by_account_id_never_stores_or_shows_managers_the_invitees_email()
    {
        var guild = await Found(Alice, "Roofers");

        var invite = await _invitationService.CreateAsync(guild.Id, Alice, new CreateGuildInvitationDto { InvitedUserId = "carol", Role = "MEMBER" });
        var managerView = await _invitationService.GetByGuildIdAsync(guild.Id, Alice);

        invite.IsSuccess.Should().BeTrue(invite.Error);
        invite.Value!.InvitedEmail.Should().BeNull();
        managerView.Value!.Should().ContainSingle().Which.InvitedEmail.Should().BeNull();
        _invitations.Rows.Single().invitedEmail.Should().BeNull();
        (await _invitationService.GetPendingForUserAsync("carol")).Value.Should().ContainSingle();
    }

    [Theory]
    [InlineData("nobody@example.test", "MEMBER", ResultType.ValidationError)]
    [InlineData("carol@example.test", "OWNER", ResultType.ValidationError)]
    [InlineData("carol@example.test", "NOT_A_ROLE", ResultType.ValidationError)]
    public async Task Invitations_that_could_never_be_honoured_are_refused_and_not_stored(string email, string role, ResultType expected)
    {
        var guild = await Found(Alice, "Roofers");

        var result = await _invitationService.CreateAsync(guild.Id, Alice, new CreateGuildInvitationDto { InvitedEmail = email, Role = role });

        result.Type.Should().Be(expected);
        result.Error.Should().NotContain("Requested value", "enum parser text must not leak to callers");
        _invitations.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task An_application_becomes_a_membership_once_and_only_by_a_manager()
    {
        var guild = await Found(Alice, "Roofers");
        var applied = await _applicationService.CreateAsync(guild.Id, Carol, new CreateGuildApplicationDto { RequestedRole = "MEMBER", Message = "I would like to help with roofing jobs." });

        var selfApproved = await _applicationService.AcceptAsync(guild.Id, applied.Value!.Id, Carol, null);
        var approved = await _applicationService.AcceptAsync(guild.Id, applied.Value!.Id, Alice, "Welcome");
        var again = await _applicationService.AcceptAsync(guild.Id, applied.Value!.Id, Alice, "Welcome");

        selfApproved.Type.Should().Be(ResultType.Forbidden);
        approved.IsSuccess.Should().BeTrue(approved.Error);
        again.IsSuccess.Should().BeFalse();
        _members.Rows.Where(m => m.userId == "carol").Should().ContainSingle();
        _applications.Rows.Single().reviewedById.Should().Be("alice");
    }

    [Fact]
    public async Task A_review_is_one_per_outsider_and_moves_the_guild_rating()
    {
        var guild = await Found(Alice, "Roofers");

        var ownReview = await _reviewService.CreateAsync(guild.Id, Alice, new CreateGuildReviewDto { Rating = 5 });
        var first = await _reviewService.CreateAsync(guild.Id, Carol, new CreateGuildReviewDto { Rating = 4, Comment = "Solid work" });
        var second = await _reviewService.CreateAsync(guild.Id, Carol, new CreateGuildReviewDto { Rating = 1 });
        var othersDelete = await _reviewService.DeleteAsync(guild.Id, first.Value!.Id, Mallory);

        ownReview.Type.Should().Be(ResultType.Forbidden);
        first.IsSuccess.Should().BeTrue(first.Error);
        second.Type.Should().Be(ResultType.Conflict);
        othersDelete.Type.Should().Be(ResultType.Forbidden);
        _reviews.Rows.Should().ContainSingle(r => r.userId == "carol" && r.comment == "Solid work");
        _guilds.Rows.Single().rating.Should().Be(4m);
    }

    /// <summary>A guild as legacy rows and seed-local-demo.ts left it: an owner of record and no OWNER membership row.</summary>
    private Guild LegacyGuild(string ownerId)
    {
        var guild = new Guild { id = "legacy", name = "Neighborhood Infrastructure Guild", slug = "legacy", email = "g@x.test", ownerId = ownerId, projectsCount = 3 };
        _guilds.Rows.Add(guild);
        return guild;
    }

    [Fact]
    public async Task On_a_guild_with_no_owner_row_the_owner_still_counts_as_a_member_and_cannot_be_invited_admitted_or_added()
    {
        var guild = LegacyGuild("alice");
        _members.Rows.Add(new GuildMember { id = "admin-bob", guildId = guild.id, userId = "bob", role = GuildMemberRole.ADMIN, joinedAt = DateTime.UtcNow });

        var selfInviteByEmail = await _invitationService.CreateAsync(guild.id, Alice, new CreateGuildInvitationDto { InvitedEmail = "alice@example.test", Role = "MEMBER" });
        var adminInvitesOwner = await _invitationService.CreateAsync(guild.id, Bob, new CreateGuildInvitationDto { InvitedUserId = "alice", Role = "MEMBER" });
        var ownerApplies = await _applicationService.CreateAsync(guild.id, Alice, new CreateGuildApplicationDto { RequestedRole = "MEMBER", Message = "Let me in" });
        var ownerAdded = await _memberService.CreateAsync(guild.id, Alice, new CreateGuildMemberDto { UserId = "alice", Role = GuildMemberRole.MEMBER });

        selfInviteByEmail.Type.Should().Be(ResultType.Conflict);
        adminInvitesOwner.Type.Should().Be(ResultType.Conflict);
        ownerApplies.Type.Should().Be(ResultType.Conflict);
        ownerAdded.Type.Should().Be(ResultType.Conflict);
        _invitations.Rows.Should().BeEmpty();
        _applications.Rows.Should().BeEmpty();
        _members.Rows.Should().NotContain(m => m.userId == "alice", "the owner must not gain a second, lesser membership");
    }

    [Fact]
    public async Task A_membership_another_request_stores_first_is_reported_as_already_a_member_not_an_error()
    {
        _unitOfWork.Unique(_members, m => (m.guildId, m.userId));
        var guild = await Found(Alice, "Roofers");
        var invite = await _invitationService.CreateAsync(guild.Id, Alice, new CreateGuildInvitationDto { InvitedUserId = "carol", Role = "MEMBER" });

        // Carol's application is approved in another tab after this accept has checked and found no membership.
        _members.AfterExists = () =>
        {
            _members.AfterExists = null;
            _members.Rows.Add(new GuildMember { id = "concurrent", guildId = guild.Id, userId = "carol", role = GuildMemberRole.MEMBER, joinedAt = DateTime.UtcNow });
        };
        var accepted = await _invitationService.AcceptAsync(guild.Id, invite.Value!.Id, Carol);

        accepted.Type.Should().Be(ResultType.Conflict);
        _members.Rows.Where(m => m.userId == "carol").Should().ContainSingle().Which.id.Should().Be("concurrent");
    }

    [Fact]
    public async Task A_guilds_project_count_is_the_projects_assigned_to_it_not_the_stored_counter()
    {
        var projects = new InMemoryRepository<Project>();
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var guildService = new GuildService(_guilds, _members, _tasks, _unitOfWork, mapper, projects);
        var guild = LegacyGuild("alice");
        projects.Rows.Add(new Project { id = "p1", title = "Cool roofs", slug = "cool-roofs", assignedGuildId = guild.id });
        projects.Rows.Add(new Project { id = "p2", title = "Unrelated", slug = "unrelated" });

        var detail = await guildService.GetBySlugAsync("legacy");
        var directory = await guildService.GetAllAsync();

        detail.Value!.ProjectsCount.Should().Be(1, "the stored projectsCount of 3 is a number nothing maintains");
        directory.Value!.Should().ContainSingle().Which.ProjectsCount.Should().Be(1);
    }

    [Fact]
    public async Task A_second_review_another_request_stores_first_is_already_reviewed_and_the_rating_counts_it_once()
    {
        _unitOfWork.Unique(_reviews, r => (r.guildId, r.userId));
        var projects = new InMemoryRepository<Project>();
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var guildService = new GuildService(_guilds, _members, _tasks, _unitOfWork, mapper, projects, _reviews);
        var guild = await Found(Alice, "Roofers");

        // Carol's other tab posts a 2-star review after this request found none.
        _reviews.AfterExists = () =>
        {
            _reviews.AfterExists = null;
            _reviews.Rows.Add(new GuildReview { id = "concurrent", guildId = guild.Id, userId = "carol", rating = 2, createdAt = DateTime.UtcNow });
        };
        var raced = await _reviewService.CreateAsync(guild.Id, Carol, new CreateGuildReviewDto { Rating = 5 });
        var bobs = await _reviewService.CreateAsync(guild.Id, Bob, new CreateGuildReviewDto { Rating = 4 });

        raced.Type.Should().Be(ResultType.Conflict);
        _reviews.Rows.Where(r => r.userId == "carol").Should().ContainSingle().Which.id.Should().Be("concurrent");
        bobs.IsSuccess.Should().BeTrue(bobs.Error);

        // A stale stored aggregate is never what a reader sees.
        _guilds.Rows.Single().reviewsCount = 7;
        _guilds.Rows.Single().rating = 1m;
        var detail = await guildService.GetByIdAsync(guild.Id);
        detail.Value!.ReviewsCount.Should().Be(2);
        detail.Value.Rating.Should().Be(3m);
    }

    [Fact]
    public async Task A_review_can_only_name_a_project_of_the_reviewers_that_the_guild_was_assigned_to()
    {
        var projects = new InMemoryRepository<Project>();
        var reviewService = new GuildReviewService(_reviews, _guilds, _members, _unitOfWork, projects);
        var guild = await Found(Alice, "Roofers");
        projects.Rows.Add(new Project { id = "carols-roof", title = "Carol's roof", slug = "carols-roof", createdById = "carol", assignedGuildId = guild.Id });
        projects.Rows.Add(new Project { id = "bobs-shed", title = "Bob's shed", slug = "bobs-shed", createdById = "bob", assignedGuildId = guild.Id });
        projects.Rows.Add(new Project { id = "carols-other", title = "Unassigned", slug = "carols-other", createdById = "carol" });

        var someoneElsesProject = await reviewService.CreateAsync(guild.Id, Carol, new CreateGuildReviewDto { Rating = 1, ProjectId = "bobs-shed" });
        var unassignedProject = await reviewService.CreateAsync(guild.Id, Carol, new CreateGuildReviewDto { Rating = 1, ProjectId = "carols-other" });
        var genuine = await reviewService.CreateAsync(guild.Id, Carol, new CreateGuildReviewDto { Rating = 5, ProjectId = "carols-roof" });

        someoneElsesProject.Type.Should().Be(ResultType.ValidationError);
        unassignedProject.Type.Should().Be(ResultType.ValidationError);
        genuine.IsSuccess.Should().BeTrue(genuine.Error);
        _reviews.Rows.Should().ContainSingle().Which.projectId.Should().Be("carols-roof");
    }

    private async Task<GuildMemberDto> AddMember(string guildId, GuildActor by, string userId, GuildMemberRole role)
    {
        var result = await _memberService.CreateAsync(guildId, by, new CreateGuildMemberDto { UserId = userId, Role = role });
        result.IsSuccess.Should().BeTrue(result.Error);
        return result.Value!;
    }
}
