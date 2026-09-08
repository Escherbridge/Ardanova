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

Funding rechecks the project owner after external payment confirmation and
immediately before a funding-allocation dispatch. An incomplete owner leaves
the payment in a durable pending settlement/outbox and retries the leased
outbox without gateway submission; it never synthesizes a refund.
