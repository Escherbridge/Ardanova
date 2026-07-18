namespace ArdaNova.Application.Tests.Policies;

using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Domain.Policies;
using FluentAssertions;
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
            nameof(FundingIntent.assetDefinitionId),
            nameof(FundingIntent.projectTokenPolicyId),
            nameof(FundingIntent.equityOrRedemptionRightPolicyId),
            nameof(FundingIntent.eligibilityDecisionId),
        });
        NullablePropertyNames(typeof(TaskCommerceAgreement)).Should().Contain(nameof(TaskCommerceAgreement.assetDefinitionId));
        NullablePropertyNames(typeof(EconomicSettlement)).Should().Contain(nameof(EconomicSettlement.assetDefinitionId));
    }

    private static AssetDefinition ValidAsset() => new()
    {
        id = "asset-arda",
        kind = AssetDefinitionKind.ECOSYSTEM_UTILITY,
        chainType = "Algorand",
        chainNetwork = "Mainnet",
        canonicalAssetId = "123456",
        symbol = "ARDA",
        displayName = "ArdaNova",
        scale = 6,
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
