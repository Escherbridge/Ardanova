namespace ArdaNova.Application.Tasks;

/// <summary>The claim write: one conditional UPDATE that enforces <see cref="TaskClaimRule"/>'s blocking facts atomically.</summary>
/// <remarks>See Services/Implementations/AGENTS.md §Task assignment.</remarks>
public interface ITaskClaimStore
{
    /// <summary>
    /// Assigns <paramref name="taskId"/> to <paramref name="userId"/> only while it is unassigned, not COMPLETED,
    /// escrow NONE and under no agreement. Returns the rows written: 1 for the claim, 0 when any fact blocked it.
    /// </summary>
    Task<int> TryClaimAsync(string taskId, string userId, CancellationToken ct = default);
}

/// <summary>Stable machine codes on a refused claim's 409, so a client branches on the code, never the prose.</summary>
public static class TaskClaimCodes
{
    /// <summary>Someone else holds the task.</summary>
    public const string Taken = "TASK_TAKEN";

    /// <summary>The task is COMPLETED.</summary>
    public const string Done = "TASK_DONE";

    /// <summary>An accepted bid's agreement, or escrow, owns who does the work.</summary>
    public const string UnderAgreement = "TASK_UNDER_AGREEMENT";
}
