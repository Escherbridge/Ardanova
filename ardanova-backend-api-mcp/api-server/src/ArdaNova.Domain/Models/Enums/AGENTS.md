# Enums

Every `.cs` file here is GENERATED from `ardanova-client/prisma/database-architecture.dbml` by
`npm run generate:csharp`, and the generator deletes any `.cs` file here whose enum is not in the
DBML. C# enums cannot be partial, so nothing hand-written can live beside them: what a member
MEANS is recorded below. Contract: `../AGENTS.md` section "Generated contract".

# FundingIntentStatus

Lifecycle of one sponsorship: platform units in, project units out.

There is no card-payment state here any more. A sponsor already holds platform units when they
press Support, so the intent goes straight from DRAFT to the node. See
`src/ArdaNova.Application/Services/Implementations/AGENTS.md`, sponsorship.

- `DRAFT` - locked terms committed; nothing has been asked of the node yet.
- `SETTLEMENT_PENDING` - sent to the node, which has not yet given a durable answer.
- `SETTLED` - the node moved the value and ArdaNova recorded the stake.
- `HOLDING` - readiness lapsed between the click and the confirmation, so nothing was taken and
  the sponsor keeps their platform units. Terminal: a lapse must never become an endless retry.
- `REJECTED` - refused by policy before any node call.
- `CANCELLED` - withdrawn by the sponsor before settlement.
- `FAILED` - the node refused, or the platform gave up; no value moved.

# EconomicSettlementKind

The two newest kinds also carry DBML value notes.

- `EXCHANGE` - a sponsor's ARDA exchanged for project units in one node operation.
- `HOLD_RELEASE` - units already held on the node released to their beneficiary when the window
  elapses.
