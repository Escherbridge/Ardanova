namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Data;
using ArdaNova.Infrastructure.UnitOfWork;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// MED-2: accepting a compare-and-set's mirror must accept exactly the columns the CAS wrote. A whole-row accept
/// silently dropped any other pending change on the row. Change-tracker logic only, so no database is opened.
/// </summary>
public class UnitOfWorkMarkPersistedTests
{
    [Fact]
    public void MarkPersisted_AcceptsOnlyTheColumnsTheCompareAndSetWrote_AndKeepsEveryOtherPendingChange()
    {
        using var context = NewContext();
        var settlement = TrackedSettlement(context);
        settlement.failureDetail = "a change the compare-and-set did not write";
        settlement.status = EconomicSettlementStatus.PENDING_DISPATCH;
        settlement.version = 2;

        new UnitOfWork(context).MarkPersisted(
            settlement, nameof(EconomicSettlement.status), nameof(EconomicSettlement.version));

        var entry = context.Entry(settlement);
        entry.Property(item => item.status).IsModified.Should().BeFalse("the CAS already wrote it");
        entry.Property(item => item.status).CurrentValue.Should().Be(EconomicSettlementStatus.PENDING_DISPATCH);
        entry.Property(item => item.version).IsModified.Should().BeFalse();
        entry.Property(item => item.failureDetail).IsModified.Should().BeTrue("a change the CAS did not write is still saved");
        entry.State.Should().Be(EntityState.Modified);
    }

    [Theory]
    [InlineData(null)] // no column named at all
    [InlineData("notAColumn")] // a column the row does not have
    public void MarkPersisted_RefusesAnEmptyOrUnknownColumnList(string? column)
    {
        string[] columns = column is null ? [] : [column];
        using var context = NewContext();
        var settlement = TrackedSettlement(context);
        settlement.status = EconomicSettlementStatus.PENDING_DISPATCH;

        var accept = () => new UnitOfWork(context).MarkPersisted(settlement, columns);

        accept.Should().Throw<Exception>();
        context.Entry(settlement).Property(item => item.status).IsModified.Should().BeTrue("nothing was accepted");
    }

    private static ArdaNovaDbContext NewContext()
        => new(new DbContextOptionsBuilder<ArdaNovaDbContext>()
            .UseNpgsql("Host=unused.invalid;Database=unused")
            .Options);

    private static EconomicSettlement TrackedSettlement(ArdaNovaDbContext context)
    {
        var settlement = new EconomicSettlement
        {
            id = "settlement-mark",
            kind = EconomicSettlementKind.EXCHANGE,
            status = EconomicSettlementStatus.AUTHORIZED,
            idempotencyKey = "avatar:funding-intent:intent-mark",
            beneficiaryUserId = "user-mark",
            assetCode = "PROJ",
            amount = 3m,
            scale = 2,
            version = 1,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow,
        };
        context.Attach(settlement);
        return settlement;
    }
}
