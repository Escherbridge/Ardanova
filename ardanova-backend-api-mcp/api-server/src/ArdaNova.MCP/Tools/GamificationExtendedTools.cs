namespace ArdaNova.MCP.Tools;

using System.ComponentModel;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ModelContextProtocol.Server;

[McpServerToolType]
public class UserStreakTools
{
    private readonly IUserStreakService _streakService;

    public UserStreakTools(IUserStreakService streakService)
    {
        _streakService = streakService;
    }

    [McpServerTool(Name = "streak_get_by_user_id")]
    [Description("Retrieves the streak record for a user")]
    public async Task<UserStreakDto?> GetStreakByUserId(
        [Description("The user ID")] string userId,
        CancellationToken ct = default)
    {
        var result = await _streakService.GetByUserIdAsync(userId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "streak_record_activity")]
    [Description("Records daily activity for a user and updates their streak")]
    public async Task<UserStreakDto?> RecordActivity(
        [Description("The user ID")] string userId,
        CancellationToken ct = default)
    {
        var result = await _streakService.RecordActivityAsync(userId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "streak_reset")]
    [Description("Resets a user's streak to zero")]
    public async Task<UserStreakDto?> ResetStreak(
        [Description("The user ID")] string userId,
        CancellationToken ct = default)
    {
        var result = await _streakService.ResetStreakAsync(userId, ct);
        return result.IsSuccess ? result.Value : null;
    }
}

[McpServerToolType]
public class ReferralTools
{
    private readonly IReferralService _referralService;

    public ReferralTools(IReferralService referralService)
    {
        _referralService = referralService;
    }

    [McpServerTool(Name = "referral_get_by_id")]
    [Description("Retrieves a referral by its unique identifier")]
    public async Task<ReferralDto?> GetReferralById(
        [Description("The referral ID")] string id,
        CancellationToken ct = default)
    {
        var result = await _referralService.GetByIdAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "referral_get_by_referrer")]
    [Description("Retrieves all referrals made by a user")]
    public async Task<IReadOnlyList<ReferralDto>?> GetReferralsByReferrer(
        [Description("The referrer user ID")] string referrerId,
        CancellationToken ct = default)
    {
        var result = await _referralService.GetByReferrerIdAsync(referrerId, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "referral_get_by_code")]
    [Description("Retrieves a referral by its referral code")]
    public async Task<ReferralDto?> GetReferralByCode(
        [Description("The referral code")] string code,
        CancellationToken ct = default)
    {
        var result = await _referralService.GetByCodeAsync(code, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "referral_complete")]
    [Description("Marks a referral as completed")]
    public async Task<ReferralDto?> CompleteReferral(
        [Description("The referral ID")] string id,
        CancellationToken ct = default)
    {
        var result = await _referralService.CompleteAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    [McpServerTool(Name = "referral_claim_reward")]
    [Description("Claims the reward for a completed referral")]
    public async Task<ReferralDto?> ClaimReferralReward(
        [Description("The referral ID")] string id,
        [Description("XP amount to reward")] int xpAmount,
        [Description("Optional token amount to reward")] decimal? tokenAmount = null,
        CancellationToken ct = default)
    {
        var dto = new ClaimReferralRewardDto
        {
            XpAmount = xpAmount,
            TokenAmount = tokenAmount
        };
        var result = await _referralService.ClaimRewardAsync(id, dto, ct);
        return result.IsSuccess ? result.Value : null;
    }
}
