# MCP tool boundaries

The stdio MCP host currently has no trusted end-user principal. Tools in this
directory may expose non-sensitive discovery operations, but must not accept a
caller-supplied user ID as authorization or invoke value-, role-, credential-,
or governance-changing services directly.

Escrow mutation tools were removed because they bypassed the actor and funder
checks enforced by the HTTP API. Reintroduce them only after the transport
provides a verified principal and the tool layer shares the HTTP authorization
policy rather than duplicating it.

Membership credential lifecycle, vote-delegation, and referral reward-claim
tools are removed for the same reason. Grant reasons, delegator identities, and
reward amounts supplied by an MCP caller are data, not authority; reward
derivation and beneficiary binding must also be server-owned, atomic, and
idempotent.
