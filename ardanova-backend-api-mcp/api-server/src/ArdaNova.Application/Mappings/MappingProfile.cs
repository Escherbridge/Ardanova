namespace ArdaNova.Application.Mappings;

using AutoMapper;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Entities;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>();
        CreateMap<Account, AccountDto>();
        CreateMap<Session, SessionDto>();
        CreateMap<VerificationToken, VerificationTokenDto>();
        CreateMap<UserSkill, UserSkillDto>();
        CreateMap<UserExperience, UserExperienceDto>();

        // Project mappings
        CreateMap<Project, ProjectDto>();
        CreateMap<ProjectTask, ProjectTaskDto>();
        CreateMap<ProjectTaskDependency, ProjectTaskDependencyDto>();
        CreateMap<ProjectResource, ProjectResourceDto>();
        CreateMap<ProjectMilestone, ProjectMilestoneDto>();
        CreateMap<ProjectSupport, ProjectSupportDto>();
        CreateMap<ProjectApplication, ProjectApplicationDto>();
        CreateMap<ProjectComment, ProjectCommentDto>();
        CreateMap<ProjectUpdate, ProjectUpdateDto>();
        CreateMap<ProjectEquity, ProjectEquityDto>();

        // Agency mappings
        CreateMap<Agency, AgencyDto>();
        CreateMap<AgencyMember, AgencyMemberDto>();
        CreateMap<ProjectBid, ProjectBidDto>();
        CreateMap<AgencyReview, AgencyReviewDto>();

        // Business mappings
        CreateMap<Business, BusinessDto>();
        CreateMap<Customer, CustomerDto>();
        CreateMap<Product, ProductDto>();
        CreateMap<Invoice, InvoiceDto>();
        CreateMap<Sale, SaleDto>();
        CreateMap<SaleItem, SaleItemDto>();
        CreateMap<InventoryItem, InventoryItemDto>();
        CreateMap<MarketingCampaign, MarketingCampaignDto>();
        CreateMap<BusinessAnalytics, BusinessAnalyticsDto>();

        // Wallet mappings
        CreateMap<Wallet, WalletDto>();

        // Task Escrow mappings
        CreateMap<TaskEscrow, TaskEscrowDto>();

        // Gamification extended mappings
        CreateMap<UserStreak, UserStreakDto>();
        CreateMap<Referral, ReferralDto>();

        // Notification & Activity mappings
        CreateMap<Notification, NotificationDto>();
        CreateMap<Activity, ActivityDto>();

        // Governance mappings
        CreateMap<DelegatedVote, DelegatedVoteDto>();

        // Exchange mappings
        CreateMap<TokenSwap, TokenSwapDto>();
        CreateMap<LiquidityPool, LiquidityPoolDto>();
        CreateMap<LiquidityProvider, LiquidityProviderDto>();
    }
}
