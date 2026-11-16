namespace ArdaNova.Application.Mappings;

using Mapster;
using MapsterMapper;
using ArdaNova.Application.DTOs;
using ArdaNova.Domain.Models.Entities;

/// <summary>
/// Entity-to-DTO maps. Case-insensitive name matching covers the camelCase entity
/// properties; every explicit member below is a conversion, a derived value, or an ignore.
/// </summary>
public static class MappingProfile
{
    public static TypeAdapterConfig SharedConfig { get; } = Build(beforePolicy: null);

    public static IMapper SharedMapper { get; } = new Mapper(SharedConfig);

    public static TypeAdapterConfig CreateConfig(Action<TypeAdapterConfig>? beforePolicy = null)
        => beforePolicy is null ? SharedConfig : Build(beforePolicy);

    public static IMapper CreateMapper(Action<TypeAdapterConfig>? beforePolicy = null)
        => beforePolicy is null ? SharedMapper : new Mapper(Build(beforePolicy));

    private static TypeAdapterConfig Build(Action<TypeAdapterConfig>? beforePolicy)
    {
        var config = new TypeAdapterConfig();
        // Entities are camelCase and DTOs are PascalCase. IgnoreCase matches those names
        // and does not fold underscores, so distinct names stay distinct.
        config.Default.NameMatchingStrategy(NameMatchingStrategy.IgnoreCase);
        Register(config);
        beforePolicy?.Invoke(config);
        MappingSecurityPolicy.Apply(config);
        return config;
    }

    public static void Register(TypeAdapterConfig config)
    {

        // User mappings
        config.NewConfig<User, UserDto>();
        config.NewConfig<Account, AccountDto>();
        config.NewConfig<Session, SessionDto>();
        config.NewConfig<VerificationToken, VerificationTokenDto>();
        config.NewConfig<UserSkill, UserSkillDto>();
        config.NewConfig<UserExperience, UserExperienceDto>();

        // Project mappings
        config.NewConfig<User, ProjectCreatorDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Name, src => src.name)
            .Map(dest => dest.Image, src => src.image);
        config.NewConfig<Project, ProjectDto>()
            .Map(dest => dest.CreatedBy, src => src.CreatedBy)
            .Map(dest => dest.Categories, src =>
                string.IsNullOrEmpty(src.categories)
                    ? new List<string>()
                    : src.categories.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
            // Lane L (SR-03 Phase 3 §3): the edit form's round-trip view of the derived fields, each
            // as a "#slug" chip via the same canonicalisation Hashtags.Extract uses for post copy.
            // Fully-qualified rather than a new `using`, so this append stays a one-line diff next
            // to K1's Post/Task blocks below (G0 §11: both lanes append to this file).
            .Map(dest => dest.Hashtags, src =>
                ArdaNova.Application.Services.Implementations.ProjectHashtagDisplay.From(src.categories, src.projectType));
        config.NewConfig<ProjectTask, ProjectTaskDto>();
        config.NewConfig<ProjectTaskDependency, ProjectTaskDependencyDto>();
        config.NewConfig<ProjectResource, ProjectResourceDto>();
        config.NewConfig<ProjectMilestone, ProjectMilestoneDto>();
        config.NewConfig<Epic, EpicDto>();
        config.NewConfig<Sprint, SprintDto>();
        config.NewConfig<ProductBacklogItem, ProductBacklogItemDto>();
        config.NewConfig<Feature, FeatureDto>();
        config.NewConfig<ProjectSupport, ProjectSupportDto>();
        config.NewConfig<ProjectApplication, ProjectApplicationDto>();
        config.NewConfig<ProjectComment, ProjectCommentDto>();
        config.NewConfig<ProjectUpdate, ProjectUpdateDto>();
        config.NewConfig<ProjectEquity, ProjectEquityDto>();
        config.NewConfig<ProjectMember, ProjectMemberDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.ProjectId, src => src.projectId)
            .Map(dest => dest.UserId, src => src.userId)
            .Map(dest => dest.Role, src => src.role)
            .Map(dest => dest.ShareBalance, src => src.shareBalance)
            .Map(dest => dest.VotingPower, src => src.votingPower)
            .Map(dest => dest.JoinedAt, src => src.joinedAt)
            .Map(dest => dest.InvitedById, src => src.invitedById)
            .Ignore(dest => dest.User);
        config.NewConfig<User, ProjectMemberUserDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Name, src => src.name)
            .Map(dest => dest.Email, src => src.email)
            .Map(dest => dest.Image, src => src.image);

        // Guild mappings (entity Guild -> DTO GuildDto)
        config.NewConfig<Guild, GuildDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Name, src => src.name)
            .Map(dest => dest.Slug, src => src.slug)
            .Map(dest => dest.Description, src => src.description)
            .Map(dest => dest.Website, src => src.website)
            .Map(dest => dest.Email, src => src.email)
            .Map(dest => dest.Phone, src => src.phone)
            .Map(dest => dest.Address, src => src.address)
            .Map(dest => dest.Logo, src => src.logo)
            .Map(dest => dest.Portfolio, src => src.portfolio)
            .Map(dest => dest.Specialties, src => src.specialties)
            .Map(dest => dest.IsVerified, src => src.isVerified)
            .Map(dest => dest.Rating, src => src.rating)
            .Map(dest => dest.ReviewsCount, src => src.reviewsCount)
            .Map(dest => dest.ProjectsCount, src => src.projectsCount)
            .Map(dest => dest.CreatedAt, src => src.createdAt)
            .Map(dest => dest.UpdatedAt, src => src.updatedAt)
            .Map(dest => dest.OwnerId, src => src.ownerId);
        config.NewConfig<GuildMember, GuildMemberDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.GuildId, src => src.guildId)
            .Map(dest => dest.UserId, src => src.userId)
            .Map(dest => dest.Role, src => src.role)
            .Map(dest => dest.JoinedAt, src => src.joinedAt);
        config.NewConfig<GuildReview, GuildReviewDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.GuildId, src => src.guildId)
            .Map(dest => dest.ProjectId, src => src.projectId)
            .Map(dest => dest.UserId, src => src.userId)
            .Map(dest => dest.Rating, src => src.rating)
            .Map(dest => dest.Comment, src => src.comment)
            .Map(dest => dest.CreatedAt, src => src.createdAt);

        // Post / feed
        config.NewConfig<Post, PostDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.AuthorId, src => src.authorId)
            .Map(dest => dest.ProjectId, src => src.projectId)
            .Map(dest => dest.GuildId, src => src.guildId)
            .Map(dest => dest.Type, src => src.type.ToString())
            .Map(dest => dest.Visibility, src => src.visibility.ToString())
            .Map(dest => dest.Title, src => src.title)
            .Map(dest => dest.Content, src => src.content)
            .Map(dest => dest.Metadata, src => src.metadata)
            .Map(dest => dest.Hashtags, src => src.hashtags)
            .Map(dest => dest.TaskId, src => src.taskId)
            .Map(dest => dest.LikesCount, src => src.likesCount)
            .Map(dest => dest.CommentsCount, src => src.commentsCount)
            .Map(dest => dest.SharesCount, src => src.sharesCount)
            .Map(dest => dest.ViewsCount, src => src.viewsCount)
            .Map(dest => dest.IsPinned, src => src.isPinned)
            .Map(dest => dest.CreatedAt, src => src.createdAt)
            .Map(dest => dest.UpdatedAt, src => src.updatedAt);
        config.NewConfig<PostComment, PostCommentDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.PostId, src => src.postId)
            .Map(dest => dest.AuthorId, src => src.authorId)
            .Map(dest => dest.ParentId, src => src.parentId)
            .Map(dest => dest.Content, src => src.content)
            .Map(dest => dest.LikesCount, src => src.likesCount)
            .Map(dest => dest.CreatedAt, src => src.createdAt)
            .Map(dest => dest.UpdatedAt, src => src.updatedAt);

        // Product mappings (entity Product -> DTO ProductDto)
        config.NewConfig<Product, ProductDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.ProjectId, src => src.projectId)
            .Map(dest => dest.Name, src => src.name)
            .Map(dest => dest.Description, src => src.description)
            .Map(dest => dest.Sku, src => src.sku)
            .Map(dest => dest.Price, src => src.price)
            .Map(dest => dest.Cost, src => src.cost)
            .Map(dest => dest.Category, src => src.category)
            .Map(dest => dest.IsActive, src => src.isActive)
            .Map(dest => dest.CreatedAt, src => src.createdAt)
            .Map(dest => dest.UpdatedAt, src => src.updatedAt)
            .Map(dest => dest.UserId, src => src.userId);

        // Task mappings (entity ProjectTask -> DTO TaskDto)
        config.NewConfig<ProjectTask, TaskDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.ProjectId, src => src.projectId)
            .Map(dest => dest.PbiId, src => src.pbiId)
            .Map(dest => dest.Title, src => src.title)
            .Map(dest => dest.Description, src => src.description)
            .Map(dest => dest.Status, src => src.status)
            .Map(dest => dest.Priority, src => src.priority)
            .Map(dest => dest.TaskType, src => src.taskType)
            .Map(dest => dest.EstimatedHours, src => src.estimatedHours)
            .Map(dest => dest.ActualHours, src => src.actualHours)
            .Map(dest => dest.EquityReward, src => src.equityReward)
            .Map(dest => dest.EscrowStatus, src => src.escrowStatus)
            .Map(dest => dest.DueDate, src => src.dueDate)
            .Map(dest => dest.CompletedAt, src => src.completedAt)
            .Map(dest => dest.CreatedAt, src => src.createdAt)
            .Map(dest => dest.UpdatedAt, src => src.updatedAt)
            .Map(dest => dest.AssignedToId, src => src.assignedToId)
            .Ignore(dest => dest.AssignedTo)
            .Ignore(dest => dest.Project)
            .Ignore(dest => dest.HasAgreement)
            .Ignore(dest => dest.HelpWanted);
        config.NewConfig<User, TaskUserDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Name, src => src.name)
            .Map(dest => dest.Image, src => src.image);
        config.NewConfig<Project, TaskProjectDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Title, src => src.title)
            .Map(dest => dest.Slug, src => src.slug);

        // Event mappings (entity Event -> DTO EventDto)
        config.NewConfig<Event, EventDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Title, src => src.title)
            .Map(dest => dest.Slug, src => src.slug)
            .Map(dest => dest.Description, src => src.description)
            .Map(dest => dest.Type, src => src.type)
            .Map(dest => dest.Visibility, src => src.visibility)
            .Map(dest => dest.Status, src => src.status)
            .Map(dest => dest.Location, src => src.location)
            .Map(dest => dest.LocationUrl, src => src.locationUrl)
            .Map(dest => dest.IsOnline, src => src.isOnline)
            .Map(dest => dest.MeetingUrl, src => src.meetingUrl)
            .Map(dest => dest.Timezone, src => src.timezone)
            .Map(dest => dest.StartDate, src => src.startDate)
            .Map(dest => dest.EndDate, src => src.endDate)
            .Map(dest => dest.MaxAttendees, src => src.maxAttendees)
            .Map(dest => dest.AttendeesCount, src => src.attendeesCount)
            .Map(dest => dest.CoverImage, src => src.coverImage)
            .Map(dest => dest.CreatedAt, src => src.createdAt)
            .Map(dest => dest.UpdatedAt, src => src.updatedAt)
            .Map(dest => dest.OrganizerId, src => src.organizerId)
            .Map(dest => dest.ProjectId, src => src.projectId)
            .Map(dest => dest.GuildId, src => src.guildId)
            .Ignore(dest => dest.Organizer);
        config.NewConfig<User, EventOrganizerDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Name, src => src.name)
            .Map(dest => dest.Image, src => src.image);
        config.NewConfig<EventAttendee, EventAttendeeDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.EventId, src => src.eventId)
            .Map(dest => dest.UserId, src => src.userId)
            .Map(dest => dest.Status, src => src.status)
            .Map(dest => dest.RsvpAt, src => src.rsvpAt)
            .Map(dest => dest.AttendedAt, src => src.attendedAt)
            .Map(dest => dest.Notes, src => src.notes)
            .Ignore(dest => dest.User);
        config.NewConfig<User, EventAttendeeUserDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Name, src => src.name)
            .Map(dest => dest.Image, src => src.image);

        // Opportunity mappings (entity Opportunity -> DTO OpportunityDto)
        config.NewConfig<Opportunity, OpportunityDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Title, src => src.title)
            .Map(dest => dest.Slug, src => src.slug)
            .Map(dest => dest.Description, src => src.description)
            .Map(dest => dest.Type, src => src.type)
            .Map(dest => dest.Status, src => src.status)
            .Map(dest => dest.ExperienceLevel, src => src.experienceLevel)
            .Map(dest => dest.Requirements, src => src.requirements)
            .Map(dest => dest.Skills, src => src.skills)
            .Map(dest => dest.Benefits, src => src.benefits)
            .Map(dest => dest.Location, src => src.location)
            .Map(dest => dest.IsRemote, src => src.isRemote)
            .Map(dest => dest.Compensation, src => src.compensation)
            .Map(dest => dest.CompensationDetails, src => src.compensationDetails)
            .Map(dest => dest.Deadline, src => src.deadline)
            .Map(dest => dest.MaxApplications, src => src.maxApplications)
            .Map(dest => dest.ApplicationsCount, src => src.applicationsCount)
            .Map(dest => dest.CoverImage, src => src.coverImage)
            .Map(dest => dest.CreatedAt, src => src.createdAt)
            .Map(dest => dest.UpdatedAt, src => src.updatedAt)
            .Map(dest => dest.ClosedAt, src => src.closedAt)
            .Map(dest => dest.PosterId, src => src.posterId)
            .Map(dest => dest.GuildId, src => src.guildId)
            .Map(dest => dest.ProjectId, src => src.projectId)
            .Map(dest => dest.TaskId, src => src.taskId)
            .Map(dest => dest.ProjectRole, src => src.projectRole)
            .Ignore(dest => dest.Poster);
        config.NewConfig<User, OpportunityPosterDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Name, src => src.name)
            .Map(dest => dest.Image, src => src.image);
        config.NewConfig<OpportunityApplication, OpportunityApplicationDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.OpportunityId, src => src.opportunityId)
            .Map(dest => dest.ApplicantId, src => src.applicantId)
            .Map(dest => dest.CoverLetter, src => src.coverLetter)
            .Map(dest => dest.Portfolio, src => src.portfolio)
            .Map(dest => dest.AdditionalInfo, src => src.additionalInfo)
            .Map(dest => dest.Status, src => src.status)
            .Map(dest => dest.ReviewNotes, src => src.reviewNotes)
            .Map(dest => dest.AppliedAt, src => src.appliedAt)
            .Map(dest => dest.ReviewedAt, src => src.reviewedAt)
            .Ignore(dest => dest.Applicant);
        config.NewConfig<User, OpportunityApplicationApplicantDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Name, src => src.name)
            .Map(dest => dest.Image, src => src.image)
            .Map(dest => dest.Email, src => src.email);

        // Opportunity Update mappings
        config.NewConfig<OpportunityUpdate, OpportunityUpdateDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.OpportunityId, src => src.opportunityId)
            .Map(dest => dest.UserId, src => src.userId)
            .Map(dest => dest.Title, src => src.title)
            .Map(dest => dest.Content, src => src.content)
            .Map(dest => dest.Images, src => src.images)
            .Map(dest => dest.CreatedAt, src => src.createdAt)
            .Ignore(dest => dest.User);
        config.NewConfig<User, OpportunityUpdateAuthorDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Name, src => src.name)
            .Map(dest => dest.Image, src => src.image);

        // Opportunity Comment mappings
        config.NewConfig<OpportunityComment, OpportunityCommentDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.OpportunityId, src => src.opportunityId)
            .Map(dest => dest.UserId, src => src.userId)
            .Map(dest => dest.Content, src => src.content)
            .Map(dest => dest.ParentId, src => src.parentId)
            .Map(dest => dest.CreatedAt, src => src.createdAt)
            .Map(dest => dest.UpdatedAt, src => src.updatedAt)
            .Ignore(dest => dest.Author);
        config.NewConfig<User, OpportunityCommentAuthorDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Name, src => src.name)
            .Map(dest => dest.Image, src => src.image)
            .Map(dest => dest.Email, src => src.email);

        // Opportunity Bid mappings
        config.NewConfig<OpportunityBid, OpportunityBidDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.OpportunityId, src => src.opportunityId)
            .Map(dest => dest.BidderId, src => src.bidderId)
            .Map(dest => dest.GuildId, src => src.guildId)
            .Map(dest => dest.ProposedAmount, src => src.proposedAmount)
            .Map(dest => dest.Proposal, src => src.proposal)
            .Map(dest => dest.EstimatedHours, src => src.estimatedHours)
            .Map(dest => dest.Timeline, src => src.timeline)
            .Map(dest => dest.Deliverables, src => src.deliverables)
            .Map(dest => dest.Status, src => src.status)
            .Map(dest => dest.CreatedAt, src => src.createdAt)
            .Map(dest => dest.ReviewedAt, src => src.reviewedAt)
            .Ignore(dest => dest.Bidder)
            .Ignore(dest => dest.Guild);
        config.NewConfig<User, OpportunityBidBidderDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Name, src => src.name)
            .Map(dest => dest.Image, src => src.image)
            .Map(dest => dest.Email, src => src.email);
        config.NewConfig<Guild, OpportunityBidGuildDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Name, src => src.name)
            .Map(dest => dest.Logo, src => src.logo)
            .Map(dest => dest.Slug, src => src.slug);

        // Governance mappings (entity Proposal -> DTO ProposalDto)
        config.NewConfig<Proposal, ProposalDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.ProjectId, src => src.projectId)
            .Map(dest => dest.CreatorId, src => src.creatorId)
            .Map(dest => dest.Type, src => src.type)
            .Map(dest => dest.Title, src => src.title)
            .Map(dest => dest.Description, src => src.description)
            .Map(dest => dest.Options, src => src.options)
            .Map(dest => dest.Quorum, src => src.quorum)
            .Map(dest => dest.Threshold, src => src.threshold)
            .Map(dest => dest.Status, src => src.status)
            .Map(dest => dest.VotingStart, src => src.votingStart)
            .Map(dest => dest.VotingEnd, src => src.votingEnd)
            .Map(dest => dest.ExecutionDelay, src => src.executionDelay)
            .Map(dest => dest.CreatedAt, src => src.createdAt)
            .Map(dest => dest.UpdatedAt, src => src.updatedAt)
            .Ignore(dest => dest.Creator)
            .Ignore(dest => dest.Project)
            .Ignore(dest => dest.VotesCount)
            .Ignore(dest => dest.TotalVotingPower);
        config.NewConfig<User, ProposalCreatorDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Name, src => src.name)
            .Map(dest => dest.Image, src => src.image);
        config.NewConfig<Project, ProposalProjectDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Title, src => src.title)
            .Map(dest => dest.Slug, src => src.slug);
        config.NewConfig<Vote, VoteDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.ProposalId, src => src.proposalId)
            .Map(dest => dest.VoterId, src => src.voterId)
            .Map(dest => dest.Choice, src => src.choice)
            .Map(dest => dest.Weight, src => src.weight)
            .Map(dest => dest.Reason, src => src.reason)
            .Map(dest => dest.TxHash, src => src.txHash)
            .Map(dest => dest.CreatedAt, src => src.createdAt)
            .Ignore(dest => dest.Voter);
        config.NewConfig<User, VoteUserDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Name, src => src.name)
            .Map(dest => dest.Image, src => src.image);
        config.NewConfig<ProposalComment, ProposalCommentDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.ProposalId, src => src.proposalId)
            .Map(dest => dest.UserId, src => src.userId)
            .Map(dest => dest.Content, src => src.content)
            .Map(dest => dest.ParentId, src => src.parentId)
            .Map(dest => dest.CreatedAt, src => src.createdAt)
            .Map(dest => dest.UpdatedAt, src => src.updatedAt)
            .Ignore(dest => dest.User)
            .Ignore(dest => dest.Replies);
        config.NewConfig<User, ProposalCommentUserDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Name, src => src.name)
            .Map(dest => dest.Image, src => src.image);

        // UserFollow mappings
        config.NewConfig<UserFollow, UserFollowDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.FollowerId, src => src.followerId)
            .Map(dest => dest.FollowingId, src => src.followingId)
            .Map(dest => dest.CreatedAt, src => src.createdAt);

        // Wallet mappings
        config.NewConfig<Wallet, WalletDto>();

        // Task Escrow mappings
        config.NewConfig<TaskEscrow, TaskEscrowDto>();

        // Gamification extended mappings
        config.NewConfig<UserStreak, UserStreakDto>();
        config.NewConfig<Referral, ReferralDto>();

        // Leaderboard mappings
        config.NewConfig<Leaderboard, LeaderboardDto>();
        config.NewConfig<LeaderboardEntry, LeaderboardEntryDto>();

        // XP Event mappings
        config.NewConfig<XPEvent, XPEventDto>();

        // Achievement mappings
        config.NewConfig<Achievement, AchievementDto>();
        config.NewConfig<UserAchievement, UserAchievementDto>();

        // Notification & Activity mappings
        config.NewConfig<Notification, NotificationDto>();
        config.NewConfig<Activity, ActivityDto>();

        // Membership Credential mappings
        config.NewConfig<MembershipCredential, MembershipCredentialDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.ProjectId, src => src.projectId)
            .Map(dest => dest.GuildId, src => src.guildId)
            .Map(dest => dest.UserId, src => src.userId)
            .Map(dest => dest.AssetId, src => src.assetId)
            .Map(dest => dest.Status, src => src.status.ToString())
            .Map(dest => dest.IsTransferable, src => src.isTransferable)
            .Map(dest => dest.Tier, src => src.tier != null ? src.tier.ToString() : null)
            .Map(dest => dest.GrantedVia, src => src.grantedVia.ToString())
            .Map(dest => dest.GrantedByProposalId, src => src.grantedByProposalId)
            .Map(dest => dest.MetadataUri, src => src.metadataUri)
            .Map(dest => dest.MintTxHash, src => src.mintTxHash)
            .Map(dest => dest.RevokeTxHash, src => src.revokeTxHash)
            .Map(dest => dest.MintedAt, src => src.mintedAt)
            .Map(dest => dest.RevokedAt, src => src.revokedAt)
            // The issuance-path flag the member-facing screens gate their proof copy on. Read off
            // issuedThroughNodeAt, which the node path alone stamps — NEVER off mintTxHash or
            // assetId, which the removed platform-held mint also wrote and the simulated provider
            // fabricated. See MembershipCredentialDto.IsIndependentlyVerifiable.
            .Map(dest => dest.IsIndependentlyVerifiable, src => src.issuedThroughNodeAt != null)
            .Map(dest => dest.CreatedAt, src => src.createdAt)
            .Map(dest => dest.UpdatedAt, src => src.updatedAt);

        // Exchange mappings
        // TODO: LiquidityPool/LiquidityProvider DTOs not yet implemented
        // CreateMap<LiquidityPool, LiquidityPoolDto>();
        // CreateMap<LiquidityProvider, LiquidityProviderDto>();

        // Attachment mappings
        config.NewConfig<Attachment, AttachmentDto>()
            .Map(dest => dest.BucketPath, src => src.bucketPath)
            .Map(dest => dest.UploadedById, src => src.uploadedById)
            .Map(dest => dest.CreatedAt, src => src.createdAt)
            .Map(dest => dest.LastUsedAt, src => src.lastUsedAt)
            .Map(dest => dest.Type, src => src.type);

        // KYC mappings
        config.NewConfig<KycSubmission, KycSubmissionDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.UserId, src => src.userId)
            .Map(dest => dest.Provider, src => src.provider)
            .Map(dest => dest.Status, src => src.status)
            .Map(dest => dest.ReviewerId, src => src.reviewerId)
            .Map(dest => dest.ReviewNotes, src => src.reviewNotes)
            .Map(dest => dest.RejectionReason, src => src.rejectionReason)
            .Map(dest => dest.ProviderSessionId, src => src.providerSessionId)
            .Map(dest => dest.SubmittedAt, src => src.submittedAt)
            .Map(dest => dest.ReviewedAt, src => src.reviewedAt)
            .Map(dest => dest.ExpiresAt, src => src.expiresAt)
            .Map(dest => dest.CreatedAt, src => src.createdAt)
            .Map(dest => dest.UpdatedAt, src => src.updatedAt)
            .Ignore(dest => dest.Documents);
        config.NewConfig<KycDocument, KycDocumentDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.SubmissionId, src => src.submissionId)
            .Map(dest => dest.Type, src => src.type)
            .Map(dest => dest.FileUrl, src => src.fileUrl)
            .Map(dest => dest.FileName, src => src.fileName)
            .Map(dest => dest.MimeType, src => src.mimeType)
            .Map(dest => dest.FileSizeBytes, src => src.fileSizeBytes)
            .Map(dest => dest.Metadata, src => src.metadata)
            .Map(dest => dest.CreatedAt, src => src.createdAt);

        // Project Invitation mappings
        config.NewConfig<ProjectInvitation, ProjectInvitationDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.ProjectId, src => src.projectId)
            .Map(dest => dest.InvitedById, src => src.invitedById)
            .Map(dest => dest.InvitedUserId, src => src.invitedUserId)
            .Map(dest => dest.InvitedEmail, src => src.invitedEmail)
            .Map(dest => dest.Role, src => src.role.ToString())
            .Map(dest => dest.Message, src => src.message)
            .Map(dest => dest.Status, src => src.status.ToString())
            .Map(dest => dest.Token, src => src.token)
            .Map(dest => dest.CreatedAt, src => src.createdAt)
            .Map(dest => dest.ExpiresAt, src => src.expiresAt)
            .Map(dest => dest.RespondedAt, src => src.respondedAt)
            .Ignore(dest => dest.InvitedUser)
            .Ignore(dest => dest.InvitedBy)
            .Ignore(dest => dest.Project);

        // Tokenomics mappings
        config.NewConfig<ProjectTokenConfig, ProjectTokenConfigDto>();
        config.NewConfig<TokenAllocation, TokenAllocationDto>();
        config.NewConfig<TokenBalance, TokenBalanceDto>();
        config.NewConfig<PayoutRequest, PayoutRequestDto>();
        config.NewConfig<PlatformTreasury, TreasuryStatusDto>();
        config.NewConfig<PlatformTreasuryTransaction, PlatformTreasuryTransactionDto>();
        config.NewConfig<ProjectInvestment, ProjectInvestmentDto>();

    }
}
