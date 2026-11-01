# Tasks

## TaskClaimRule

`TaskClaimRule.Evaluate` is the single statement of who may claim a task. Two places must agree with it,
or a feed card offers "Pick up task" on a task the claim then refuses:

- **The claim write** (Phase 3 Lane K1, `TaskClaimStore`): a conditional `UPDATE … WHERE` that enforces the
  same blocking facts atomically (unassigned, not COMPLETED, no agreement, escrow NONE). Membership and the
  paid-task readiness check run in `TasksController` before the write. K1 keeps a real-Postgres table test
  proving the SQL predicate and this rule agree on every fact row.
- **The feed card** (Lane K2): `viewer.canClaim = TaskClaimRule.CanClaimInPlace(...)`. A paid task
  (`EquityReward > 0`) is never claimable in place: the card routes to the task page, where the payout
  readiness check (`TasksController.Assign`, "assign a paid task") runs.

Order of verdicts matters: the most blocking reason wins, so a non-member never learns more about the task
than "not a member". Sources of the rules: membership `TasksController.cs` (Assign, IsProjectMemberAsync);
agreement/escrow conflict `TaskServices.AssignAsync`; paid readiness `TasksController.Assign`.
