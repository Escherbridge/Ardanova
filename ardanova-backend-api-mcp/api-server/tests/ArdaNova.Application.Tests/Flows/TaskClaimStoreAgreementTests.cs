namespace ArdaNova.Application.Tests.Flows;

using ArdaNova.Application.Tasks;
using ArdaNova.Application.Tests.Policies;
using ArdaNova.Application.Tests.Support;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Infrastructure.Data;
using ArdaNova.Infrastructure.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Npgsql.NameTranslation;
using TaskStatus = ArdaNova.Domain.Models.Enums.TaskStatus;

/// <summary>
/// The claim's SQL and <see cref="TaskClaimRule"/> are two statements of one rule (Tasks/AGENTS.md). Every fact row -
/// <see cref="TaskClaimRuleTests.Rows"/> plus every combination of assignee, status, escrow, agreement and reward -
/// is seeded as a real task, and <see cref="TaskClaimStore"/> must write exactly the rows the rule admits
/// (Claimable, or PaidNeedsReadiness: readiness is checked before the write). Membership is the controller's check,
/// so the rule is asked as for a member. Without <c>ARDANOVA_TEST_POSTGRES</c> this reports SKIPPED.
/// </summary>
[Trait("Category", "Database")]
public sealed class TaskClaimStoreAgreementTests : IAsyncLifetime
{
    private const string Founder = "user-founder";
    private const string Claimer = "user-claimer";
    private const string Holder = "user-b";
    private const string ProjectId = "project-claims";

    private DbContextOptions<ArdaNovaDbContext>? _options;
    private NpgsqlDataSource? _dataSource;

    public async Task InitializeAsync()
    {
        var connection = new NpgsqlConnectionStringBuilder(PostgresTestServer.ConnectionString!)
        {
            Database = "ardanova_claimrule_" + Guid.NewGuid().ToString("N")[..12],
        };
        var translator = new NpgsqlNullNameTranslator();
        var enumTypes = typeof(TaskStatus).Assembly.GetTypes()
            .Where(type => type.IsEnum && type.Namespace == "ArdaNova.Domain.Models.Enums")
            .ToList();
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connection.ConnectionString);
        foreach (var enumType in enumTypes)
            dataSourceBuilder.MapEnum(enumType, enumType.Name, translator);
        _dataSource = dataSourceBuilder.Build();
        _options = new DbContextOptionsBuilder<ArdaNovaDbContext>()
            .UseNpgsql(_dataSource, npgsql =>
            {
                foreach (var enumType in enumTypes)
                    npgsql.MapEnum(enumType, enumType.Name, "public", translator);
            })
            .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;

        await using var context = NewContext();
        await context.Database.EnsureCreatedAsync();
        var monthAgo = DateTime.UtcNow.AddDays(-30);
        foreach (var id in new[] { Founder, Claimer, Holder })
            context.Users.Add(new User { id = id, email = $"{id}@example.test", name = id[5..], createdAt = monthAgo, updatedAt = monthAgo });
        context.Projects.Add(new Project
        {
            id = ProjectId,
            title = "Claims",
            slug = ProjectId,
            description = "Claims, a community project.",
            problemStatement = "Something is missing.",
            solution = "Build it together.",
            categories = "CONSTRUCTION",
            status = ProjectStatus.PUBLISHED,
            createdById = Founder,
            createdAt = monthAgo,
            updatedAt = monthAgo,
            publishedAt = monthAgo,
        });
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        if (_options is null)
            return;

        await using (var context = NewContext())
            await context.Database.EnsureDeletedAsync();
        if (_dataSource is not null)
            await _dataSource.DisposeAsync();
    }

    [PostgresFact]
    public async Task TheClaimWrite_AcceptsExactlyTheFactRowsTheClaimRuleAdmits()
    {
        var rows = SharedRuleRows().Concat(EveryFactCombination()).Distinct().ToList();
        SharedRuleRows().Should().BeSubsetOf(rows);
        var taskIds = await SeedAsync(rows);

        var disagreements = new List<string>();
        var expectedClaimed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (facts, taskId) in rows.Zip(taskIds))
        {
            int written;
            await using (var context = NewContext())
                written = await new TaskClaimStore(context).TryClaimAsync(taskId, Claimer);

            var verdict = TaskClaimRule.Evaluate(facts, viewerIsProjectMember: true);
            var ruleAdmits = verdict is TaskClaimVerdict.Claimable or TaskClaimVerdict.PaidNeedsReadiness;
            if (written != (ruleAdmits ? 1 : 0))
                disagreements.Add($"{facts}: the store wrote {written} row(s), the rule said {verdict}");
            if (ruleAdmits)
                expectedClaimed.Add(taskId);
        }

        disagreements.Should().BeEmpty("the SQL predicate and TaskClaimRule are one rule");
        expectedClaimed.Should().NotBeEmpty().And.HaveCountLessThan(rows.Count, "the table must exercise both answers");

        // Each write touched only its own task: claimed tasks name the claimer, every other task kept its assignee.
        await using var check = NewContext();
        var stored = await check.ProjectTasks.AsNoTracking().ToDictionaryAsync(task => task.id, task => task.assignedToId);
        foreach (var (facts, taskId) in rows.Zip(taskIds))
            stored[taskId].Should().Be(expectedClaimed.Contains(taskId) ? Claimer : facts.AssignedToId, $"task for {facts}");
    }

    /// <summary>The rows S3 wrote for the rule itself; the viewer-membership column is the controller's concern here.</summary>
    private static IEnumerable<TaskClaimFacts> SharedRuleRows()
        => TaskClaimRuleTests.Rows.Select(row => new TaskClaimFacts(
            (string?)row[0], (TaskStatus)row[1], (EscrowStatus)row[2], (bool)row[3], (decimal?)row[4]));

    private static IEnumerable<TaskClaimFacts> EveryFactCombination()
        => from assignee in new[] { null, Holder }
           from status in Enum.GetValues<TaskStatus>()
           from escrow in Enum.GetValues<EscrowStatus>()
           from hasAgreement in new[] { false, true }
           from reward in new decimal?[] { null, 0m, 25m }
           select new TaskClaimFacts(assignee, status, escrow, hasAgreement, reward);

    /// <summary>One task per row; an agreement row is a real accepted bid (opportunity, bid, agreement), as TaskCommerceService writes it.</summary>
    private async Task<List<string>> SeedAsync(IReadOnlyList<TaskClaimFacts> rows)
    {
        var now = DateTime.UtcNow;
        var taskIds = new List<string>(rows.Count);
        await using var context = NewContext();
        for (var index = 0; index < rows.Count; index++)
        {
            var facts = rows[index];
            var taskId = $"task-{index:D3}";
            taskIds.Add(taskId);
            context.ProjectTasks.Add(new ProjectTask
            {
                id = taskId,
                projectId = ProjectId,
                title = $"Task {index}",
                status = facts.Status,
                priority = TaskPriority.MEDIUM,
                taskType = TaskType.FEATURE,
                escrowStatus = facts.EscrowStatus,
                equityReward = facts.EquityReward,
                assignedToId = facts.AssignedToId,
                createdAt = now,
                updatedAt = now,
            });
            if (!facts.HasAgreement)
                continue;

            context.Opportunities.Add(new Opportunity
            {
                id = $"opportunity-{index:D3}",
                title = $"Bounty {index}",
                slug = $"bounty-{index:D3}",
                description = "Paid work.",
                posterId = Founder,
                projectId = ProjectId,
                createdAt = now,
                updatedAt = now,
            });
            context.OpportunityBids.Add(new OpportunityBid
            {
                id = $"bid-{index:D3}",
                opportunityId = $"opportunity-{index:D3}",
                bidderId = Holder,
                proposal = "I can do it.",
                status = BidStatus.ACCEPTED,
                createdAt = now,
            });
            context.TaskCommerceAgreements.Add(new TaskCommerceAgreement
            {
                id = $"agreement-{index:D3}",
                semanticKey = $"agreement-key-{index:D3}",
                projectId = ProjectId,
                taskId = taskId,
                bidId = $"bid-{index:D3}",
                contributorUserId = Holder,
                assetCode = "UNIT",
                awardAmount = 25m,
                scale = 2,
                acceptedTermsSnapshot = "{}",
                termsHash = $"terms-{index:D3}",
                createdAt = now,
                updatedAt = now,
            });
        }

        await context.SaveChangesAsync();
        return taskIds;
    }

    private ArdaNovaDbContext NewContext() => new(_options!);
}
