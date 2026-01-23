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

        // Agency services
        services.AddScoped<IAgencyService, AgencyService>();
        services.AddScoped<IAgencyMemberService, AgencyMemberService>();
        services.AddScoped<IProjectBidService, ProjectBidService>();
        services.AddScoped<IAgencyReviewService, AgencyReviewService>();

        // Business services
        services.AddScoped<IBusinessService, BusinessService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<ISaleItemService, SaleItemService>();
        services.AddScoped<IInventoryItemService, InventoryItemService>();
        services.AddScoped<IMarketingCampaignService, MarketingCampaignService>();
        services.AddScoped<IBusinessAnalyticsService, BusinessAnalyticsService>();

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

        // Governance services
        services.AddScoped<IDelegatedVoteService, DelegatedVoteService>();

        // Exchange services
        services.AddScoped<ITokenSwapService, TokenSwapService>();
        services.AddScoped<ILiquidityPoolService, LiquidityPoolService>();
        services.AddScoped<ILiquidityProviderService, LiquidityProviderService>();

        return services;
    }
}
