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
        // The viewer-scoped read surface is the same scoped instance, exposed under its
        // own interface so callers depend on it explicitly rather than casting.
        services.AddScoped<IProjectVisibility>(sp => (IProjectVisibility)sp.GetRequiredService<IProjectService>());
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
        // Viewer-aware post reads: the same scoped instance under its own interface.
        services.AddScoped<IPostFieldNotesService>(sp => (IPostFieldNotesService)sp.GetRequiredService<IPostService>());

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
        // Funding setup is the same scoped instance under its own interface, so the funding
        // routes depend on it explicitly rather than casting - the IProjectVisibility pattern.
        services.AddScoped<IProjectTokenProvisioning>(sp => (IProjectTokenProvisioning)sp.GetRequiredService<IProjectTokenService>());
        services.AddScoped<ITokenBalanceService, TokenBalanceService>();
        services.AddScoped<IExchangeService, ExchangeService>();
        // The declared-rate source is the same scoped instance under its own interface, so a
        // caller that only needs the rate does not take a dependency on the whole exchange.
        services.AddScoped<IProjectUnitRateSource>(sp => (IProjectUnitRateSource)sp.GetRequiredService<IExchangeService>());
        services.AddScoped<ISwapService, SwapService>();
        services.AddScoped<IPayoutService, PayoutService>();
        // Cashing out is the same scoped instance under its own interface, so the payouts route
        // injects the cash-out surface directly rather than probing the payout service with `as`.
        services.AddScoped<ICashOutService>(sp => (ICashOutService)sp.GetRequiredService<IPayoutService>());
        services.AddScoped<ITreasuryService, TreasuryService>();
        // The buy-back read model is the same scoped instance under its own interface, so the
        // treasury route can constructor-inject what it needs instead of casting - the
        // IProjectUnitRateSource pattern above.
        services.AddScoped<IProjectBuyBackReadModel>(sp => (IProjectBuyBackReadModel)sp.GetRequiredService<ITreasuryService>());
        // One project's money, derived rather than accumulated. Its treasury-balance source is
        // the read model registered directly above, so this line must follow it. With this
        // registration the container is the SINGLE construction path for the service.
        services.AddScoped<IProjectLedgerService, ProjectLedgerService>();
        services.AddScoped<IProjectGateService, ProjectGateService>();
        // The add-funds seam is ALWAYS bound, and its default refuses. Infrastructure's
        // AddAzoaFiatRail replaces this with the node-backed gateway when the operator has
        // opted in and the transport is configured, so an unconfigured deployment cannot
        // send anybody to a payment page.
        services.AddScoped<IFiatRailGateway, DisabledFiatRailGateway>();
        services.AddScoped<IFundingIntentService, FundingIntentService>();
        // The operator channel the dispatcher wakes a person through, and the only registration
        // that can actually reach a phone. It posts to OperatorAlerts:WebhookUrl and ALWAYS writes
        // the structured log line first, so an unconfigured or refusing endpoint downgrades to the
        // old logging behaviour LOUDLY - a critical line naming the key - instead of silently.
        services.AddScoped<IOperatorAlertService, WebhookOperatorAlertService>();
        services.AddScoped<IEconomicOutboxDispatchService, EconomicOutboxDispatchService>();

        // AZOA shared-node integration services (contract sections 4, 5, 6, and 9).
        // Each AZOA track registers its own service on its own line below.
        services.AddScoped<IAzoaCustodialAccountService, AzoaCustodialAccountService>();
        services.AddScoped<IAzoaValueEligibilityService, AzoaValueEligibilityService>();
        services.AddScoped<IAzoaQuestAuthoringService, AzoaQuestAuthoringService>(); // azoa-quest-authoring
        services.AddScoped<IAzoaQuestSignalService, AzoaQuestSignalService>(); // azoa-quest-authoring
        services.AddScoped<IAzoaAllocationService, AzoaAllocationService>();   // treasury-reward-to-azoa-allocation

        // Enum lookup (singleton - enum types are static)
        services.AddSingleton<IEnumLookupService, EnumLookupService>();

        return services;
    }
}
