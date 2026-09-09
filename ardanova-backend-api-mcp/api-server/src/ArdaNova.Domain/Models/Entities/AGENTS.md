# Entities

Every file in this directory is GENERATED from
`ardanova-client/prisma/database-architecture.dbml` by
`ardanova-client/scripts/generate-csharp-models.ts` (`npm run generate:csharp`). The DBML is
the single source of truth for columns; `prisma/schema.prisma` and these C# entities are both
downstream of it. Adding a column in only one of the three is how a rename passes review and
then vanishes on the next regeneration - so a new column lands in the DBML first, in the
prisma model and the SQL migration alongside it, and only then here.

The doc-comments these files carry ARE hand-written and will not survive a regeneration
verbatim. That is the deliberate trade this directory makes: the rationale lives in this file,
and each entity carries only a one-line pointer at the section it belongs to.

# Sponsorship

`FundingIntent` is one sponsorship, recorded as an EXCHANGE rather than a payment. Both legs
and the rate that related them are written once, when the person authorises it, and are
immutable from then on: the quantity of project units bought, the platform units given up, and
the declared rate. The node is asked to move exactly those amounts, so a rate that moves
afterwards cannot change what was agreed - a sponsorship settled a week later settles at the
number the sponsor actually saw.

Three unique indexes carry the idempotency of the whole flow: `semanticKey` (the same intent
asked for twice is one intent), `exchangeIdempotencyKey` (the node call is addressed, not
repeated), and `settlementId` (one settlement row per intent). A redelivered authorisation
resolves to the row that already exists rather than opening a second sponsorship.

HOLDING is a first-class resting state, not a failure. When the beneficiary's account is not
yet ready, nothing was submitted and the sponsor's reservation still stands at the node; the
settlement row keeps a readiness failure code and waits. See
`Services/Implementations/AGENTS.md`, "Reward dispatch hygiene", for the schedule that governs
how long it waits and who gets woken.

# Project token

`ProjectTokenConfig` records a project's stake and, separately, the three node-side facts that
make that stake real: the asset was minted, a treasury account exists, and the reserve ledger
knows its partition caps. Each of those fields is written ONLY once its own node call actually
succeeded.

There is deliberately no `provisioned` boolean. The fundability predicates read the facts
themselves, so there is no second flag that can disagree with them - a stored flag is exactly
how a project comes to advertise itself as fundable while the node holds nothing for it.

`contributorPartitionSupply` is a DECLARED cap, mirroring the node reserve ledger's
`contributor` partition. It is a display and planning figure; the node's own ledger, not this
column, refuses an award that would exceed the allowance.

# Token allocation

`TokenAllocation` is one commitment of a project's stake - to a backlog item, or to a person -
and, once the node has actually drawn it, the READ-MODEL of the hold that backs it.

The award of record is `tokenAmount`, a whole-unit count. `equityPercentage` is a derived
display share of the declared supply and is never the thing anyone is paid; two award fields
that can be written independently is how a task's board figure and its agreement come to
disagree.

The hold columns mirror the node; they are not a source. `IsNodeHeld` (a non-null `holdId`) is
the single predicate every payout path reads, which is why every row that predates this design
- all of them, since no allocation on record was ever committed - is unpayable rather than
payable-by-default. Backfilling those columns would turn historical reservations into awards
nothing backs.

A backlog reservation (`pbiId` set, no recipient) commits nothing on the node: there is no
person to hold units for yet, so it can never be paid out. Only an award accepted from a bid
carries a hold, and only that can be approved.

`workerHoldReleasesAt` ends the worker's own window. It is a tax-deferral window - the award is
theirs, held until that date so they are not taxed before they can realise it - and never a
chargeback window, because reserve-sourced reward units were never bought with a card. See
`Services/Implementations/AGENTS.md`, "Worker awards", for the two-hold lifecycle.

# Treasury

`TreasuryTransaction` is RETIRED. A project's money view is derived - see
`Services/Implementations/AGENTS.md`, "Project money" - from the rows that already record each
event, so this per-project ledger was never written and is never read. Reviving it would create a
second record of the same movements that can disagree with all four sources, and the balance it
accumulated into would be a database number standing in for money the node actually custodies.

The retirement is a control, not a comment: the constructor is not public, so application code
cannot create a row and a future writer gets a compile error instead of quietly starting a second
ledger. EF still materialises rows through it, which is why it is `protected` rather than removed.

The columns are unchanged on purpose - the stored schema does not move. Dropping the table and
its `DbSet` (`Infrastructure/Data/ArdaNovaDbContext.cs:95`) is a MIGRATION, not an entity edit,
and belongs to whichever item next opens that file.

# Guild award splits

`OpportunityBid.guildAwardSplit` is a `decimal?` with `[Precision(18, 8)]`. It is the PART OF
`proposedAmount` owed to the guild, in the SAME UNITS — not a percentage and not basis points —
so the bidder's own share is `proposedAmount - guildAwardSplit`. NULL means no split was
declared, which is why an undeclared split is still refused rather than silently treated as
zero. The validation rule (`0 < guildAwardSplit < proposedAmount`, whole units only) lives with
the refusals it replaced in `Application/Services/Implementations/OpportunityBidService.cs`.

The `why` for the column and its migration is in
`ardanova-client/prisma/migrations/20260909100000_retire_card_rail_and_guild_split/migration.sql`.
That same migration retires the card rail: `ProjectInvestment.stripePaymentIntentId` and the
whole `StripeWebhookEvent` receipt table are gone, because the rail they served no longer
exists. `PayoutRequest.stripePayoutId` is a DIFFERENT rail and is deliberately untouched.

Note these C# entity files carry hand-added doc-comments the DBML generator does not emit
(`ProjectInvestment.showBackerName`, for one), so a regeneration strips them. That is a known
property of this directory, not something any single change introduced.