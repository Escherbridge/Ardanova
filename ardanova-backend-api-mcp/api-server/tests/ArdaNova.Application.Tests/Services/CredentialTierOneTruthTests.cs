namespace ArdaNova.Application.Tests.Services;

using System.Linq.Expressions;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using FluentAssertions;
using Moq;

/// <summary>
/// One tier, two columns, and the seams that keep them equal.
/// </summary>
/// <remarks>
/// The defect these cover is a "portable credential" whose tier could disagree with the tier the
/// member actually earned. Each test pins one of the three write seams: the grant that stamps it,
/// the tier update that refuses to contradict it, and the XP award that re-stamps it.
/// </remarks>
public class CredentialTierOneTruthTests
{
    private readonly Mock<IRepository<MembershipCredential>> _credentials = new();
    private readonly Mock<IRepository<XPEvent>> _xpEvents = new();
    private readonly Mock<IRepository<User>> _users = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IKycGateService> _kycGate = new();

    public CredentialTierOneTruthTests()
    {
        _kycGate.Setup(gate => gate.RequireProAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
        _unitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _credentials.Setup(repo => repo.UpdateAsync(It.IsAny<MembershipCredential>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _users.Setup(repo => repo.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mapper.Setup(mapper => mapper.Map<MembershipCredentialDto>(It.IsAny<MembershipCredential>()))
            .Returns((MembershipCredential credential) => new MembershipCredentialDto
            {
                Id = credential.id,
                ProjectId = credential.projectId,
                UserId = credential.userId,
                Status = credential.status.ToString(),
                Tier = credential.tier?.ToString(),
                GrantedVia = credential.grantedVia.ToString(),
                CreatedAt = credential.createdAt,
                UpdatedAt = credential.updatedAt
            });
    }

    // The real XP service, not a stub: these tests are about the two columns agreeing, so both
    // sides must be reading the same derivation rather than two copies of one formula.
    private XPEventService XpService => new(
        _xpEvents.Object,
        _users.Object,
        _unitOfWork.Object,
        _mapper.Object,
        _credentials.Object);

    private MembershipCredentialService CredentialService => new(
        _credentials.Object,
        _unitOfWork.Object,
        _mapper.Object,
        _kycGate.Object,
        _users.Object,
        XpService);

    [Fact]
    public async Task GrantAsync_StampsTheTierTheHolderEarned()
    {
        var userId = Guid.NewGuid().ToString();
        // 6,000 XP is GOLD on the one derivation both sides use.
        GivenUser(userId, totalXP: 6_000, storedTier: UserTier.BRONZE);
        _credentials.Setup(repo => repo.FindAsync(
                It.IsAny<Expression<Func<MembershipCredential, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MembershipCredential>());
        _credentials.Setup(repo => repo.AddAsync(It.IsAny<MembershipCredential>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MembershipCredential credential, CancellationToken _) => credential);

        var result = await CredentialService.GrantAsync(new GrantMembershipCredentialDto
        {
            ProjectId = Guid.NewGuid().ToString(),
            UserId = userId,
            GrantedVia = "FOUNDER"
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be("GOLD");
        _credentials.Verify(repo => repo.AddAsync(
            It.Is<MembershipCredential>(credential => credential.tier == UserTier.GOLD),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTierAsync_RefusesATierTheHolderHasNotEarned()
    {
        var userId = Guid.NewGuid().ToString();
        var credentialId = Guid.NewGuid().ToString();
        // 1,200 XP is SILVER; the caller is asking for DIAMOND.
        GivenUser(userId, totalXP: 1_200, storedTier: UserTier.SILVER);
        GivenCredential(credentialId, userId, MembershipCredentialStatus.ACTIVE, UserTier.SILVER);

        var result = await CredentialService.UpdateTierAsync(
            credentialId, new UpdateCredentialTierDto { Tier = "DIAMOND" });

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Conflict);
        result.Error.Should().Contain("SILVER");
        _credentials.Verify(repo => repo.UpdateAsync(
            It.IsAny<MembershipCredential>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTierAsync_AcceptsTheEarnedTier()
    {
        var userId = Guid.NewGuid().ToString();
        var credentialId = Guid.NewGuid().ToString();
        GivenUser(userId, totalXP: 5_000, storedTier: UserTier.GOLD);
        GivenCredential(credentialId, userId, MembershipCredentialStatus.ACTIVE, UserTier.SILVER);

        var result = await CredentialService.UpdateTierAsync(
            credentialId, new UpdateCredentialTierDto { Tier = "GOLD" });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tier.Should().Be("GOLD");
    }

    [Fact]
    public async Task AwardXPAsync_RestampsTheHoldersLiveCredentials()
    {
        var userId = Guid.NewGuid().ToString();
        // 990 + 20 crosses into SILVER, so every live credential must move with it.
        GivenUser(userId, totalXP: 990, storedTier: UserTier.BRONZE);
        var live = NewCredential(Guid.NewGuid().ToString(), userId, MembershipCredentialStatus.ACTIVE, UserTier.BRONZE);
        _credentials.Setup(repo => repo.FindAsync(
                It.IsAny<Expression<Func<MembershipCredential, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MembershipCredential> { live });
        _xpEvents.Setup(repo => repo.AddAsync(It.IsAny<XPEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((XPEvent entity, CancellationToken _) => entity);
        _mapper.Setup(mapper => mapper.Map<XPEventDto>(It.IsAny<XPEvent>())).Returns(new XPEventDto());

        var result = await XpService.AwardXPAsync(new AwardXPDto
        {
            UserId = userId,
            EventType = XPEventType.TASK_COMPLETED,
            Amount = 20,
            Source = "task"
        });

        result.IsSuccess.Should().BeTrue();
        live.tier.Should().Be(UserTier.SILVER, "the credential mirrors the tier the member now holds");
        _credentials.Verify(repo => repo.UpdateAsync(live, It.IsAny<CancellationToken>()), Times.Once);
        _users.Verify(repo => repo.UpdateAsync(
            It.Is<User>(user => user.tier == UserTier.SILVER), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AwardXPAsync_LeavesWithdrawnCredentialsOutOfTheSync()
    {
        var userId = Guid.NewGuid().ToString();
        GivenUser(userId, totalXP: 990, storedTier: UserTier.BRONZE);
        Expression<Func<MembershipCredential, bool>>? captured = null;
        _credentials.Setup(repo => repo.FindAsync(
                It.IsAny<Expression<Func<MembershipCredential, bool>>>(), It.IsAny<CancellationToken>()))
            .Callback((Expression<Func<MembershipCredential, bool>> predicate, CancellationToken _) => captured = predicate)
            .ReturnsAsync(new List<MembershipCredential>());
        _xpEvents.Setup(repo => repo.AddAsync(It.IsAny<XPEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((XPEvent entity, CancellationToken _) => entity);
        _mapper.Setup(mapper => mapper.Map<XPEventDto>(It.IsAny<XPEvent>())).Returns(new XPEventDto());

        await XpService.AwardXPAsync(new AwardXPDto
        {
            UserId = userId,
            EventType = XPEventType.TASK_COMPLETED,
            Amount = 20,
            Source = "task"
        });

        // The filter itself is the control: a withdrawn credential is a historical record and
        // must not be rewritten by a later award.
        captured.Should().NotBeNull();
        var matches = captured!.Compile();
        matches(NewCredential("a", userId, MembershipCredentialStatus.ACTIVE, UserTier.BRONZE)).Should().BeTrue();
        matches(NewCredential("b", userId, MembershipCredentialStatus.SUSPENDED, UserTier.BRONZE)).Should().BeTrue();
        matches(NewCredential("c", userId, MembershipCredentialStatus.REVOKED, UserTier.BRONZE)).Should().BeFalse();
        matches(NewCredential("d", "someone-else", MembershipCredentialStatus.ACTIVE, UserTier.BRONZE)).Should().BeFalse();
    }

    private void GivenUser(string userId, int totalXP, UserTier storedTier)
        => _users.Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                id = userId,
                totalXP = totalXP,
                tier = storedTier
            });

    private void GivenCredential(
        string credentialId, string userId, MembershipCredentialStatus status, UserTier? tier)
        => _credentials.Setup(repo => repo.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewCredential(credentialId, userId, status, tier));

    private static MembershipCredential NewCredential(
        string credentialId, string userId, MembershipCredentialStatus status, UserTier? tier)
        => new()
        {
            id = credentialId,
            userId = userId,
            projectId = Guid.NewGuid().ToString(),
            status = status,
            tier = tier,
            isTransferable = false,
            grantedVia = MembershipGrantType.FOUNDER,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
}
