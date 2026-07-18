namespace ArdaNova.Domain.Policies;

using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Domain.ValueObjects;

/// <summary>Validates immutable utility-token and three-leg exchange authorization facts.</summary>
public static class FinancialAuthorizationPolicy
{
    /// <summary>Validates a project utility asset and its active policy against a token configuration.</summary>
    public static bool TryValidateUtilityAuthorization(
        ProjectTokenConfig? config,
        AssetDefinition? asset,
        ProjectTokenPolicy? policy,
        DateTime asOf,
        out string error)
    {
        if (config is null || asset is null || policy is null)
        {
            error = "A project token configuration, asset definition, and utility policy are required.";
            return false;
        }

        if (!string.Equals(config.assetDefinitionId, asset.id, StringComparison.Ordinal)
            || !string.Equals(policy.assetDefinitionId, asset.id, StringComparison.Ordinal)
            || !string.Equals(policy.projectId, config.projectId, StringComparison.Ordinal))
        {
            error = "The utility policy, asset definition, and project token configuration must belong to the same project asset.";
            return false;
        }

        if (!AssetDefinitionPolicy.IsValid(asset)
            || asset.kind != AssetDefinitionKind.PROJECT_UTILITY
            || asset.scale != config.assetScale)
        {
            error = "The project utility asset has an invalid canonical identity or scale.";
            return false;
        }

        if (!IsEffectiveUtilityPolicy(policy, asOf))
        {
            error = "The project utility policy is not active for this authorization.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <summary>Validates an unexpired project-utility to ARDA to project-utility quote.</summary>
    public static bool TryValidateSwapQuote(
        SwapQuote? quote,
        AssetDefinition? sourceAsset,
        AssetDefinition? ardaAsset,
        AssetDefinition? targetAsset,
        ProjectTokenPolicy? sourcePolicy,
        ProjectTokenPolicy? targetPolicy,
        DateTime asOf,
        out string error)
    {
        if (quote is null || sourceAsset is null || ardaAsset is null || targetAsset is null
            || sourcePolicy is null || targetPolicy is null)
        {
            error = "A complete three-leg quote requires all assets and both project utility policies.";
            return false;
        }

        if (quote.expiresAt <= asOf || !HasHash(quote.termsHash)
            || string.IsNullOrWhiteSpace(quote.liquiditySnapshot)
            || string.IsNullOrWhiteSpace(quote.gateDecisionSnapshot))
        {
            error = "The quote must have unexpired, immutable terms and gate/liquidity evidence.";
            return false;
        }

        if (!HasExpectedQuoteReferences(quote, sourceAsset, ardaAsset, targetAsset, sourcePolicy, targetPolicy))
        {
            error = "The quote references do not match its authoritative assets and policies.";
            return false;
        }

        if (!AssetDefinitionPolicy.IsValid(sourceAsset)
            || !AssetDefinitionPolicy.IsValid(ardaAsset)
            || !AssetDefinitionPolicy.IsValid(targetAsset)
            || sourceAsset.kind != AssetDefinitionKind.PROJECT_UTILITY
            || ardaAsset.kind != AssetDefinitionKind.ECOSYSTEM_UTILITY
            || targetAsset.kind != AssetDefinitionKind.PROJECT_UTILITY
            || sourceAsset.id == targetAsset.id
            || sourceAsset.id == ardaAsset.id
            || ardaAsset.id == targetAsset.id)
        {
            error = "A quote must contain distinct project utility source/target assets and an ecosystem utility intermediary.";
            return false;
        }

        if (!IsEffectiveUtilityPolicy(sourcePolicy, asOf)
            || !IsEffectiveUtilityPolicy(targetPolicy, asOf)
            || !FixedScaleAmount.TryFromPositiveBaseUnits(quote.sourceAmountAtoms, sourceAsset.scale, out _)
            || !FixedScaleAmount.TryFromPositiveBaseUnits(quote.ardaAmountAtoms, ardaAsset.scale, out _)
            || !FixedScaleAmount.TryFromPositiveBaseUnits(quote.targetAmountAtoms, targetAsset.scale, out _))
        {
            error = "The quote contains an inactive utility policy or a non-canonical leg amount.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <summary>Validates one ordered settlement leg against its immutable asset scale.</summary>
    public static bool TryValidateSettlementLeg(
        EconomicSettlementLeg? leg,
        AssetDefinition? asset,
        out string error)
    {
        if (leg is null || asset is null
            || !string.Equals(leg.assetDefinitionId, asset.id, StringComparison.Ordinal)
            || leg.position < 0
            || !AssetDefinitionPolicy.IsValid(asset)
            || !FixedScaleAmount.TryFromPositiveBaseUnits(leg.amountAtoms, asset.scale, out _))
        {
            error = "A settlement leg requires a non-negative position, canonical asset, and positive canonical atom amount.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <summary>Validates funding payment facts and exactly one authorized utility or rights award path.</summary>
    public static bool TryValidateFundingAuthorization(
        FundingIntent? intent,
        ProjectTokenConfig? config,
        AssetDefinition? paymentAsset,
        AssetDefinition? awardAsset,
        ProjectTokenPolicy? utilityPolicy,
        EquityOrRedemptionRightPolicy? rightsPolicy,
        EligibilityDecision? eligibilityDecision,
        DateTime asOf,
        out string error)
    {
        if (intent is null || config is null || paymentAsset is null || awardAsset is null
            || !string.Equals(intent.projectId, config.projectId, StringComparison.Ordinal)
            || !string.Equals(intent.projectTokenConfigId, config.id, StringComparison.Ordinal)
            || !string.Equals(intent.paymentAssetDefinitionId, paymentAsset.id, StringComparison.Ordinal)
            || !string.Equals(intent.awardAssetDefinitionId, awardAsset.id, StringComparison.Ordinal)
            || string.Equals(paymentAsset.id, awardAsset.id, StringComparison.Ordinal))
        {
            error = "Funding must reference its project configuration and distinct payment and award assets.";
            return false;
        }

        if (!AssetDefinitionPolicy.IsValid(paymentAsset)
            || !AssetDefinitionPolicy.IsValid(awardAsset)
            || paymentAsset.kind != AssetDefinitionKind.FIAT
            || !string.Equals(intent.currencyCode, paymentAsset.canonicalAssetId, StringComparison.OrdinalIgnoreCase)
            || intent.scale != paymentAsset.scale
            || intent.amount <= 0m
            || !FixedScaleAmount.TryFromPositiveDecimal(intent.amount, paymentAsset.scale, out _)
            || !FixedScaleAmount.TryFromPositiveBaseUnits(intent.awardAmountAtoms, awardAsset.scale, out _))
        {
            error = "Funding payment currency/scale or award atoms do not match their canonical assets.";
            return false;
        }

        if (utilityPolicy is not null)
        {
            if (rightsPolicy is not null || eligibilityDecision is not null
                || !string.IsNullOrWhiteSpace(intent.equityOrRedemptionRightPolicyId)
                || !string.IsNullOrWhiteSpace(intent.eligibilityDecisionId)
                || !string.Equals(intent.projectTokenPolicyId, utilityPolicy.id, StringComparison.Ordinal))
            {
                error = "A utility funding award cannot carry a separate rights policy or eligibility decision.";
                return false;
            }

            if (!TryValidateUtilityAuthorization(config, awardAsset, utilityPolicy, asOf, out error))
                return false;

            error = string.Empty;
            return true;
        }

        if (rightsPolicy is null || eligibilityDecision is null
            || !string.IsNullOrWhiteSpace(intent.projectTokenPolicyId)
            || !IsEffectiveRightsPolicy(rightsPolicy, asOf)
            || !string.Equals(rightsPolicy.projectId, intent.projectId, StringComparison.Ordinal)
            || !string.Equals(intent.equityOrRedemptionRightPolicyId, rightsPolicy.id, StringComparison.Ordinal)
            || !string.Equals(intent.eligibilityDecisionId, eligibilityDecision.id, StringComparison.Ordinal)
            || !string.Equals(eligibilityDecision.equityOrRedemptionRightPolicyId, rightsPolicy.id, StringComparison.Ordinal)
            || !string.Equals(eligibilityDecision.userId, intent.funderUserId, StringComparison.Ordinal)
            || eligibilityDecision.status != EligibilityDecisionStatus.APPROVED
            || (eligibilityDecision.expiresAt is not null && eligibilityDecision.expiresAt <= asOf)
            || !MatchesRightAsset(awardAsset.kind, rightsPolicy.kind))
        {
            error = "A rights funding award requires a current approved decision for the funder and exact rights policy.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <summary>Validates that only the quoted actor can accept its still-live exchange quote.</summary>
    public static bool TryValidateSwapOrderAcceptance(
        SwapOrder? order,
        SwapQuote? quote,
        DateTime asOf,
        out string error)
    {
        if (order is null || quote is null
            || order.status != SwapOrderStatus.DRAFT
            || !string.Equals(order.quoteId, quote.id, StringComparison.Ordinal)
            || !string.Equals(order.actorUserId, quote.actorUserId, StringComparison.Ordinal)
            || quote.expiresAt <= asOf
            || !string.IsNullOrWhiteSpace(order.economicSettlementId))
        {
            error = "Only the quote actor may accept an unexpired draft quote before it is linked to a settlement.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool HasExpectedQuoteReferences(
        SwapQuote quote,
        AssetDefinition sourceAsset,
        AssetDefinition ardaAsset,
        AssetDefinition targetAsset,
        ProjectTokenPolicy sourcePolicy,
        ProjectTokenPolicy targetPolicy)
        => string.Equals(quote.sourceAssetDefinitionId, sourceAsset.id, StringComparison.Ordinal)
           && string.Equals(quote.ardaAssetDefinitionId, ardaAsset.id, StringComparison.Ordinal)
           && string.Equals(quote.targetAssetDefinitionId, targetAsset.id, StringComparison.Ordinal)
           && string.Equals(quote.sourceProjectTokenPolicyId, sourcePolicy.id, StringComparison.Ordinal)
           && string.Equals(quote.targetProjectTokenPolicyId, targetPolicy.id, StringComparison.Ordinal)
           && string.Equals(sourcePolicy.assetDefinitionId, sourceAsset.id, StringComparison.Ordinal)
           && string.Equals(targetPolicy.assetDefinitionId, targetAsset.id, StringComparison.Ordinal);

    private static bool IsEffectiveUtilityPolicy(ProjectTokenPolicy policy, DateTime asOf)
        => policy.version > 0
           && HasHash(policy.termsHash)
           && !string.IsNullOrWhiteSpace(policy.allocationRules)
           && policy.effectiveFrom <= asOf
           && (policy.retiredAt is null || policy.retiredAt > asOf)
           && (policy.retiredAt is null || policy.retiredAt > policy.effectiveFrom);

    private static bool IsEffectiveRightsPolicy(EquityOrRedemptionRightPolicy policy, DateTime asOf)
        => policy.version > 0
           && HasHash(policy.termsHash)
           && !string.IsNullOrWhiteSpace(policy.jurisdiction)
           && !string.IsNullOrWhiteSpace(policy.disclosureVersion)
           && !string.IsNullOrWhiteSpace(policy.eligibilityPolicyVersion)
           && !string.IsNullOrWhiteSpace(policy.termsSnapshot)
           && policy.effectiveFrom <= asOf
           && (policy.retiredAt is null || policy.retiredAt > asOf)
           && (policy.retiredAt is null || policy.retiredAt > policy.effectiveFrom);

    private static bool MatchesRightAsset(AssetDefinitionKind assetKind, EquityOrRedemptionRightKind policyKind)
        => (assetKind, policyKind) is (AssetDefinitionKind.EQUITY_RIGHT, EquityOrRedemptionRightKind.EQUITY)
            or (AssetDefinitionKind.REDEMPTION_RIGHT, EquityOrRedemptionRightKind.REDEMPTION);

    private static bool HasHash(string? value)
        => value?.Length == 64 && value.All(Uri.IsHexDigit);
}
