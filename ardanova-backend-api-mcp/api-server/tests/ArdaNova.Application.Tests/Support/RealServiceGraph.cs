namespace ArdaNova.Application.Tests.Support;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Infrastructure.Data;
using ArdaNova.Infrastructure.Repositories;
using ArdaNova.Infrastructure.Tasks;
using AutoMapper;
using UnitOfWorkOverContext = ArdaNova.Infrastructure.UnitOfWork.UnitOfWork;

/// <summary>
/// The post, task and notification services wired over one real DbContext, with real collaborators, for flow tests
/// that build services by hand. One place to follow when these constructors change.
/// </summary>
public static class RealServiceGraph
{
    public static PostService Posts(ArdaNovaDbContext context, IMapper mapper, IUnitOfWork? unitOfWork = null) => new(
        new Repository<Post>(context),
        new Repository<PostLike>(context),
        new Repository<PostComment>(context),
        new Repository<PostBookmark>(context),
        new Repository<PostShare>(context),
        unitOfWork ?? new UnitOfWorkOverContext(context),
        mapper,
        new Repository<ProjectTask>(context),
        new Repository<Project>(context),
        Authorization(context, mapper),
        Notifications(context, mapper));

    public static TaskService Tasks(ArdaNovaDbContext context, IMapper mapper, IUnitOfWork? unitOfWork = null) => new(
        new Repository<ProjectTask>(context),
        new Repository<User>(context),
        new Repository<Project>(context),
        new Repository<TaskCommerceAgreement>(context),
        unitOfWork ?? new UnitOfWorkOverContext(context),
        mapper,
        new Repository<Post>(context),
        new TaskClaimStore(context),
        Notifications(context, mapper));

    public static NotificationService Notifications(ArdaNovaDbContext context, IMapper mapper) => new(
        new Repository<Notification>(context),
        new UnitOfWorkOverContext(context),
        mapper,
        new Repository<Post>(context),
        new Repository<ProjectTask>(context),
        new Repository<User>(context),
        new Repository<Project>(context));

    public static HierarchyAuthorizationService Authorization(ArdaNovaDbContext context, IMapper mapper)
    {
        var unitOfWork = new UnitOfWorkOverContext(context);
        return new HierarchyAuthorizationService(
            new ProjectService(new ProjectRepository(context), new Repository<User>(context), new ProjectDeletionBlockers(context), unitOfWork, mapper),
            new ProjectMemberService(new Repository<ProjectMember>(context), new Repository<User>(context), new Repository<ProjectTask>(context), unitOfWork, mapper),
            new ProjectMilestoneService(new Repository<ProjectMilestone>(context), unitOfWork, mapper),
            new EpicService(new Repository<Epic>(context), new Repository<ProjectMilestone>(context), unitOfWork, mapper),
            new SprintService(new Repository<Sprint>(context), new Repository<Epic>(context), unitOfWork, mapper),
            new FeatureService(new Repository<Feature>(context), new Repository<Sprint>(context), unitOfWork, mapper),
            new ProductBacklogItemService(new Repository<ProductBacklogItem>(context), new Repository<Feature>(context), unitOfWork, mapper),
            Tasks(context, mapper));
    }
}
