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
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.Categories, opt => opt.MapFrom(src =>
                string.IsNullOrEmpty(src.categories)
                    ? new List<string>()
                    : src.categories.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()));
        CreateMap<ProjectTask, ProjectTaskDto>();
        CreateMap<ProjectTaskDependency, ProjectTaskDependencyDto>();
        CreateMap<ProjectResource, ProjectResourceDto>();
        CreateMap<ProjectMilestone, ProjectMilestoneDto>();
        CreateMap<Epic, EpicDto>();
        CreateMap<Sprint, SprintDto>();
        CreateMap<ProductBacklogItem, ProductBacklogItemDto>();
        CreateMap<Feature, FeatureDto>();
        CreateMap<ProjectSupport, ProjectSupportDto>();
        CreateMap<ProjectApplication, ProjectApplicationDto>();
        CreateMap<ProjectComment, ProjectCommentDto>();
        CreateMap<ProjectUpdate, ProjectUpdateDto>();
        CreateMap<ProjectEquity, ProjectEquityDto>();
        CreateMap<ProjectMember, ProjectMemberDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.projectId))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.userId))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.role))
            .ForMember(dest => dest.ShareBalance, opt => opt.MapFrom(src => src.shareBalance))
            .ForMember(dest => dest.VotingPower, opt => opt.MapFrom(src => src.votingPower))
            .ForMember(dest => dest.JoinedAt, opt => opt.MapFrom(src => src.joinedAt))
            .ForMember(dest => dest.InvitedById, opt => opt.MapFrom(src => src.invitedById))
            .ForMember(dest => dest.User, opt => opt.Ignore());
        CreateMap<User, ProjectMemberUserDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.email))
            .ForMember(dest => dest.Image, opt => opt.MapFrom(src => src.image));

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
        CreateMap<GuildReview, GuildReviewDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.GuildId, opt => opt.MapFrom(src => src.guildId))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.projectId))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.userId))
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.rating))
            .ForMember(dest => dest.Comment, opt => opt.MapFrom(src => src.comment))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.createdAt));

        // Product mappings (entity Product -> DTO ProductDto)
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.projectId))
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

        // Task mappings (entity ProjectTask -> DTO TaskDto)
        CreateMap<ProjectTask, TaskDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.projectId))
            .ForMember(dest => dest.PbiId, opt => opt.MapFrom(src => src.pbiId))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.title))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.description))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.status))
            .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.priority))
            .ForMember(dest => dest.TaskType, opt => opt.MapFrom(src => src.taskType))
            .ForMember(dest => dest.EstimatedHours, opt => opt.MapFrom(src => src.estimatedHours))
            .ForMember(dest => dest.ActualHours, opt => opt.MapFrom(src => src.actualHours))
            .ForMember(dest => dest.EquityReward, opt => opt.MapFrom(src => src.equityReward))
            .ForMember(dest => dest.EscrowStatus, opt => opt.MapFrom(src => src.escrowStatus))
            .ForMember(dest => dest.DueDate, opt => opt.MapFrom(src => src.dueDate))
            .ForMember(dest => dest.CompletedAt, opt => opt.MapFrom(src => src.completedAt))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.createdAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.updatedAt))
            .ForMember(dest => dest.AssignedToId, opt => opt.MapFrom(src => src.assignedToId))
            .ForMember(dest => dest.AssignedTo, opt => opt.Ignore())
            .ForMember(dest => dest.Project, opt => opt.Ignore());
        CreateMap<User, TaskUserDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
            .ForMember(dest => dest.Image, opt => opt.MapFrom(src => src.image));
        CreateMap<Project, TaskProjectDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.title))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.slug));

        // Event mappings (entity Event -> DTO EventDto)
        CreateMap<Event, EventDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.title))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.slug))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.description))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.type))
            .ForMember(dest => dest.Visibility, opt => opt.MapFrom(src => src.visibility))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.status))
            .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.location))
            .ForMember(dest => dest.LocationUrl, opt => opt.MapFrom(src => src.locationUrl))
            .ForMember(dest => dest.IsOnline, opt => opt.MapFrom(src => src.isOnline))
            .ForMember(dest => dest.MeetingUrl, opt => opt.MapFrom(src => src.meetingUrl))
            .ForMember(dest => dest.Timezone, opt => opt.MapFrom(src => src.timezone))
            .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.startDate))
            .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.endDate))
            .ForMember(dest => dest.MaxAttendees, opt => opt.MapFrom(src => src.maxAttendees))
            .ForMember(dest => dest.AttendeesCount, opt => opt.MapFrom(src => src.attendeesCount))
            .ForMember(dest => dest.CoverImage, opt => opt.MapFrom(src => src.coverImage))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.createdAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.updatedAt))
            .ForMember(dest => dest.OrganizerId, opt => opt.MapFrom(src => src.organizerId))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.projectId))
            .ForMember(dest => dest.GuildId, opt => opt.MapFrom(src => src.guildId))
            .ForMember(dest => dest.Organizer, opt => opt.Ignore());
        CreateMap<User, EventOrganizerDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
            .ForMember(dest => dest.Image, opt => opt.MapFrom(src => src.image));
        CreateMap<EventAttendee, EventAttendeeDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.eventId))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.userId))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.status))
            .ForMember(dest => dest.RsvpAt, opt => opt.MapFrom(src => src.rsvpAt))
            .ForMember(dest => dest.AttendedAt, opt => opt.MapFrom(src => src.attendedAt))
            .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.notes))
            .ForMember(dest => dest.User, opt => opt.Ignore());
        CreateMap<User, EventAttendeeUserDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
            .ForMember(dest => dest.Image, opt => opt.MapFrom(src => src.image));

        // Opportunity mappings (entity Opportunity -> DTO OpportunityDto)
        CreateMap<Opportunity, OpportunityDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.title))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.slug))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.description))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.type))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.status))
            .ForMember(dest => dest.ExperienceLevel, opt => opt.MapFrom(src => src.experienceLevel))
            .ForMember(dest => dest.Requirements, opt => opt.MapFrom(src => src.requirements))
            .ForMember(dest => dest.Skills, opt => opt.MapFrom(src => src.skills))
            .ForMember(dest => dest.Benefits, opt => opt.MapFrom(src => src.benefits))
            .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.location))
            .ForMember(dest => dest.IsRemote, opt => opt.MapFrom(src => src.isRemote))
            .ForMember(dest => dest.Compensation, opt => opt.MapFrom(src => src.compensation))
            .ForMember(dest => dest.CompensationDetails, opt => opt.MapFrom(src => src.compensationDetails))
            .ForMember(dest => dest.Deadline, opt => opt.MapFrom(src => src.deadline))
            .ForMember(dest => dest.MaxApplications, opt => opt.MapFrom(src => src.maxApplications))
            .ForMember(dest => dest.ApplicationsCount, opt => opt.MapFrom(src => src.applicationsCount))
            .ForMember(dest => dest.CoverImage, opt => opt.MapFrom(src => src.coverImage))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.createdAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.updatedAt))
            .ForMember(dest => dest.ClosedAt, opt => opt.MapFrom(src => src.closedAt))
            .ForMember(dest => dest.PosterId, opt => opt.MapFrom(src => src.posterId))
            .ForMember(dest => dest.GuildId, opt => opt.MapFrom(src => src.guildId))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.projectId))
            .ForMember(dest => dest.TaskId, opt => opt.MapFrom(src => src.taskId))
            .ForMember(dest => dest.Poster, opt => opt.Ignore());
        CreateMap<User, OpportunityPosterDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
            .ForMember(dest => dest.Image, opt => opt.MapFrom(src => src.image));
        CreateMap<OpportunityApplication, OpportunityApplicationDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.OpportunityId, opt => opt.MapFrom(src => src.opportunityId))
            .ForMember(dest => dest.ApplicantId, opt => opt.MapFrom(src => src.applicantId))
            .ForMember(dest => dest.CoverLetter, opt => opt.MapFrom(src => src.coverLetter))
            .ForMember(dest => dest.Portfolio, opt => opt.MapFrom(src => src.portfolio))
            .ForMember(dest => dest.AdditionalInfo, opt => opt.MapFrom(src => src.additionalInfo))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.status))
            .ForMember(dest => dest.ReviewNotes, opt => opt.MapFrom(src => src.reviewNotes))
            .ForMember(dest => dest.AppliedAt, opt => opt.MapFrom(src => src.appliedAt))
            .ForMember(dest => dest.ReviewedAt, opt => opt.MapFrom(src => src.reviewedAt))
            .ForMember(dest => dest.Applicant, opt => opt.Ignore());
        CreateMap<User, OpportunityApplicationApplicantDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
            .ForMember(dest => dest.Image, opt => opt.MapFrom(src => src.image))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.email));

        // Opportunity Update mappings
        CreateMap<OpportunityUpdate, OpportunityUpdateDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.OpportunityId, opt => opt.MapFrom(src => src.opportunityId))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.userId))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.title))
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.content))
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.images))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.createdAt))
            .ForMember(dest => dest.User, opt => opt.Ignore());
        CreateMap<User, OpportunityUpdateAuthorDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
            .ForMember(dest => dest.Image, opt => opt.MapFrom(src => src.image));

        // Opportunity Comment mappings
        CreateMap<OpportunityComment, OpportunityCommentDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.OpportunityId, opt => opt.MapFrom(src => src.opportunityId))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.userId))
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.content))
            .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.parentId))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.createdAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.updatedAt))
            .ForMember(dest => dest.Author, opt => opt.Ignore());
        CreateMap<User, OpportunityCommentAuthorDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
            .ForMember(dest => dest.Image, opt => opt.MapFrom(src => src.image))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.email));

        // Governance mappings (entity Proposal -> DTO ProposalDto)
        CreateMap<Proposal, ProposalDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.projectId))
            .ForMember(dest => dest.CreatorId, opt => opt.MapFrom(src => src.creatorId))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.type))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.title))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.description))
            .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.options))
            .ForMember(dest => dest.Quorum, opt => opt.MapFrom(src => src.quorum))
            .ForMember(dest => dest.Threshold, opt => opt.MapFrom(src => src.threshold))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.status))
            .ForMember(dest => dest.VotingStart, opt => opt.MapFrom(src => src.votingStart))
            .ForMember(dest => dest.VotingEnd, opt => opt.MapFrom(src => src.votingEnd))
            .ForMember(dest => dest.ExecutionDelay, opt => opt.MapFrom(src => src.executionDelay))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.createdAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.updatedAt))
            .ForMember(dest => dest.Creator, opt => opt.Ignore())
            .ForMember(dest => dest.Project, opt => opt.Ignore())
            .ForMember(dest => dest.VotesCount, opt => opt.Ignore())
            .ForMember(dest => dest.TotalVotingPower, opt => opt.Ignore());
        CreateMap<User, ProposalCreatorDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
            .ForMember(dest => dest.Image, opt => opt.MapFrom(src => src.image));
        CreateMap<Project, ProposalProjectDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.title))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.slug));
        CreateMap<Vote, VoteDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.ProposalId, opt => opt.MapFrom(src => src.proposalId))
            .ForMember(dest => dest.VoterId, opt => opt.MapFrom(src => src.voterId))
            .ForMember(dest => dest.Choice, opt => opt.MapFrom(src => src.choice))
            .ForMember(dest => dest.Weight, opt => opt.MapFrom(src => src.weight))
            .ForMember(dest => dest.Reason, opt => opt.MapFrom(src => src.reason))
            .ForMember(dest => dest.TxHash, opt => opt.MapFrom(src => src.txHash))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.createdAt))
            .ForMember(dest => dest.Voter, opt => opt.Ignore());
        CreateMap<User, VoteUserDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
            .ForMember(dest => dest.Image, opt => opt.MapFrom(src => src.image));
        CreateMap<ProposalComment, ProposalCommentDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.ProposalId, opt => opt.MapFrom(src => src.proposalId))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.userId))
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.content))
            .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.parentId))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.createdAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.updatedAt))
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Replies, opt => opt.Ignore());
        CreateMap<User, ProposalCommentUserDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
            .ForMember(dest => dest.Image, opt => opt.MapFrom(src => src.image));

        // Wallet mappings
        CreateMap<Wallet, WalletDto>();

        // Task Escrow mappings
        CreateMap<TaskEscrow, TaskEscrowDto>();

        // Gamification extended mappings
        CreateMap<UserStreak, UserStreakDto>();
        CreateMap<Referral, ReferralDto>();

        // Leaderboard mappings
        CreateMap<Leaderboard, LeaderboardDto>();
        CreateMap<LeaderboardEntry, LeaderboardEntryDto>();

        // XP Event mappings
        CreateMap<XPEvent, XPEventDto>();

        // Achievement mappings
        CreateMap<Achievement, AchievementDto>();
        CreateMap<UserAchievement, UserAchievementDto>();

        // Notification & Activity mappings
        CreateMap<Notification, NotificationDto>();
        CreateMap<Activity, ActivityDto>();

        // Governance mappings
        CreateMap<DelegatedVote, DelegatedVoteDto>();

        // Membership Credential mappings
        CreateMap<MembershipCredential, MembershipCredentialDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.ProjectId, opt => opt.MapFrom(src => src.projectId))
            .ForMember(dest => dest.GuildId, opt => opt.MapFrom(src => src.guildId))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.userId))
            .ForMember(dest => dest.AssetId, opt => opt.MapFrom(src => src.assetId))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.status.ToString()))
            .ForMember(dest => dest.IsTransferable, opt => opt.MapFrom(src => src.isTransferable))
            .ForMember(dest => dest.Tier, opt => opt.MapFrom(src => src.tier != null ? src.tier.ToString() : null))
            .ForMember(dest => dest.GrantedVia, opt => opt.MapFrom(src => src.grantedVia.ToString()))
            .ForMember(dest => dest.GrantedByProposalId, opt => opt.MapFrom(src => src.grantedByProposalId))
            .ForMember(dest => dest.MetadataUri, opt => opt.MapFrom(src => src.metadataUri))
            .ForMember(dest => dest.MintTxHash, opt => opt.MapFrom(src => src.mintTxHash))
            .ForMember(dest => dest.RevokeTxHash, opt => opt.MapFrom(src => src.revokeTxHash))
            .ForMember(dest => dest.MintedAt, opt => opt.MapFrom(src => src.mintedAt))
            .ForMember(dest => dest.RevokedAt, opt => opt.MapFrom(src => src.revokedAt))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.createdAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.updatedAt));

        // Exchange mappings
        // TODO: LiquidityPool/LiquidityProvider DTOs not yet implemented
        // CreateMap<LiquidityPool, LiquidityPoolDto>();
        // CreateMap<LiquidityProvider, LiquidityProviderDto>();

        // Attachment mappings
        CreateMap<Attachment, AttachmentDto>()
            .ForMember(dest => dest.BucketPath, opt => opt.MapFrom(src => src.bucketPath))
            .ForMember(dest => dest.UploadedById, opt => opt.MapFrom(src => src.uploadedById))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.createdAt))
            .ForMember(dest => dest.LastUsedAt, opt => opt.MapFrom(src => src.lastUsedAt))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.type));

        // KYC mappings
        CreateMap<KycSubmission, KycSubmissionDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.userId))
            .ForMember(dest => dest.Provider, opt => opt.MapFrom(src => src.provider))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.status))
            .ForMember(dest => dest.ReviewerId, opt => opt.MapFrom(src => src.reviewerId))
            .ForMember(dest => dest.ReviewNotes, opt => opt.MapFrom(src => src.reviewNotes))
            .ForMember(dest => dest.RejectionReason, opt => opt.MapFrom(src => src.rejectionReason))
            .ForMember(dest => dest.ProviderSessionId, opt => opt.MapFrom(src => src.providerSessionId))
            .ForMember(dest => dest.SubmittedAt, opt => opt.MapFrom(src => src.submittedAt))
            .ForMember(dest => dest.ReviewedAt, opt => opt.MapFrom(src => src.reviewedAt))
            .ForMember(dest => dest.ExpiresAt, opt => opt.MapFrom(src => src.expiresAt))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.createdAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.updatedAt))
            .ForMember(dest => dest.Documents, opt => opt.Ignore());
        CreateMap<KycDocument, KycDocumentDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.SubmissionId, opt => opt.MapFrom(src => src.submissionId))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.type))
            .ForMember(dest => dest.FileUrl, opt => opt.MapFrom(src => src.fileUrl))
            .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.fileName))
            .ForMember(dest => dest.MimeType, opt => opt.MapFrom(src => src.mimeType))
            .ForMember(dest => dest.FileSizeBytes, opt => opt.MapFrom(src => src.fileSizeBytes))
            .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src => src.metadata))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.createdAt));
    }
}
