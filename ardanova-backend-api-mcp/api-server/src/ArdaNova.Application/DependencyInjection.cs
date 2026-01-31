namespace ArdaNova.Application;

using ArdaNova.Application.Mappings;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // AutoMapper
        services.AddAutoMapper(typeof(MappingProfile).Assembly);

        // User services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IUserSkillService, UserSkillService>();
        services.AddScoped<IUserExperienceService, UserExperienceService>();

        // Project services
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IProjectTaskService, ProjectTaskService>();
        services.AddScoped<IProjectTaskDependencyService, ProjectTaskDependencyService>();
        services.AddScoped<IProjectResourceService, ProjectResourceService>();
        services.AddScoped<IProjectMilestoneService, ProjectMilestoneService>();
        services.AddScoped<IProjectSupportService, ProjectSupportService>();
        services.AddScoped<IProjectApplicationService, ProjectApplicationService>();
        services.AddScoped<IProjectCommentService, ProjectCommentService>();
        services.AddScoped<IProjectUpdateService, ProjectUpdateService>();
        services.AddScoped<IProjectEquityService, ProjectEquityService>();
        services.AddScoped<IProjectMemberService, ProjectMemberService>();

        // Guild services 
        services.AddScoped<IGuildService, GuildService>();
        services.AddScoped<IGuildMemberService, GuildMemberService>();
        services.AddScoped<IProjectBidService, ProjectBidService>();
        services.AddScoped<IGuildReviewService, GuildReviewService>();
        services.AddScoped<IGuildUpdateService, GuildUpdateService>();
        services.AddScoped<IGuildApplicationService, GuildApplicationService>();
        services.AddScoped<IGuildInvitationService, GuildInvitationService>();
        services.AddScoped<IGuildFollowService, GuildFollowService>();

        // Shop services
        services.AddScoped<IShopService, ShopService>();
        services.AddScoped<IShopCustomerService, ShopCustomerService>();
        services.AddScoped<IShopProductService, ShopProductService>();
        services.AddScoped<IShopAnalyticsService, ShopAnalyticsService>();
        services.AddScoped<IShopInvoiceService, ShopInvoiceService>();
        services.AddScoped<IShopSaleService, ShopSaleService>();
        services.AddScoped<IShopSaleItemService, ShopSaleItemService>();
        services.AddScoped<IShopInventoryItemService, ShopInventoryItemService>();
        services.AddScoped<IShopMarketingCampaignService, ShopMarketingCampaignService>();

        // Roadmap services
        services.AddScoped<IRoadmapService, RoadmapService>();
        services.AddScoped<IRoadmapPhaseService, RoadmapPhaseService>();

        // Sprint services
        services.AddScoped<ISprintService, SprintService>();
        services.AddScoped<ISprintItemService, SprintItemService>();

        // Backlog services
        services.AddScoped<IProductBacklogItemService, ProductBacklogItemService>();
        services.AddScoped<IBacklogItemService, BacklogItemService>();

        // Epic services
        services.AddScoped<IEpicService, EpicService>();

        // Task Bid services
        services.AddScoped<ITaskBidService, TaskBidService>();

        // Wallet services
        services.AddScoped<IWalletService, WalletService>();

        // Task Escrow services
        services.AddScoped<ITaskEscrowService, TaskEscrowService>();

        // Gamification extended services
        services.AddScoped<IUserStreakService, UserStreakService>();
        services.AddScoped<IReferralService, ReferralService>();

        // Notification & Activity services
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IActivityService, ActivityService>();

        // Task services
        services.AddScoped<ITaskService, TaskService>();

        // Event services
        services.AddScoped<IEventService, EventService>();

        // Opportunity services
        services.AddScoped<IOpportunityService, OpportunityService>();

        // Governance services
        services.AddScoped<IGovernanceService, GovernanceService>();
        services.AddScoped<IDelegatedVoteService, DelegatedVoteService>();

        // Exchange services
        services.AddScoped<ITokenSwapService, TokenSwapService>();
        services.AddScoped<ILiquidityPoolService, LiquidityPoolService>();
        services.AddScoped<ILiquidityProviderService, LiquidityProviderService>();

        // Attachment services
        services.AddScoped<IAttachmentService, AttachmentService>();

        // Chat services
        services.AddScoped<IChatService, ChatService>();

        return services;
    }
}
