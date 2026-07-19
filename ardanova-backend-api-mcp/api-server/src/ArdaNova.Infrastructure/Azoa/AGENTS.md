# Azoa transport boundaries

ArdaNova uses four typed HTTP clients so credentials cannot drift across trust
boundaries:

- `AzoaPublicNodeClient` sends no API key and exposes only anonymous avatar registration.
- `AzoaCustodialNodeClient` sends `Azoa:CustodyApiKey` and accepts only
  `/api/tenant/custodial-accounts` routes. Its key is limited to
  `tenant:provision`, `wallet:manage`, `kyc:read`, and `kyc:submit`.
- `AzoaNodeClient` sends `Azoa:ValueApiKey` only to allocation and fungible-mint
  routes. Those calls currently need `nft:mint`; the key carries no onboarding
  or quest authority.
- `AzoaQuestNodeClient` sends `Azoa:QuestApiKey` only to `/api/quest` routes.
  Authoring requires `dapp:develop` and an API-key owner with the persisted dApp
  Developer role. Current read/validate/signal routes require authentication but
  do not enforce an additional quest scope.

`Azoa:TenantApiKey` is a migration fallback only outside Production. Production
ignores it, requires explicit keys for enabled capabilities, and rejects the
same secret across custody, value, and quest clients. Redirect following stays disabled
for every client so a scoped key cannot cross origins.

Production credentials also pass the shared generated-secret policy: at least
32 UTF-8 bytes, no placeholder markers, and no published README examples. Scope
names describe authority; they are never usable secret values.

All credential-bearing clients reject absolute destinations, query/fragment
suffixes, dot segments, and encoded path delimiters before network I/O. This
prevents URI normalization from moving a scoped key into another route family.

Node response messages are provider-controlled and never cross into API
results verbatim. They can contain stack traces, database details, or secret
material even when wrapped in a nominal error envelope. The transport maps
status codes to stable public messages and logs only request metadata. The
`KYC_FORBIDDEN` signal remains part of the public contract, but its provider
detail is replaced with a fixed message.

Production accepts AZOA as intentionally disabled only when no capability or
scoped credential is configured. Any partial capability configuration requires
an HTTPS base origin. Base URLs are origins, not arbitrary URIs: credentials,
paths, queries, and fragments are rejected so every typed client has one clear
authority and route policy.

Tenant custody remains anchored to the configured tenant GUID. Every returned
account status must match that tenant and the actor-derived ArdaNova user before
thin avatar, wallet, or KYC references are persisted.
