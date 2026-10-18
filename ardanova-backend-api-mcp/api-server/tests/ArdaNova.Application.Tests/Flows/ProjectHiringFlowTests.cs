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
    public async Task ATaskWhoseBidWasAccepted_CannotBeHandedToSomeoneElse()
    {
        _agreements.Add(new TaskCommerceAgreement { id = "agreement-1", taskId = "task-public" });

        var reassigned = await _taskService.AssignAsync("task-public", Stranger);

        reassigned.Type.Should().Be(ResultType.Conflict);
        _tasks.Items.Single(task => task.id == "task-public").assignedToId.Should().BeNull();
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
