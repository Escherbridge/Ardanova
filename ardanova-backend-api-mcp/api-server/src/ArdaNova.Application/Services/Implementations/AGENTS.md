# Opportunity role conversion

Project-wizard roles cross the opportunity boundary as typed allocation and
application-state fields. `OpportunityService` maps the open flag to the
existing lifecycle status and stores optional equity metadata in a versioned
compensation envelope, avoiding a schema-only field that older deployments
cannot read. API responses decode the envelope back into typed fields.

# Value-action readiness

`AzoaValueEligibilityService` is the single provider-authoritative check for
value-bearing actions. It fails closed unless the account has a stable avatar,
approved identity verification, and a managed account id and address in
addition to every aggregate readiness signal. Draft project creation and
ordinary unpaid collaboration deliberately do not call it; callers add it only
after actor authorization and before an action can promise, move, or receive
value.

The gate has THREE outcomes, not two, and the difference is load-bearing.
`Forbidden` means the account was read and this person genuinely has setup steps
left. `Conflict` means readiness could not be confirmed at all — AZOA unreachable,
a malformed answer, a node outage — and says nothing about the person. Flattening
Conflict into Forbidden blames someone who already finished setup for the platform
being down and sends them back to a flow they completed. Callers propagate the
outcome with `ValueEligibilityFailure.Propagate` (services) or
`ValueEligibilityFailure.StatusCode` (controllers, 409 vs 403); the outbox emits
`AZOA_ELIGIBILITY_UNCONFIRMED` rather than `AZOA_ACCOUNT_NOT_READY` so an operator
triages the platform, not the recipient. Never restate the rule inline.

Funding rechecks the project owner after external payment confirmation and
immediately before a funding-allocation dispatch. An incomplete owner leaves
the payment in a durable pending settlement/outbox and retries the leased
outbox without gateway submission; it never synthesizes a refund.
