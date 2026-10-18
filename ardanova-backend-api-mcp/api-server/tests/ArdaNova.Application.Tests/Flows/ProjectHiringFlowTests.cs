namespace ArdaNova.Application.Tests.Flows;

using System.Linq.Expressions;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Mappings;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using FluentAssertions;
using Moq;
using TaskStatus = ArdaNova.Domain.Models.Enums.TaskStatus;

/// <summary>
/// Plan work in private, hire for it, then hand it to the person hired. Real services over
/// list-backed repositories, so a read that stops composing the visibility predicate goes red.
/// </summary>
public class ProjectHiringFlowTests
{
    private const string Founder = "founder-1";
    private const string Applicant = "applicant-1";
    private const string Stranger = "stranger-1";
    private const string DraftProjectId = "project-draft";
    private const string PublishedProjectId = "project-published";

    private readonly ListRepository<Project> _projects = new();
    private readonly ListRepository<ProjectMember> _members = new();
    private readonly ListRepository<ProjectTask> _tasks = new();
    private readonly ListRepository<Opportunity> _opportunities = new();
    private readonly ListRepository<OpportunityApplication> _opportunityApplications = new();
    private readonly ListRepository<ProjectApplication> _projectApplications = new();
    private readonly ListRepository<TaskCommerceAgreement> _agreements = new();
    private readonly ListRepository<User> _users = new();
    private readonly IUnitOfWork _unitOfWork = new Mock<IUnitOfWork>().Object;
    private readonly IMapper _mapper;

    private readonly TaskService _taskService;
    private readonly OpportunityService _opportunityService;
    private readonly ProjectApplicationService _applicationService;

    public ProjectHiringFlowTests()
    {
        _mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
            MappingSecurityPolicy.Apply(cfg);
        }).CreateMapper();

        _projects.Add(NewProject(DraftProjectId, ProjectStatus.DRAFT));
        _projects.Add(NewProject(PublishedProjectId, ProjectStatus.PUBLISHED));
        _tasks.Add(NewTask("task-draft", DraftProjectId, "secret draft plan"));
        _tasks.Add(NewTask("task-public", PublishedProjectId, "public plan"));

        _taskService = new TaskService(_tasks, _users, _projects, _agreements, _unitOfWork, _mapper);
        _opportunityService = new OpportunityService(
            _opportunities,
            _opportunityApplications,
            new ListRepository<OpportunityUpdate>(),
            new ListRepository<OpportunityComment>(),
            _users,
            new ListRepository<Guild>(),
            _projects,
            _tasks,
            _members,
            _unitOfWork,
            _mapper);
        _applicationService = new ProjectApplicationService(
            _projectApplications, _projects, _members, _unitOfWork, _mapper);
    }

    [Fact]
    public async Task TaskSearch_KeepsADraftProjectsPlanPrivateToItsFounder()
    {
        var anonymous = await _taskService.SearchAsync(ProjectViewer.Anonymous, "plan", null, null, null, null, 1, 10);
        var stranger = await _taskService.SearchAsync(new ProjectViewer(Stranger, false), "secret draft", null, null, null, null, 1, 10);
        var founder = await _taskService.SearchAsync(new ProjectViewer(Founder, false), "plan", null, null, null, null, 1, 10);

        anonymous.Value!.Items.Select(task => task.Id).Should().Equal("task-public");
        anonymous.Value.TotalCount.Should().Be(1);
        stranger.Value!.Items.Should().BeEmpty();
        founder.Value!.Items.Select(task => task.Id).Should().BeEquivalentTo(new[] { "task-draft", "task-public" });
    }

    [Fact]
    public async Task OpportunityReads_HideDraftProjectsAndDraftRolesFromThePublic()
    {
        _opportunities.Add(NewOpportunity("role-on-draft", DraftProjectId, OpportunityStatus.OPEN));
        _opportunities.Add(NewOpportunity("draft-role", PublishedProjectId, OpportunityStatus.DRAFT));
        _opportunities.Add(NewOpportunity("open-role", PublishedProjectId, OpportunityStatus.OPEN));

        var anonymousList = await _opportunityService.GetAllAsync(ProjectViewer.Anonymous);
        var anonymousDetail = await _opportunityService.GetVisibleByIdAsync(ProjectViewer.Anonymous, "role-on-draft");
        var founderList = await _opportunityService.GetAllAsync(new ProjectViewer(Founder, false));

        anonymousList.Value!.Select(role => role.Id).Should().Equal("open-role");
        anonymousDetail.Type.Should().Be(ResultType.NotFound);
        founderList.Value!.Select(role => role.Id)
            .Should().BeEquivalentTo(new[] { "role-on-draft", "draft-role", "open-role" });
    }

    [Fact]
    public async Task AcceptingAnOpportunityApplication_SeatsTheApplicant_SoTheyCanBeAssignedWork()
    {
        _opportunities.Add(NewOpportunity("open-role", PublishedProjectId, OpportunityStatus.OPEN, ProjectRole.CORE_CONTRIBUTOR));
        var applied = await _opportunityService.ApplyAsync("open-role", new ApplyToOpportunityDto
        {
            ApplicantId = Applicant,
            CoverLetter = "I build cooling maps."
        });

        var accepted = await _opportunityService.UpdateApplicationStatusAsync(
            applied.Value!.Id,
            new UpdateApplicationStatusDto { Status = ApplicationStatus.ACCEPTED });
        var assigned = await _taskService.AssignAsync("task-public", Applicant);

        accepted.IsSuccess.Should().BeTrue();
        _members.Items.Should().ContainSingle(member =>
            member.projectId == PublishedProjectId &&
            member.userId == Applicant &&
            member.role == ProjectRole.CORE_CONTRIBUTOR);
        assigned.Value!.AssignedToId.Should().Be(Applicant);
    }

    [Fact]
    public async Task AnOpportunityApplicationIsDecidedOnce_AndWithdrawingIsNotTheReviewersCall()
    {
        _opportunities.Add(NewOpportunity("open-role", PublishedProjectId, OpportunityStatus.OPEN));
        var applied = await _opportunityService.ApplyAsync("open-role", new ApplyToOpportunityDto
        {
            ApplicantId = Applicant,
            CoverLetter = "Hello"
        });
        var applicationId = applied.Value!.Id;

        var withdrawnByReviewer = await _opportunityService.UpdateApplicationStatusAsync(
            applicationId, new UpdateApplicationStatusDto { Status = ApplicationStatus.WITHDRAWN });
        await _opportunityService.UpdateApplicationStatusAsync(
            applicationId, new UpdateApplicationStatusDto { Status = ApplicationStatus.REJECTED });
        var reopened = await _opportunityService.UpdateApplicationStatusAsync(
            applicationId, new UpdateApplicationStatusDto { Status = ApplicationStatus.ACCEPTED });

        withdrawnByReviewer.Error.Should().Be(OpportunityService.ReviewerMayOnlyAcceptOrReject);
        reopened.Error.Should().Be(OpportunityService.ApplicationNoLongerPending);
        _opportunityApplications.Items.Single().status.Should().Be(ApplicationStatus.REJECTED);
        _members.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ProjectApplication_RunsPendingToAcceptedOnce_AndAcceptanceMakesAMember()
    {
        var selfApply = await _applicationService.CreateAsync(NewProjectApplication(Founder));
        var first = await _applicationService.CreateAsync(NewProjectApplication(Applicant));
        var duplicate = await _applicationService.CreateAsync(NewProjectApplication(Applicant));

        var accepted = await _applicationService.AcceptAsync(first.Value!.Id, "Welcome aboard");
        var rejectedAfterAccept = await _applicationService.RejectAsync(first.Value.Id, null);
        var applyAgainAsMember = await _applicationService.CreateAsync(NewProjectApplication(Applicant));

        selfApply.Type.Should().Be(ResultType.ValidationError);
        duplicate.Type.Should().Be(ResultType.ValidationError);
        accepted.IsSuccess.Should().BeTrue();
        rejectedAfterAccept.Error.Should().Be(ProjectApplicationService.ApplicationNoLongerPending);
        applyAgainAsMember.Type.Should().Be(ResultType.ValidationError);
        _projectApplications.Items.Should().ContainSingle()
            .Which.status.Should().Be(ApplicationStatus.ACCEPTED);
        _members.Items.Should().ContainSingle(member =>
            member.projectId == PublishedProjectId && member.userId == Applicant);
    }

    [Fact]
    public async Task AcceptingAWithdrawnProjectApplication_IsRefused_AndSeatsNobody()
    {
        var applied = await _applicationService.CreateAsync(NewProjectApplication(Applicant));
        await _applicationService.WithdrawAsync(applied.Value!.Id);

        var accepted = await _applicationService.AcceptAsync(applied.Value.Id, null);

        accepted.Error.Should().Be(ProjectApplicationService.ApplicationNoLongerPending);
        _members.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ARoleSavedAsDraft_TakesApplicationsOnlyOnceItsTeamOpensIt()
    {
        var created = await _opportunityService.CreateAsync(new CreateOpportunityDto
        {
            PosterId = Founder,
            ProjectId = PublishedProjectId,
            Title = "Heat mapper",
            Description = "Map the hottest blocks.",
            Type = OpportunityType.PROJECT_ROLE,
            ExperienceLevel = ExperienceLevel.JUNIOR,
            IsOpenForApplications = false,
        });
        var roleId = created.Value!.Id;
        var whileDraft = await _opportunityService.ApplyAsync(roleId, new ApplyToOpportunityDto
        {
            ApplicantId = Applicant,
            CoverLetter = "I map heat islands for a living."
        });

        var opened = await _opportunityService.UpdateAsync(roleId, new UpdateOpportunityDto { Status = OpportunityStatus.OPEN });
        var publicView = await _opportunityService.GetVisibleByIdAsync(ProjectViewer.Anonymous, roleId);
        var onceOpen = await _opportunityService.ApplyAsync(roleId, new ApplyToOpportunityDto
        {
            ApplicantId = Applicant,
            CoverLetter = "I map heat islands for a living."
        });

        whileDraft.Type.Should().Be(ResultType.ValidationError);
        opened.Value!.IsOpenForApplications.Should().BeTrue();
        publicView.IsSuccess.Should().BeTrue();
        onceOpen.IsSuccess.Should().BeTrue();
        _opportunityApplications.Items.Should().ContainSingle(application => application.applicantId == Applicant);
    }

    /// <summary>Hiring is a second way to grant a role, so it obeys "below your own": no sock-puppet LEADER from a CORE_CONTRIBUTOR.</summary>
    [Fact]
    public async Task RoleOffers_FollowTheRankRule_ForPostersAndReviewersAlike()
    {
        const string Core = "core-1";
        _members.Add(new ProjectMember
        {
            id = "core-row",
            projectId = PublishedProjectId,
            userId = Core,
            role = ProjectRole.CORE_CONTRIBUTOR,
            joinedAt = DateTime.UtcNow
        });
        var core = new ProjectViewer(Core, false);
        var founder = new ProjectViewer(Founder, false);

        var refusals = new Dictionary<string, string?>
        {
            ["core offers LEADER"] = await _opportunityService.RefuseRoleOfferAsync(core, PublishedProjectId, ProjectRole.LEADER),
            ["core offers CORE_CONTRIBUTOR"] = await _opportunityService.RefuseRoleOfferAsync(core, PublishedProjectId, ProjectRole.CORE_CONTRIBUTOR),
            ["core offers CONTRIBUTOR"] = await _opportunityService.RefuseRoleOfferAsync(core, PublishedProjectId, ProjectRole.CONTRIBUTOR),
            ["core offers the default seat"] = await _opportunityService.RefuseRoleOfferAsync(core, PublishedProjectId, null),
            ["founder offers LEADER"] = await _opportunityService.RefuseRoleOfferAsync(founder, PublishedProjectId, ProjectRole.LEADER),
            ["founder offers FOUNDER"] = await _opportunityService.RefuseRoleOfferAsync(founder, PublishedProjectId, ProjectRole.FOUNDER),
            ["stranger offers CONTRIBUTOR"] = await _opportunityService.RefuseRoleOfferAsync(new ProjectViewer(Stranger, false), PublishedProjectId, ProjectRole.CONTRIBUTOR),
            ["admin offers LEADER"] = await _opportunityService.RefuseRoleOfferAsync(new ProjectViewer("admin-1", true), PublishedProjectId, ProjectRole.LEADER),
        };

        refusals.Should().Equal(new Dictionary<string, string?>
        {
            ["core offers LEADER"] = ProjectMemberRolePolicy.OutranksActor,
            ["core offers CORE_CONTRIBUTOR"] = ProjectMemberRolePolicy.OutranksActor,
            ["core offers CONTRIBUTOR"] = null,
            ["core offers the default seat"] = null,
            ["founder offers LEADER"] = null,
            ["founder offers FOUNDER"] = ProjectMemberRolePolicy.FounderIsNotGrantable,
            ["stranger offers CONTRIBUTOR"] = ProjectMemberRolePolicy.OutranksActor,
            ["admin offers LEADER"] = null,
        });
    }

    [Fact]
    public async Task ATeamPositionPostedWithoutARole_IsListedAsTheContributorSeat_AndSeatsOneOnAccept()
    {
        var created = await _opportunityService.CreateAsync(new CreateOpportunityDto
        {
            PosterId = Founder,
            ProjectId = PublishedProjectId,
            Title = "Street tree surveyor",
            Description = "Count the shade.",
            Type = OpportunityType.GUILD_POSITION,
            ExperienceLevel = ExperienceLevel.JUNIOR,
            IsOpenForApplications = true,
        });
        var applied = await _opportunityService.ApplyAsync(created.Value!.Id, new ApplyToOpportunityDto
        {
            ApplicantId = Applicant,
            CoverLetter = "I have surveyed trees before."
        });
        await _opportunityService.UpdateApplicationStatusAsync(
            applied.Value!.Id, new UpdateApplicationStatusDto { Status = ApplicationStatus.ACCEPTED });

        created.Value.ProjectRole.Should().Be(ProjectRole.CONTRIBUTOR);
        created.Value.Type.Should().Be(OpportunityType.PROJECT_ROLE);
        created.Value.Origin.Should().Be(OpportunityOrigin.TEAM_POSITION);
        _members.Items.Should().ContainSingle(member =>
            member.userId == Applicant && member.role == ProjectRole.CONTRIBUTOR);
    }

    [Fact]
    public async Task ABlankTaskTitle_IsRefused_OnCreateAndOnRename()
    {
        var created = await _taskService.CreateAsync(new CreateTaskDto { ProjectId = PublishedProjectId, Title = "   " });
        var renamed = await _taskService.UpdateAsync("task-public", new UpdateTaskDto { Title = "" });

        created.Error.Should().Be(TaskService.TaskTitleRequired);
        renamed.Error.Should().Be(TaskService.TaskTitleRequired);
        _tasks.Items.Select(task => task.title).Should().BeEquivalentTo(new[] { "secret draft plan", "public plan" });
    }

    [Fact]
    public async Task RemovingAMember_ReturnsTheirUnfinishedTasksToThePool()
    {
        _members.Add(new ProjectMember
        {
            id = "member-row",
            projectId = PublishedProjectId,
            userId = Applicant,
            role = ProjectRole.CONTRIBUTOR,
            joinedAt = DateTime.UtcNow
        });
        await _taskService.AssignAsync("task-public", Applicant);
        var finished = NewTask("task-finished", PublishedProjectId, "done plan");
        finished.status = TaskStatus.COMPLETED;
        finished.assignedToId = Applicant;
        _tasks.Add(finished);
        var memberService = new ProjectMemberService(_members, _users, _tasks, _unitOfWork, _mapper);

        var removed = await memberService.DeleteAsync("member-row");

        removed.IsSuccess.Should().BeTrue();
        _members.Items.Should().BeEmpty();
        _tasks.Items.Single(task => task.id == "task-public").assignedToId.Should().BeNull();
        _tasks.Items.Single(task => task.id == "task-finished").assignedToId.Should().Be(Applicant);
    }

    [Fact]
    public async Task ATaskWhoseBidWasAccepted_CannotBeHandedToSomeoneElse()
    {
        _agreements.Add(new TaskCommerceAgreement { id = "agreement-1", taskId = "task-public" });

        var reassigned = await _taskService.AssignAsync("task-public", Stranger);

        reassigned.Type.Should().Be(ResultType.Conflict);
        _tasks.Items.Single(task => task.id == "task-public").assignedToId.Should().BeNull();
    }

    /// <summary>Internal promotion: an existing member hired into a higher role holds that role afterwards, in one row.</summary>
    [Fact]
    public async Task AcceptingAMembersApplicationForAHigherRole_PromotesThem()
    {
        _members.Add(new ProjectMember
        {
            id = "member-row",
            projectId = PublishedProjectId,
            userId = Applicant,
            role = ProjectRole.CONTRIBUTOR,
            joinedAt = DateTime.UtcNow
        });
        _opportunities.Add(NewOpportunity("lead-role", PublishedProjectId, OpportunityStatus.OPEN, ProjectRole.LEADER));
        var applied = await _opportunityService.ApplyAsync("lead-role", new ApplyToOpportunityDto
        {
            ApplicantId = Applicant,
            CoverLetter = "I have run the survey crew for a season."
        });

        var accepted = await _opportunityService.UpdateApplicationStatusAsync(
            applied.Value!.Id, new UpdateApplicationStatusDto { Status = ApplicationStatus.ACCEPTED });

        accepted.Value!.Status.Should().Be(ApplicationStatus.ACCEPTED);
        _members.Items.Should().ContainSingle(member => member.userId == Applicant)
            .Which.role.Should().Be(ProjectRole.LEADER);
    }

    /// <summary>ACCEPTED always means the role was seated: a hire the applicant already outranks is refused, never a silent no-op or a demotion.</summary>
    [Fact]
    public async Task AHireTheApplicantAlreadyOutranks_IsRefused_OnApplyAndOnAccept()
    {
        _opportunities.Add(NewOpportunity("contributor-role", PublishedProjectId, OpportunityStatus.OPEN, ProjectRole.CONTRIBUTOR));
        _opportunities.Add(NewOpportunity("core-role", PublishedProjectId, OpportunityStatus.OPEN, ProjectRole.CORE_CONTRIBUTOR));
        var lowerApplication = await _opportunityService.ApplyAsync("contributor-role", new ApplyToOpportunityDto { ApplicantId = Applicant, CoverLetter = "Hi" });
        var higherApplication = await _opportunityService.ApplyAsync("core-role", new ApplyToOpportunityDto { ApplicantId = Applicant, CoverLetter = "Hi" });
        await _opportunityService.UpdateApplicationStatusAsync(
            higherApplication.Value!.Id, new UpdateApplicationStatusDto { Status = ApplicationStatus.ACCEPTED });

        var staleAccept = await _opportunityService.UpdateApplicationStatusAsync(
            lowerApplication.Value!.Id, new UpdateApplicationStatusDto { Status = ApplicationStatus.ACCEPTED });
        _opportunities.Add(NewOpportunity("another-contributor-role", PublishedProjectId, OpportunityStatus.OPEN, ProjectRole.CONTRIBUTOR));
        var lateApply = await _opportunityService.ApplyAsync("another-contributor-role", new ApplyToOpportunityDto { ApplicantId = Applicant, CoverLetter = "Hi" });
        var founderApply = await _opportunityService.ApplyAsync("core-role", new ApplyToOpportunityDto { ApplicantId = Founder, CoverLetter = "Hi" });

        staleAccept.Error.Should().Be(OpportunityService.ApplicantAlreadyHoldsRole);
        lateApply.Error.Should().Be(OpportunityService.ApplicantAlreadyHoldsRole);
        founderApply.Error.Should().Be(OpportunityService.ApplicantAlreadyHoldsRole);
        _opportunityApplications.Items.Single(application => application.id == lowerApplication.Value.Id)
            .status.Should().Be(ApplicationStatus.PENDING);
        _members.Items.Should().ContainSingle(member => member.userId == Applicant)
            .Which.role.Should().Be(ProjectRole.CORE_CONTRIBUTOR);
    }

    [Fact]
    public async Task ReopeningACompletedTask_ClearsItsCompletionDate()
    {
        var completed = await _taskService.UpdateStatusAsync("task-public", TaskStatus.COMPLETED);
        var reopened = await _taskService.UpdateStatusAsync("task-public", TaskStatus.IN_PROGRESS);
        var completedAgain = await _taskService.UpdateAsync("task-public", new UpdateTaskDto { Status = TaskStatus.COMPLETED });

        completed.Value!.CompletedAt.Should().NotBeNull();
        reopened.Value!.CompletedAt.Should().BeNull();
        completedAgain.Value!.CompletedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData("title too long", 201, null, null)]
    [InlineData("negative estimate", 10, -1, null)]
    [InlineData("negative actual", 10, null, -1)]
    public async Task UnusableTaskFields_AreRefused_AndTheTaskIsUnchanged(string because, int titleLength, int? estimatedHours, int? actualHours)
    {
        var expected = titleLength > TaskService.TaskTitleMaxLength ? TaskService.TaskTitleTooLong : TaskService.TaskHoursNegative;

        // Create takes no actual hours, so that case is an update-only refusal.
        var created = actualHours is null
            ? await _taskService.CreateAsync(new CreateTaskDto
            {
                ProjectId = PublishedProjectId,
                Title = new string('t', titleLength),
                EstimatedHours = estimatedHours,
            })
            : null;
        var updated = await _taskService.UpdateAsync("task-public", new UpdateTaskDto
        {
            Title = new string('t', titleLength),
            EstimatedHours = estimatedHours,
            ActualHours = actualHours,
        });

        created?.Error.Should().Be(expected, because);
        updated.Error.Should().Be(expected, because);
        _tasks.Items.Should().HaveCount(2);
        _tasks.Items.Single(task => task.id == "task-public").title.Should().Be("public plan");
    }

    /// <summary>The database refuses to orphan a project's plan; the service says so instead of surfacing a foreign-key error.</summary>
    [Fact]
    public async Task AProjectWithPlannedWork_CannotBeDeleted_ButAnEmptyOneCan()
    {
        var milestones = new ListRepository<ProjectMilestone>();
        _projects.Add(NewProject("project-empty", ProjectStatus.DRAFT));
        _projects.Add(NewProject("project-with-milestone", ProjectStatus.DRAFT));
        milestones.Add(new ProjectMilestone { id = "milestone-1", projectId = "project-with-milestone", title = "Survey" });
        var projectRepository = new Mock<IProjectRepository>();
        projectRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) => _projects.Items.FirstOrDefault(project => project.id == id));
        projectRepository.Setup(r => r.DeleteAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback((Project project, CancellationToken _) => _projects.Items.Remove(project))
            .Returns(Task.CompletedTask);
        var projectService = new ProjectService(projectRepository.Object, _users, _tasks, milestones, _unitOfWork, _mapper);

        var withTasks = await projectService.DeleteAsync(PublishedProjectId);
        var withMilestone = await projectService.DeleteAsync("project-with-milestone");
        var empty = await projectService.DeleteAsync("project-empty");

        withTasks.Type.Should().Be(ResultType.Conflict);
        withTasks.Error.Should().Be(ProjectService.ProjectHasPlannedWork);
        withMilestone.Type.Should().Be(ResultType.Conflict);
        empty.IsSuccess.Should().BeTrue();
        _projects.Items.Select(project => project.id)
            .Should().BeEquivalentTo(new[] { DraftProjectId, PublishedProjectId, "project-with-milestone" });
    }

    private static Project NewProject(string id, ProjectStatus status) => new()
    {
        id = id,
        title = id,
        slug = id,
        description = "Cooling the block",
        problemStatement = "Heat",
        solution = "Shade",
        categories = "ENVIRONMENT",
        status = status,
        createdById = Founder,
        createdAt = DateTime.UtcNow,
        updatedAt = DateTime.UtcNow
    };

    private static ProjectTask NewTask(string id, string projectId, string title) => new()
    {
        id = id,
        projectId = projectId,
        title = title,
        status = TaskStatus.TODO,
        priority = TaskPriority.MEDIUM,
        taskType = TaskType.FEATURE,
        escrowStatus = EscrowStatus.NONE,
        createdAt = DateTime.UtcNow,
        updatedAt = DateTime.UtcNow
    };

    private static Opportunity NewOpportunity(
        string id,
        string projectId,
        OpportunityStatus status,
        ProjectRole? projectRole = null) => new()
    {
        id = id,
        title = id,
        slug = id,
        description = "A role",
        type = OpportunityType.PROJECT_ROLE,
        status = status,
        experienceLevel = ExperienceLevel.JUNIOR,
        posterId = Founder,
        projectId = projectId,
        projectRole = projectRole,
        createdAt = DateTime.UtcNow,
        updatedAt = DateTime.UtcNow
    };

    private static CreateProjectApplicationDto NewProjectApplication(string userId) => new()
    {
        ProjectId = PublishedProjectId,
        UserId = userId,
        RoleTitle = "Mapper",
        Message = "I would like to help."
    };

    /// <summary>A list-backed repository: writes land immediately, reads run the real LINQ predicates.</summary>
    private sealed class ListRepository<T> : IRepository<T> where T : class
    {
        public List<T> Items { get; } = new();

        public void Add(T entity) => Items.Add(entity);

        private static string IdOf(T entity) =>
            (string)typeof(T).GetProperty("id")!.GetValue(entity)!;

        public Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(item => IdOf(item) == id));

        public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<T>>(Items.ToList());

        public Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<T>>(Items.AsQueryable().Where(predicate).ToList());

        public Task<IReadOnlyList<TResult>> FindProjectedAsync<TResult>(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, TResult>> selector,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TResult>>(Items.AsQueryable().Where(predicate).Select(selector).ToList());

        public Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.AsQueryable().FirstOrDefault(predicate));

        public Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            Items.Add(entity);
            return Task.FromResult(entity);
        }

        public Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            var added = entities.ToList();
            Items.AddRange(added);
            return Task.FromResult<IEnumerable<T>>(added);
        }

        public Task UpdateAsync(T entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            Items.Remove(entity);
            return Task.CompletedTask;
        }

        public Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            foreach (var entity in entities.ToList())
                Items.Remove(entity);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.AsQueryable().Any(predicate));

        public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(predicate is null ? Items.Count : Items.AsQueryable().Count(predicate));

        public Task<PagedResult<T>> GetPagedAsync(
            int page,
            int pageSize,
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken cancellationToken = default)
        {
            var matches = predicate is null ? Items.ToList() : Items.AsQueryable().Where(predicate).ToList();
            return Task.FromResult(new PagedResult<T>(
                matches.Skip((page - 1) * pageSize).Take(pageSize).ToList(), matches.Count, page, pageSize));
        }

        public IQueryable<T> Query() => Items.AsQueryable();
    }
}
