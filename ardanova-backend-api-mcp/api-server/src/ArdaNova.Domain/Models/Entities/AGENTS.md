# Entities

Every `<Table>.cs` in this directory is GENERATED from
`ardanova-client/prisma/database-architecture.dbml` by
`ardanova-client/scripts/generate-csharp-models.ts` (`npm run generate:csharp`) and is a pure
function of the DBML: CI regenerates it and fails on any byte of difference. The DBML is the
single source of truth for columns; `prisma/schema.prisma` and these C# entities are both
downstream of it. Adding a column in only one of the three is how a rename passes review and
then vanishes on the next regeneration - so a new column lands in the DBML first, in the
prisma model and the SQL migration alongside it, and only then here.

Everything hand-written lives in files the generator never writes: `<Table>.Custom.cs` (the
other half of `public partial class <Table>` - the type's summary, computed `[NotMapped]`
members, constants, constructors) and standalone helper types (`PostVisibilityPolicy.cs`,
`ProjectFundingUnits.cs`, `ProjectMediaPaths.cs`). The contract, including how a runtime
default is declared, is in `../AGENTS.md` section "Generated contract".

A generated property cannot carry a hand-written doc-comment, so what a column MEANS lives in
this file, under the entity's section ("Columns"). Each `.Custom.cs` carries only a one-line
summary and a pointer at its section.

# Project

Columns:

- `summary` - the one-line public ask, shown on cards and searched alongside the problem
  statement.
- `fundingGoalUnit` - the unit `fundingGoal` is denominated in; every ask on this node is in
  dollars. Its C# default is `ProjectFundingUnits.Ledger` (USD), declared in the DBML note with
  `csharp-default:`. The database default is still `'ARDA'`, so a row inserted outside EF (raw
  SQL, or the Prisma demo seeder `ardanova-client/scripts/seed-local-demo.ts`) gets ARDA while
  every EF insert gets USD. Aligning the two is a migration, not an entity edit.
- `mediaPaths` - uploaded media as a JSON array of storage bucket paths with display metadata.
  Never null, `"[]"` (`ProjectMediaPaths.Empty`) when empty - also a `csharp-default:`, because
  readers parse it as JSON and an empty string is not a JSON array.

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

Columns, in the groups the DBML declares them:

- The exchange, locked at authorization. Two legs and one rate. Amounts are exact integer base
  units, never a float and never a currency: the node settles token units, and a settlement
  denominated in "USD" is refused at the gateway rather than sent.
  - `platformAssetCode` - platform unit the sponsor spends; "ARDA" on this node.
  - `platformAmountBaseUnits` - exact platform-unit base units the sponsor gives up.
  - `platformScale` - decimal places of the platform unit; two, so a person always thinks in
    whole currency.
  - `requestedPlatformBaseUnits` - what the sponsor offered, before whole units were fitted to
    it. Kept because the idempotency key covers the REQUEST: a replay is only "different terms"
    when this differs, and the remainder below one unit is the sponsor's, not the platform's.
  - `projectUnitAssetCode` - symbol of the project unit the sponsor receives.
  - `projectUnitQuantity` - whole project units bought. This is the quantity the person agreed to.
  - `projectUnitScale` - decimal places of the project asset, as the node minted it.
  - `projectAssetId` - chain asset the project units are issued from; funding is impossible
    without it.
  - `usdReferenceMinorUnits` - exact currency reference the disclosure showed, in minor units.
    Never an input to the exchange.
- The declared rate (D7/D16: ArdaNova prices, the node records).
  - `declaredRateArdaPerUnit` - platform units one whole project unit cost, as an exact decimal
    string.
  - `declaredRateUsdPerUnit` - currency reference for one whole project unit, as an exact
    decimal string.
  - `declaredRateSource` - where the rate came from; stamped on the node operation.
  - `declaredRateDeclaredAt` - when ArdaNova declared the rate this sponsorship was priced at.
  - `declaredRateVersion` - optional rate-policy version, when the source publishes one.
- Snapshots: `eligibilitySnapshot`, `termsSnapshot`, `termsHash`.
- The node operation.
  - `exchangeIdempotencyKey` - the one key this sponsorship is ever settled under, shared with
    its settlement row so the synchronous attempt and the outbox worker can never create two
    economic effects.
  - `azoaOperationId` - durable node operation that moved the value.
  - `holdReference` - node-side hold the freshly issued units sit in until the window elapses.
  - `releaseNotBefore` - when the held units become the sponsor's to move; surfaced as "yours
    after N days".
  - `settlementAttemptCount` - node attempts this intent has made. Bounded: a lapse must never
    retry forever.
- Why it stopped.
  - `holdingReasonCode` - machine code an operator triages a held or failed sponsorship on.
  - `holdingReasonDetail` - consumer-safe explanation shown to the sponsor.
  - `heldAt` - when the sponsorship was parked with the sponsor's platform units untouched.

`FundingIntentVoid` is a sponsorship request key voided before anything was admitted under it:
the tombstone a late create is refused by. See `Services/Implementations/AGENTS.md`, "Sponsorship
void".

# Economic settlement

Columns on `EconomicSettlement`, as the node's receipt reported them:

- `azoaGrossBaseUnits` - base units the payer side gave up.
- `azoaFeeBaseUnits` - base units the node kept between gross and net; a silently shaved fee is
  a defect, not a detail.
- `azoaNetBaseUnits` - base units the beneficiary side actually received.

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

The predicates are `IsNodeProvisioned` and `IsFundable` in `ProjectTokenConfig.Custom.cs`.

Columns:

- `fundingGoal` - what the project is asking for, in US dollars (`ProjectFundingUnits.Ledger`).
- `fundingRaised` - dollars settled into the raise so far. Same unit as `fundingGoal`, by rule.
- Node-side provisioning (self-serve project token, W5.2) - the three facts above, each written
  only once its node call actually succeeded:
  - `assetChain` - chain the project asset was minted on; null until the mint succeeds.
  - `treasuryChain` - chain the project's node-side treasury account lives on.
  - `treasuryReference` - opaque project reference this tenant addresses the treasury by on the
    node.
  - `treasuryAddress` - address of the project's node-side custodial treasury, where funding
    ARDA lands.
  - `treasuryResolvedAt` - when the node resolved (or created) the treasury account.
  - `contributorPartitionSupply` / `investorPartitionSupply` - declared caps of the node reserve
    ledger's `contributor` / `investor` partitions, in whole units.
  - `partitionsDeclaredAt` - when the node accepted the partition declaration; null means the
    node polices nothing yet.
  - `unitsDisclosure` - owner-authored disclosure shown before anyone commits funds (Q5 / Q-X3).
  - `provisionedAt` - when every provisioning leg completed.
  - `provisionedByUserId` - the project owner who ran provisioning; never a caller-supplied id.
  - `provisioningFailureReason` - last provisioning failure, kept so the owner is told what to
    retry rather than "not configured".

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

Hold read-model columns (W7.2). Written only from what the node returned. A null here means the
node holds nothing for this allocation, which is why the payout paths refuse on it rather than
crediting a balance nothing backs. The predicates are `IsNodeHeld` and `IsAwaitingWorkerWindow`
in `TokenAllocation.Custom.cs`.

- `taskCommerceAgreementId` - the commerce agreement this award was accepted under; null for a
  plain reservation.
- `holdChain` - rail the node's hold sits on; null until a hold exists.
- `reservePartition` - named reserve-ledger allowance the node drew this commitment from.
- `holdId` - the escrow hold: opened when the owner accepted the bid, released on approval.
- `holdState` - the node's last reported state for `holdId`.
- `workerHoldId` - the worker's own window: the second hold the escrow hold is continued into
  when the owner approves completed work. It exists so the award is restricted until it is
  liquid, which defers the taxable event; it is NOT a chargeback window.
- `workerHoldState` - the node's last reported state for `workerHoldId`.
- `workerHoldReleasesAt` - when the worker's window ends and the award can become liquid.
- `holdSyncedAt` - when the hold columns were last written from a node answer.

# Token balance

`TokenBalance` is a holder's units in one asset - a READ-MODEL of chain truth, not the ledger
of record. The balance of record for a person's platform unit is their custodial wallet on the
node. These rows are ArdaNova's own bookkeeping until the node holds the units (W7.2), which is
what the three provenance columns make checkable.

Chain provenance (W5.3): three facts that say whether this row is chain truth or only ArdaNova's
own bookkeeping. Nothing stores a separate "is real" flag: `IsChainObserved`
(`TokenBalance.Custom.cs`) is DERIVED, so it can never disagree with what was actually recorded.

Columns:

- `isPlatformToken` - legacy marker. It NO LONGER locates a person's ARDA: that is read from
  their custodial wallet.
- `balance` - units in the asset's BASE units. Fixed scale, never a float, never narrowed to int.
- `lockedBalance` - base units committed elsewhere and therefore unavailable to move
  (`AvailableBalance` is `balance - lockedBalance`).
- `assetId` - the on-chain asset these units count; null means this row was never resolved to
  one.
- `walletAddress` - custodial address the units were observed at.
- `observedAt` - when a chain read produced these units. Null means never observed.

# Payout request

`PayoutRequest` is one "sell my units" request, and the read-model of the node exchange that
settled it.

This row is a receipt, never the balance of record. The units left and the platform units
arrived on the shared node; only what the node returned is written here, so a null column means
"the node has not told us yet" - never "zero" and never "done".

It records the SELL leg only. Turning platform units into money is a separate, node-owned
cash-out with its own durable ledger, read back through the payout service rather than copied
into Postgres: duplicating it would make two records of one movement of real money, which is
exactly the ledger-of-record this platform rejected for stake. Reasoning:
`src/ArdaNova.Application/Services/Implementations/AGENTS.md`, cash out.

The honest-name accessors, the derived exchange idempotency key and `IsNodeSettled` are in
`PayoutRequest.Custom.cs`.

Columns:

- `ardaTokenAmount` - whole platform units received, floored. A label; never money math.
- `usdAmount` - dollar reference at the declared rate, for display. Never read back into money
  math.
- `ardaMinorUnits` - platform units received, in exact base units at the platform's fixed
  scale. The figure of record for what this sale paid; `ardaTokenAmount` and `usdAmount` beside
  it are floored display projections that predate fixed-scale money; nothing rounds here, so a
  cent cannot go missing.
- `usdReferenceMinorUnits` - the dollar reference the node stamped on the receipt, in exact
  minor units.
- `conversionTxHash` - the node's receipt reference for the sale. The column name predates the
  exchange and is kept only so the stored schema does not move; read it through
  `SellReceiptReference`.
- `payoutTxHash` - the node's exchange operation id for the sale. The column name predates the
  exchange; read it through `SellOperationId`.
- `stripePayoutId` - RETIRED. Nothing has written this since the payout provider moved onto the
  shared node, which owns the cash-out record. Kept only until the column can be dropped.

# Treasury

`TreasuryTransaction` is RETIRED. A project's money view is derived - see
`Services/Implementations/AGENTS.md`, "Project money", and `ProjectLedgerService` - from the rows
that already record each event: the sponsorship (`FundingIntent`), the award
(`TokenAllocation`), the buy-back (`PayoutRequest`) and the treasury receipt
(`PlatformTreasuryTransaction`). So this per-project ledger was never written and is never read.
Reviving it would create a second record of the same movements that can disagree with all four
sources, and the `Treasury.balance` it accumulated into would be a database number standing in
for money the node actually custodies: the balance of record is the project's custodial
treasury account on the node.

The retirement is a control, not a comment: the constructor (in `TreasuryTransaction.Custom.cs`)
is not public, so application code cannot create a row and a future writer gets a compile error
instead of quietly starting a second ledger. EF still materialises rows through it, which is why
it is `protected` rather than removed.

The columns are unchanged on purpose - the stored schema does not move. Dropping the table and
its `DbSet` (`Infrastructure/Data/ArdaNovaDbContext.cs:95`) is a MIGRATION, not an entity edit,
and belongs to whichever item next opens that file.

# Treasury receipts

`PlatformTreasuryTransaction` is a treasury RECEIPT: what ArdaNova recorded about a move the
node actually made. Not a ledger of record. The balance of record is the project's custodial
treasury account on the node; these rows explain that balance, they do not constitute it.

AZOA provenance (W5.3): the op id is what makes a receipt verifiable. A row with no
`azoaOperationId` attests to nothing on chain - it records ArdaNova's own policy split, not a
value move on the node. `IsChainReceipt` (`PlatformTreasuryTransaction.Custom.cs`) is that test.

Columns:

- `amount` - display projection of `amountMinorUnits`. Never an input to money math.
- `amountMinorUnits` - the exact amount, in minor units (cents). This is the authoritative
  figure.
- `balanceAfter` - display projection of `balanceAfterMinorUnits`. Never an input to money math.
- `balanceAfterMinorUnits` - the policy-view balance after this receipt, in minor units. Null
  when the row records a move whose resulting balance only the node knows - a receipt must never
  invent one.
- `azoaOperationId` - durable node operation this receipt is for.
- `azoaIdempotencyKey` - the stable key the node deduped the move on; the reconcile handle.
- `azoaReceiptReference` - opaque node receipt reference, safe to log and echo back.
- `chainAssetId` - the on-chain asset the move was counted in.
- `projectTreasuryAddress` - address of the project's node-side treasury this receipt credits.

# Membership credential

`MembershipCredential.issuedThroughNodeAt` is when the credential ISSUANCE NODE issued this
credential to its holder. It is its OWN column, not derived from `mintTxHash` or `assetId`: both
of those were also written by the removed address-based mint (which put the asset in the
PLATFORM's hands) and fabricated outright by the simulated provider, so a row predating the node
path carries them and proves nothing. Null on every such row, which is the fail-closed reading.
Written in exactly one place - `MembershipCredentialServices.UpdateMintInfoAsync`, and only when
the caller states `CredentialProofOrigin.IssuanceNode`.

# Project investment

`ProjectInvestment.showBackerName` is true only when the backer opted in ("show my name to other
backers") to let project members see their display name against this pledge. Defaults to false,
so a pledge stays anonymous unless the backer said otherwise.

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

# Payout sale admissions

`PayoutSaleQuote` freezes subject, exact command, price, quantity, denomination and expiry.
`PayoutRequest.requestKey` is unique per subject and quoteId is single-use. The nullable
exchangeAdmitBefore is assigned once before a new sale can send; null legacy rows cannot
acquire negative receipt proof. exchangeCommandJson copies the quote, never a new rate.
Currency defaults use PayoutSaleDenomination to match the exchange's ARDA scale.
