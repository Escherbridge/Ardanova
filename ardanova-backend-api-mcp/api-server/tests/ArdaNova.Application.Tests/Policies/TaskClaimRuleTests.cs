using ArdaNova.Application.Tasks;
using ArdaNova.Domain.Models.Enums;
using FluentAssertions;
using Xunit;
using TaskStatus = ArdaNova.Domain.Models.Enums.TaskStatus;

namespace ArdaNova.Application.Tests.Policies;

public class TaskClaimRuleTests
{
    public static TheoryData<string?, TaskStatus, EscrowStatus, bool, decimal?, bool, TaskClaimVerdict> Rows => new()
    {
        { null, TaskStatus.TODO, EscrowStatus.NONE, false, null, true, TaskClaimVerdict.Claimable },
        { null, TaskStatus.IN_PROGRESS, EscrowStatus.NONE, false, 0m, true, TaskClaimVerdict.Claimable },
        { null, TaskStatus.TODO, EscrowStatus.NONE, false, 25m, true, TaskClaimVerdict.PaidNeedsReadiness },
        { null, TaskStatus.TODO, EscrowStatus.NONE, false, null, false, TaskClaimVerdict.NotAMember },
        { "user-b", TaskStatus.TODO, EscrowStatus.NONE, false, null, false, TaskClaimVerdict.NotAMember },
        { "user-b", TaskStatus.TODO, EscrowStatus.NONE, false, null, true, TaskClaimVerdict.AlreadyTaken },
        { null, TaskStatus.COMPLETED, EscrowStatus.NONE, false, null, true, TaskClaimVerdict.Completed },
        { null, TaskStatus.TODO, EscrowStatus.NONE, true, null, true, TaskClaimVerdict.UnderAgreement },
        { null, TaskStatus.TODO, EscrowStatus.FUNDED, false, null, true, TaskClaimVerdict.UnderAgreement },
    };

    [Theory]
    [MemberData(nameof(Rows))]
    public void Evaluate_ReturnsTheMostBlockingReason(
        string? assignedTo, TaskStatus status, EscrowStatus escrow, bool hasAgreement, decimal? reward,
        bool isMember, TaskClaimVerdict expected)
    {
        var facts = new TaskClaimFacts(assignedTo, status, escrow, hasAgreement, reward);

        TaskClaimRule.Evaluate(facts, isMember).Should().Be(expected);
        TaskClaimRule.CanClaimInPlace(facts, isMember).Should().Be(expected == TaskClaimVerdict.Claimable);
    }
}
