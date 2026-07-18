namespace ArdaNova.Application.Tests.Policies;

using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Domain.Policies;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

public class FinancialEngineDomainPersistenceContractTests
{
    [Fact]
    public void AssetDefinition_RequiresCanonicalIdentityAndFixedScale()
    {
        var asset = ValidAsset();

        AssetDefinitionPolicy.IsValid(asset).Should().BeTrue();
        RequiredPropertyNames(typeof(AssetDefinition)).Should().Contain(new[]
        {
            nameof(AssetDefinition.kind),
            nameof(AssetDefinition.chainType),
            nameof(AssetDefinition.chainNetwork),
            nameof(AssetDefinition.canonicalAssetId),
            nameof(AssetDefinition.symbol),
            nameof(AssetDefinition.displayName),
            nameof(AssetDefinition.scale),
        });
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(19)]
    public void AssetDefinition_RejectsUnsupportedScale(int scale)
    {
        var asset = ValidAsset();
        asset.scale = scale;

        AssetDefinitionPolicy.IsValid(asset).Should().BeFalse();
    }

    [Fact]
    public void AssetDefinition_RejectsSelfSupersession()
    {
        var asset = ValidAsset();
        asset.supersedesAssetDefinitionId = asset.id;

        AssetDefinitionPolicy.IsValid(asset).Should().BeFalse();
    }

    [Fact]
    public void UtilityAndRightsPolicies_AreSeparateDurableTypes()
    {
        RequiredPropertyNames(typeof(ProjectTokenPolicy)).Should().Contain(new[]
        {
            nameof(ProjectTokenPolicy.projectId),
            nameof(ProjectTokenPolicy.assetDefinitionId),
            nameof(ProjectTokenPolicy.version),
            nameof(ProjectTokenPolicy.termsHash),
            nameof(ProjectTokenPolicy.allocationRules),
        });
        RequiredPropertyNames(typeof(EquityOrRedemptionRightPolicy)).Should().Contain(new[]
        {
            nameof(EquityOrRedemptionRightPolicy.projectId),
            nameof(EquityOrRedemptionRightPolicy.kind),
            nameof(EquityOrRedemptionRightPolicy.disclosureVersion),
            nameof(EquityOrRedemptionRightPolicy.eligibilityPolicyVersion),
            nameof(EquityOrRedemptionRightPolicy.termsHash),
            nameof(EquityOrRedemptionRightPolicy.termsSnapshot),
        });
        typeof(ProjectTokenPolicy).Should().NotBe(typeof(EquityOrRedemptionRightPolicy));
    }

    [Fact]
    public void EligibilityDecision_PinsOneRightsPolicyAndAuditableEvidence()
    {
        RequiredPropertyNames(typeof(EligibilityDecision)).Should().Contain(new[]
        {
            nameof(EligibilityDecision.userId),
            nameof(EligibilityDecision.equityOrRedemptionRightPolicyId),
            nameof(EligibilityDecision.status),
            nameof(EligibilityDecision.evidenceDigest),
            nameof(EligibilityDecision.reasonCode),
        });
    }

    [Fact]
    public void ExistingValuePaths_HaveOnlyAdditiveCompatibilityLinks()
    {
        NullablePropertyNames(typeof(ProjectTokenConfig)).Should().Contain(nameof(ProjectTokenConfig.assetDefinitionId));
        NullablePropertyNames(typeof(FundingIntent)).Should().Contain(new[]
        {
            nameof(FundingIntent.paymentAssetDefinitionId),
            nameof(FundingIntent.awardAssetDefinitionId),
            nameof(FundingIntent.awardAmountAtoms),
            nameof(FundingIntent.projectTokenPolicyId),
            nameof(FundingIntent.equityOrRedemptionRightPolicyId),
            nameof(FundingIntent.eligibilityDecisionId),
        });
        NullablePropertyNames(typeof(TaskCommerceAgreement)).Should().Contain(nameof(TaskCommerceAgreement.assetDefinitionId));
        NullablePropertyNames(typeof(EconomicSettlement)).Should().Contain(nameof(EconomicSettlement.assetDefinitionId));
    }

    [Fact]
    public void FundingIntent_SeparatesPaymentAndAwardAssets()
    {
        NullablePropertyNames(typeof(FundingIntent)).Should().Contain(new[]
        {
            nameof(FundingIntent.paymentAssetDefinitionId),
            nameof(FundingIntent.awardAssetDefinitionId),
            nameof(FundingIntent.awardAmountAtoms),
        });
        typeof(FundingIntent).GetProperty("assetDefinitionId").Should().BeNull();
    }

    [Fact]
    public void SettlementLegAndExchangeContracts_PinEveryAssetAndAmount()
    {
        RequiredPropertyNames(typeof(EconomicSettlementLeg)).Should().Contain(new[]
        {
            nameof(EconomicSettlementLeg.economicSettlementId),
            nameof(EconomicSettlementLeg.position),
            nameof(EconomicSettlementLeg.kind),
            nameof(EconomicSettlementLeg.assetDefinitionId),
            nameof(EconomicSettlementLeg.amountAtoms),
        });
        RequiredPropertyNames(typeof(SwapQuote)).Should().Contain(new[]
        {
            nameof(SwapQuote.sourceAssetDefinitionId),
            nameof(SwapQuote.ardaAssetDefinitionId),
            nameof(SwapQuote.targetAssetDefinitionId),
            nameof(SwapQuote.sourceAmountAtoms),
            nameof(SwapQuote.ardaAmountAtoms),
            nameof(SwapQuote.targetAmountAtoms),
        });
        RequiredPropertyNames(typeof(SwapOrder)).Should().Contain(new[]
        {
            nameof(SwapOrder.quoteId),
            nameof(SwapOrder.actorUserId),
            nameof(SwapOrder.status),
        });
    }

    [Fact]
    public void UtilityAuthorization_RejectsCrossProjectOrWrongAssetKind()
    {
        var asset = ValidAsset();
        asset.kind = AssetDefinitionKind.PROJECT_UTILITY;
        asset.id = "project-asset";
        var config = new ProjectTokenConfig { projectId = "project-1", assetDefinitionId = asset.id, assetScale = asset.scale };
        var policy = new ProjectTokenPolicy
        {
            id = "policy-1",
            projectId = config.projectId,
            assetDefinitionId = asset.id,
            version = 1,
            termsHash = new string('a', 64),
            allocationRules = "{}",
            effectiveFrom = DateTime.UtcNow.AddMinutes(-1),
        };

        FinancialAuthorizationPolicy.TryValidateUtilityAuthorization(config, asset, policy, DateTime.UtcNow, out _).Should().BeTrue();

        policy.projectId = "other-project";
        FinancialAuthorizationPolicy.TryValidateUtilityAuthorization(config, asset, policy, DateTime.UtcNow, out _).Should().BeFalse();
    }

    [Fact]
    public void SwapQuote_RequiresDistinctUtilityLegsAndAnEcosystemIntermediary()
    {
        var now = DateTime.UtcNow;
        var source = ValidAsset("source", AssetDefinitionKind.PROJECT_UTILITY);
        var arda = ValidAsset("arda", AssetDefinitionKind.ECOSYSTEM_UTILITY);
        var target = ValidAsset("target", AssetDefinitionKind.PROJECT_UTILITY);
        var sourcePolicy = ActivePolicy("source-policy", "project-1", source.id, now);
        var targetPolicy = ActivePolicy("target-policy", "project-2", target.id, now);
        var quote = new SwapQuote
        {
            sourceAssetDefinitionId = source.id,
            ardaAssetDefinitionId = arda.id,
            targetAssetDefinitionId = target.id,
            sourceProjectTokenPolicyId = sourcePolicy.id,
            targetProjectTokenPolicyId = targetPolicy.id,
            sourceAmountAtoms = "100",
            ardaAmountAtoms = "50",
            targetAmountAtoms = "75",
            liquiditySnapshot = "{}",
            gateDecisionSnapshot = "{}",
            termsHash = new string('a', 64),
            expiresAt = now.AddMinutes(5),
        };

        FinancialAuthorizationPolicy.TryValidateSwapQuote(quote, source, arda, target, sourcePolicy, targetPolicy, now, out _).Should().BeTrue();

        arda.kind = AssetDefinitionKind.EQUITY_RIGHT;
        FinancialAuthorizationPolicy.TryValidateSwapQuote(quote, source, arda, target, sourcePolicy, targetPolicy, now, out _).Should().BeFalse();
    }

    [Fact]
    public void SettlementLeg_RejectsNegativePositionAndNonCanonicalAtoms()
    {
        var asset = ValidAsset("project-asset", AssetDefinitionKind.PROJECT_UTILITY);
        var leg = new EconomicSettlementLeg
        {
            economicSettlementId = "settlement-1",
            position = 0,
            kind = EconomicSettlementLegKind.AWARD,
            assetDefinitionId = asset.id,
            amountAtoms = "100",
        };

        FinancialAuthorizationPolicy.TryValidateSettlementLeg(leg, asset, out _).Should().BeTrue();

        leg.position = -1;
        FinancialAuthorizationPolicy.TryValidateSettlementLeg(leg, asset, out _).Should().BeFalse();
        leg.position = 0;
        leg.amountAtoms = "01";
        FinancialAuthorizationPolicy.TryValidateSettlementLeg(leg, asset, out _).Should().BeFalse();
    }

    [Fact]
    public void FundingAuthorization_RequiresCoherentPaymentAndUtilityAwardFacts()
    {
        var now = DateTime.UtcNow;
        var payment = ValidFiatAsset();
        var award = ValidAsset("project-asset", AssetDefinitionKind.PROJECT_UTILITY);
        var config = new ProjectTokenConfig { id = "config-1", projectId = "project-1", assetDefinitionId = award.id, assetScale = award.scale };
        var policy = ActivePolicy("utility-policy", config.projectId, award.id, now);
        var intent = new FundingIntent
        {
            projectId = config.projectId,
            projectTokenConfigId = config.id,
            funderUserId = "funder-1",
            paymentAssetDefinitionId = payment.id,
            awardAssetDefinitionId = award.id,
            awardAmountAtoms = "100",
            projectTokenPolicyId = policy.id,
            currencyCode = "usd",
            amount = 12.34m,
            scale = payment.scale,
        };

        FinancialAuthorizationPolicy.TryValidateFundingAuthorization(
            intent, config, payment, award, policy, null, null, now, out _).Should().BeTrue();

        intent.amount = 12.345m;
        FinancialAuthorizationPolicy.TryValidateFundingAuthorization(
            intent, config, payment, award, policy, null, null, now, out _).Should().BeFalse();

        intent.amount = 12.34m;
        intent.equityOrRedemptionRightPolicyId = "dangling-rights-policy";
        FinancialAuthorizationPolicy.TryValidateFundingAuthorization(
            intent, config, payment, award, policy, null, null, now, out _).Should().BeFalse();
    }

    [Fact]
    public void SwapOrderAcceptance_RequiresTheQuotedActorAndAnUnlinkedDraft()
    {
        var now = DateTime.UtcNow;
        var quote = new SwapQuote { id = "quote-1", actorUserId = "actor-1", expiresAt = now.AddMinutes(1) };
        var order = new SwapOrder { quoteId = quote.id, actorUserId = quote.actorUserId, status = SwapOrderStatus.DRAFT };

        FinancialAuthorizationPolicy.TryValidateSwapOrderAcceptance(order, quote, now, out _).Should().BeTrue();

        order.actorUserId = "other-actor";
        FinancialAuthorizationPolicy.TryValidateSwapOrderAcceptance(order, quote, now, out _).Should().BeFalse();
    }

    [Fact]
    public void SwapOrder_HasOneToOneSettlementBoundary()
    {
        typeof(SwapOrder).GetCustomAttributes(typeof(IndexAttribute), inherit: false)
            .Cast<IndexAttribute>()
            .Should().Contain(attribute => attribute.IsUnique && attribute.PropertyNames.SequenceEqual(new[] { nameof(SwapOrder.economicSettlementId) }));
        typeof(EconomicSettlement).GetProperty(nameof(EconomicSettlement.SwapOrder))!.PropertyType.Should().Be(typeof(SwapOrder));
    }

    private static AssetDefinition ValidAsset(
        string id = "asset-arda",
        AssetDefinitionKind kind = AssetDefinitionKind.ECOSYSTEM_UTILITY) => new()
    {
        id = id,
        kind = kind,
        chainType = "Algorand",
        chainNetwork = "Mainnet",
        canonicalAssetId = "123456",
        symbol = "ARDA",
        displayName = "ArdaNova",
        scale = 6,
    };

    private static ProjectTokenPolicy ActivePolicy(string id, string projectId, string assetDefinitionId, DateTime now) => new()
    {
        id = id,
        projectId = projectId,
        assetDefinitionId = assetDefinitionId,
        version = 1,
        termsHash = new string('a', 64),
        allocationRules = "{}",
        effectiveFrom = now.AddMinutes(-1),
    };

    private static AssetDefinition ValidFiatAsset() => new()
    {
        id = "usd",
        kind = AssetDefinitionKind.FIAT,
        chainType = "fiat",
        chainNetwork = "ISO-4217",
        canonicalAssetId = "USD",
        symbol = "USD",
        displayName = "United States Dollar",
        scale = 2,
    };

    private static IEnumerable<string> RequiredPropertyNames(Type type) => type
        .GetProperties()
        .Where(property => property.GetCustomAttributes(typeof(RequiredAttribute), inherit: false).Any())
        .Select(property => property.Name);

    private static IEnumerable<string> NullablePropertyNames(Type type) => type
        .GetProperties()
        .Where(property => Nullable.GetUnderlyingType(property.PropertyType) is not null || !property.PropertyType.IsValueType)
        .Where(property => property.GetCustomAttributes(typeof(RequiredAttribute), inherit: false).Length == 0)
        .Select(property => property.Name);
}
