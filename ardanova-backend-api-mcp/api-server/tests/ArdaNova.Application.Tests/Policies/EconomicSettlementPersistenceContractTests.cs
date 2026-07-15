namespace ArdaNova.Application.Tests.Policies;

using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Domain.Policies;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class EconomicSettlementPersistenceContractTests
{
    [Fact]
    public void Settlement_UsesFixedScaleAmountAndUniqueEconomicKeys()
    {
        var type = typeof(EconomicSettlement);

        type.GetProperty(nameof(EconomicSettlement.amount))!.PropertyType.Should().Be(typeof(decimal));
        type.GetProperty(nameof(EconomicSettlement.amount))!
            .GetCustomAttributes(typeof(PrecisionAttribute), inherit: false)
            .Cast<PrecisionAttribute>()
            .Single()
            .Precision.Should().Be(38);
        type.GetCustomAttributes(typeof(IndexAttribute), inherit: false)
            .Cast<IndexAttribute>()
            .Where(index => index.IsUnique)
            .SelectMany(index => index.PropertyNames)
            .Should().Contain(new[]
            {
                nameof(EconomicSettlement.idempotencyKey),
                nameof(EconomicSettlement.externalEventId),
            });
    }

    [Fact]
    public void Outbox_AllowsOnlyOneDispatchClaimPerSettlement()
    {
        typeof(EconomicOutbox)
            .GetCustomAttributes(typeof(IndexAttribute), inherit: false)
            .Cast<IndexAttribute>()
            .Single(index => index.PropertyNames.Contains(nameof(EconomicOutbox.settlementId)))
            .IsUnique.Should().BeTrue();
    }

    [Fact]
    public void ProjectInvestment_RequiresOneInvestmentPerStripePaymentIntent()
    {
        typeof(ProjectInvestment)
            .GetCustomAttributes(typeof(IndexAttribute), inherit: false)
            .Cast<IndexAttribute>()
            .Single(index => index.PropertyNames.Contains(nameof(ProjectInvestment.stripePaymentIntentId)))
            .IsUnique.Should().BeTrue();
    }

    [Fact]
    public void StripeWebhookEvent_UsesTheProviderEventIdAsItsPrimaryKey()
    {
        typeof(StripeWebhookEvent)
            .GetProperty(nameof(StripeWebhookEvent.id))!
            .GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.KeyAttribute), inherit: false)
            .Should().ContainSingle();
    }

    [Fact]
    public void FundingIntent_PersistsImmutableTermsAndOneProviderOrSettlementLink()
    {
        var type = typeof(FundingIntent);

        type.GetProperty(nameof(FundingIntent.amount))!.PropertyType.Should().Be(typeof(decimal));
        type.GetProperty(nameof(FundingIntent.amount))!
            .GetCustomAttributes(typeof(PrecisionAttribute), inherit: false)
            .Cast<PrecisionAttribute>()
            .Single()
            .Precision.Should().Be(38);

        type.GetCustomAttributes(typeof(IndexAttribute), inherit: false)
            .Cast<IndexAttribute>()
            .Where(index => index.IsUnique)
            .SelectMany(index => index.PropertyNames)
            .Should().Contain(new[]
            {
                nameof(FundingIntent.semanticKey),
                nameof(FundingIntent.providerPaymentIntentId),
                nameof(FundingIntent.verifiedProviderEventId),
                nameof(FundingIntent.settlementId),
            });

        RequiredPropertyNames(type).Should().Contain(new[]
        {
            nameof(FundingIntent.eligibilitySnapshot),
            nameof(FundingIntent.termsSnapshot),
            nameof(FundingIntent.termsHash),
        });
    }

    [Fact]
    public void TaskCommerceAgreement_PersistsOneAcceptedAwardPerBidAndTask()
    {
        var type = typeof(TaskCommerceAgreement);

        type.GetProperty(nameof(TaskCommerceAgreement.awardAmount))!.PropertyType.Should().Be(typeof(decimal));
        type.GetProperty(nameof(TaskCommerceAgreement.awardAmount))!
            .GetCustomAttributes(typeof(PrecisionAttribute), inherit: false)
            .Cast<PrecisionAttribute>()
            .Single()
            .Precision.Should().Be(38);

        type.GetCustomAttributes(typeof(IndexAttribute), inherit: false)
            .Cast<IndexAttribute>()
            .Where(index => index.IsUnique)
            .SelectMany(index => index.PropertyNames)
            .Should().Contain(new[]
            {
                nameof(TaskCommerceAgreement.semanticKey),
                nameof(TaskCommerceAgreement.taskId),
                nameof(TaskCommerceAgreement.bidId),
                nameof(TaskCommerceAgreement.escrowId),
                nameof(TaskCommerceAgreement.settlementId),
            });

        RequiredPropertyNames(type).Should().Contain(new[]
        {
            nameof(TaskCommerceAgreement.acceptedTermsSnapshot),
            nameof(TaskCommerceAgreement.termsHash),
        });
    }

    [Theory]
    [InlineData(EconomicSettlementStatus.DRAFT, EconomicSettlementStatus.AUTHORIZED, true)]
    [InlineData(EconomicSettlementStatus.AUTHORIZED, EconomicSettlementStatus.PENDING_DISPATCH, true)]
    [InlineData(EconomicSettlementStatus.PENDING_DISPATCH, EconomicSettlementStatus.SUBMITTED, true)]
    [InlineData(EconomicSettlementStatus.SUBMITTED, EconomicSettlementStatus.CONFIRMED, true)]
    [InlineData(EconomicSettlementStatus.SUBMITTED, EconomicSettlementStatus.AWAITING_RECONCILIATION, true)]
    [InlineData(EconomicSettlementStatus.AWAITING_RECONCILIATION, EconomicSettlementStatus.CONFIRMED, true)]
    [InlineData(EconomicSettlementStatus.AWAITING_RECONCILIATION, EconomicSettlementStatus.SUBMITTED, true)]
    [InlineData(EconomicSettlementStatus.CONFIRMED, EconomicSettlementStatus.PENDING_DISPATCH, false)]
    [InlineData(EconomicSettlementStatus.AWAITING_RECONCILIATION, EconomicSettlementStatus.PENDING_DISPATCH, false)]
    public void SettlementStateMachine_OnlyAllowsForwardOrTerminalTransitions(
        EconomicSettlementStatus current,
        EconomicSettlementStatus next,
        bool expected)
    {
        EconomicSettlementStateMachine.CanTransition(current, next).Should().Be(expected);
    }

    private static IEnumerable<string> RequiredPropertyNames(Type type) => type
        .GetProperties()
        .Where(property => property.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), inherit: false).Any())
        .Select(property => property.Name);
}
