namespace ArdaNova.Domain.Policies;

using ArdaNova.Domain.Models.Enums;

/// <summary>Defines when a project-token holder class may use its balance as liquidity.</summary>
public static class TokenLiquidityPolicy
{
    public static bool IsLiquid(TokenHolderClass holderClass, ProjectGateStatus gateStatus) =>
        (holderClass, gateStatus) switch
        {
            (TokenHolderClass.CONTRIBUTOR, ProjectGateStatus.ACTIVE) => true,
            (TokenHolderClass.CONTRIBUTOR, ProjectGateStatus.SUCCEEDED) => true,
            (TokenHolderClass.INVESTOR, ProjectGateStatus.SUCCEEDED) => true,
            (TokenHolderClass.FOUNDER, ProjectGateStatus.SUCCEEDED) => true,
            _ => false,
        };
}
