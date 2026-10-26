namespace ArdaNova.Application.Tests.Services;

using System.Linq.Expressions;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Data;
using ArdaNova.Infrastructure.Repositories;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

/// <summary>
/// A first credit through the REAL EF repository must reach SaveChanges as an INSERT. Found live on
/// the award approve: Update() re-marked the just-added row Modified, and Postgres updated 0 rows.
/// </summary>
public class TokenBalanceServiceTrackingTests
{
    [Fact]
    public async Task AFirstCreditIsSavedAsANewRow_NotAnUpdateOfOneThatDoesNotExist()
    {
        // Model-only provider: change tracking is real, nothing connects.
        using var context = new ArdaNovaDbContext(
            new DbContextOptionsBuilder<ArdaNovaDbContext>().UseNpgsql("Host=model-only").Options);
        EntityState? savedAs = null;
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => savedAs = context.ChangeTracker.Entries<TokenBalance>().Single().State)
            .ReturnsAsync(1);
        var configs = new Mock<IRepository<ProjectTokenConfig>>();
        configs.Setup(r => r.GetByIdAsync("config-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectTokenConfig { id = "config-1", gateStatus = ProjectGateStatus.ACTIVE });
        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<TokenBalanceDto>(It.IsAny<TokenBalance>()))
            .Returns((TokenBalance row) => new TokenBalanceDto { Balance = row.balance });
        var service = new TokenBalanceService(new NoStoredBalances(context), configs.Object, unitOfWork.Object, mapper.Object);

        var credited = await service.CreditAsync("worker", "config-1", 4000, TokenHolderClass.CONTRIBUTOR);

        credited.IsSuccess.Should().BeTrue(credited.Error);
        credited.Value!.Balance.Should().Be(4000);
        savedAs.Should().Be(EntityState.Added);
    }

    /// <summary>The real repository with an empty table, so the lookup needs no database.</summary>
    private sealed class NoStoredBalances(ArdaNovaDbContext context) : Repository<TokenBalance>(context)
    {
        public override Task<TokenBalance?> FindOneAsync(
            Expression<Func<TokenBalance, bool>> predicate, CancellationToken cancellationToken = default)
            => Task.FromResult<TokenBalance?>(null);
    }
}
