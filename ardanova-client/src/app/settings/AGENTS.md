# Settings UI boundaries

Wallet settings only manage public external-address records and their displayed
verification state. Never request secrets, invoke a signer, export assets, or
imply that a listed address is a custody, settlement, or payout destination.
Payout controls remain disabled until a server-issued provider flow, policy
checks, and durable settlement reconciliation are implemented. See
`conductor/tracks/gated-commerce-and-azoa-settlement/plan.md`.
