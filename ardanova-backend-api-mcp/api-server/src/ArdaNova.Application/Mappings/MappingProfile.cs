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
        CreateMap<User, ProjectCreatorDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
            .ForMember(dest => dest.Image, opt => opt.MapFrom(src => src.image));
        CreateMap<Project, ProjectDto>()
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));
        CreateMap<ProjectTask, ProjectTaskDto>();
        CreateMap<ProjectTaskDependency, ProjectTaskDependencyDto>();
        CreateMap<ProjectResource, ProjectResourceDto>();
        CreateMap<ProjectMilestone, ProjectMilestoneDto>();
        CreateMap<ProjectSupport, ProjectSupportDto>();
        CreateMap<ProjectApplication, ProjectApplicationDto>();
        CreateMap<ProjectComment, ProjectCommentDto>();
        CreateMap<ProjectUpdate, ProjectUpdateDto>();
        CreateMap<ProjectEquity, ProjectEquityDto>();

        // Guild mappings (entity Guild -> DTO GuildDto)
        CreateMap<Guild, GuildDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.slug))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.description))
            .ForMember(dest => dest.Website, opt => opt.MapFrom(src => src.website))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.email))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.phone))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.address))
            .ForMember(dest => dest.Logo, opt => opt.MapFrom(src => src.logo))
            .ForMember(dest => dest.Portfolio, opt => opt.MapFrom(src => src.portfolio))
            .ForMember(dest => dest.Specialties, opt => opt.MapFrom(src => src.specialties))
            .ForMember(dest => dest.IsVerified, opt => opt.MapFrom(src => src.isVerified))
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.rating))
            .ForMember(dest => dest.ReviewsCount, opt => opt.MapFrom(src => src.reviewsCount))
            .ForMember(dest => dest.ProjectsCount, opt => opt.MapFrom(src => src.projectsCount))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.createdAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.updatedAt))
            .ForMember(dest => dest.OwnerId, opt => opt.MapFrom(src => src.ownerId));
        CreateMap<GuildMember, GuildMemberDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.GuildId, opt => opt.MapFrom(src => src.guildId))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.userId))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.role))
            .ForMember(dest => dest.JoinedAt, opt => opt.MapFrom(src => src.joinedAt));
        CreateMap<ProjectBid, ProjectBidDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.projectId))
            .ForMember(dest => dest.GuildId, opt => opt.MapFrom(src => src.guildId))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.userId))
            .ForMember(dest => dest.Proposal, opt => opt.MapFrom(src => src.proposal))
            .ForMember(dest => dest.Timeline, opt => opt.MapFrom(src => src.timeline))
            .ForMember(dest => dest.Budget, opt => opt.MapFrom(src => src.budget))
            .ForMember(dest => dest.Deliverables, opt => opt.MapFrom(src => src.deliverables))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.status))
            .ForMember(dest => dest.SubmittedAt, opt => opt.MapFrom(src => src.submittedAt))
            .ForMember(dest => dest.ReviewedAt, opt => opt.MapFrom(src => src.reviewedAt));
        CreateMap<GuildReview, GuildReviewDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.GuildId, opt => opt.MapFrom(src => src.guildId))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.projectId))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.userId))
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.rating))
            .ForMember(dest => dest.Comment, opt => opt.MapFrom(src => src.comment))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.createdAt));

        // Shop mappings (entity Shop -> DTO ShopDto)
        CreateMap<Shop, ShopDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.description))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.address))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.phone))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.email))
            .ForMember(dest => dest.Website, opt => opt.MapFrom(src => src.website))
            .ForMember(dest => dest.Logo, opt => opt.MapFrom(src => src.logo))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.isActive))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.createdAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.updatedAt))
            .ForMember(dest => dest.OwnerId, opt => opt.MapFrom(src => src.ownerId));
        CreateMap<Product, ShopProductDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.ShopId, opt => opt.MapFrom(src => src.shopId))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.description))
            .ForMember(dest => dest.Sku, opt => opt.MapFrom(src => src.sku))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.price))
            .ForMember(dest => dest.Cost, opt => opt.MapFrom(src => src.cost))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.category))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.isActive))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.createdAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.updatedAt))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.userId));
        CreateMap<ShopAnalytics, ShopAnalyticsDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.ShopId, opt => opt.MapFrom(src => src.shopId))
            .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.date))
            .ForMember(dest => dest.Revenue, opt => opt.MapFrom(src => src.revenue))
            .ForMember(dest => dest.Expenses, opt => opt.MapFrom(src => src.expenses))
            .ForMember(dest => dest.Profit, opt => opt.MapFrom(src => src.profit))
            .ForMember(dest => dest.SalesCount, opt => opt.MapFrom(src => src.salesCount))
            .ForMember(dest => dest.NewCustomers, opt => opt.MapFrom(src => src.newCustomers))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.createdAt));

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

        // Attachment mappings
        CreateMap<Attachment, AttachmentDto>()
            .ForMember(dest => dest.BucketPath, opt => opt.MapFrom(src => src.bucketPath))
            .ForMember(dest => dest.UploadedById, opt => opt.MapFrom(src => src.uploadedById))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.createdAt))
            .ForMember(dest => dest.LastUsedAt, opt => opt.MapFrom(src => src.lastUsedAt))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.type));
    }
}
