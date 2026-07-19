namespace ArdaNova.Application;

using ArdaNova.Application.Mappings;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(
            MappingSecurityPolicy.Apply,
            typeof(MappingProfile).Assembly);

        // User services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IUserSkillService, UserSkillService>();
        services.AddScoped<IUserExperienceService, UserExperienceService>();
        services.AddScoped<IUserFollowService, UserFollowService>();

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
        services.AddScoped<IProjectInvitationService, ProjectInvitationService>();
        services.AddScoped<IHierarchyAuthorizationService, HierarchyAuthorizationService>();

        // Guild services
        services.AddScoped<IGuildService, GuildService>();
        services.AddScoped<IGuildMemberService, GuildMemberService>();
        services.AddScoped<IGuildReviewService, GuildReviewService>();
        services.AddScoped<IGuildUpdateService, GuildUpdateService>();
        services.AddScoped<IGuildApplicationService, GuildApplicationService>();
        services.AddScoped<IGuildInvitationService, GuildInvitationService>();
        services.AddScoped<IGuildFollowService, GuildFollowService>();

        // Product services
        services.AddScoped<IProductService, ProductService>();

        // Sprint services
        services.AddScoped<ISprintService, SprintService>();

        // Backlog services
        services.AddScoped<IProductBacklogItemService, ProductBacklogItemService>();

        // Epic services
        services.AddScoped<IEpicService, EpicService>();

        // Feature services
        services.AddScoped<IFeatureService, FeatureService>();

        // Wallet services
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IWalletVerificationService, WalletVerificationService>();

        // Task Escrow services
        services.AddScoped<ITaskEscrowService, TaskEscrowService>();
        services.AddScoped<ITaskCommerceService, TaskCommerceService>();

        // Gamification extended services
        services.AddScoped<IUserStreakService, UserStreakService>();
        services.AddScoped<IReferralService, ReferralService>();
        services.AddScoped<IXPEventService, XPEventService>();
        services.AddScoped<ILeaderboardService, LeaderboardService>();
        services.AddScoped<IAchievementService, AchievementService>();

        // Notification & Activity services
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IActivityService, ActivityService>();

        // Posts / feed
        services.AddScoped<IPostService, PostService>();

        // Task services
        services.AddScoped<ITaskService, TaskService>();

        // Event services
        services.AddScoped<IEventService, EventService>();

        // Opportunity services
        services.AddScoped<IOpportunityService, OpportunityService>();
        services.AddScoped<IOpportunityBidService, OpportunityBidService>();

        // Governance services
        services.AddScoped<IGovernanceService, GovernanceService>();
        services.AddScoped<IDelegatedVoteService, DelegatedVoteService>();
        services.AddScoped<IMembershipCredentialService, MembershipCredentialService>();
        services.AddScoped<ICredentialUtilityService, CredentialUtilityService>();

        // Exchange services
        // TODO: LiquidityPool/LiquidityProvider services not yet implemented
        // services.AddScoped<ILiquidityPoolService, LiquidityPoolService>();
        // services.AddScoped<ILiquidityProviderService, LiquidityProviderService>();

        // Attachment services
        services.AddScoped<IAttachmentService, AttachmentService>();

        // Chat services
        services.AddScoped<IChatService, ChatService>();

        // KYC services
        services.AddScoped<IKycService, KycService>();
        services.AddScoped<IKycProviderService, ManualKycProviderService>();
        services.AddScoped<IKycGateService, KycGateService>();

        // Tokenomics services
        services.AddScoped<IProjectTokenService, ProjectTokenService>();
        services.AddScoped<ITokenBalanceService, TokenBalanceService>();
        services.AddScoped<IExchangeService, ExchangeService>();
        services.AddScoped<ISwapService, SwapService>();
        services.AddScoped<IPayoutService, PayoutService>();
        services.AddScoped<ITreasuryService, TreasuryService>();
        services.AddScoped<IProjectGateService, ProjectGateService>();
        services.AddScoped<IStripeCheckoutGateway, StripeCheckoutGateway>();
        services.AddScoped<IFundingIntentService, FundingIntentService>();
        services.AddScoped<IStripePaymentIntentGateway, StripePaymentIntentGateway>();
        services.AddScoped<IStripeService, StripeService>();
        services.AddScoped<IEconomicOutboxDispatchService, EconomicOutboxDispatchService>();

        // AZOA shared-node integration services (contract sections 4, 5, 6, and 9).
        // Each AZOA track registers its own service on its own line below.
        services.AddScoped<IAzoaAvatarService, AzoaAvatarService>();           // azoa-avatar-onboarding
        services.AddScoped<IAzoaCustodialAccountService, AzoaCustodialAccountService>();
        services.AddScoped<IAzoaQuestAuthoringService, AzoaQuestAuthoringService>(); // azoa-quest-authoring
        services.AddScoped<IAzoaQuestSignalService, AzoaQuestSignalService>(); // azoa-quest-authoring
        services.AddScoped<IAzoaAllocationService, AzoaAllocationService>();   // treasury-reward-to-azoa-allocation

        // Enum lookup (singleton - enum types are static)
        services.AddSingleton<IEnumLookupService, EnumLookupService>();

        return services;
    }
}
