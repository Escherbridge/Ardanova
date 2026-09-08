# Azoa account surfaces

## Boundary

ArdaNova is the onboarding and explanation surface; Azoa is the authoritative
tenant-isolated identity, KYC, custody, and chain-wallet service. ArdaNova may
store an avatar id, wallet id/address, KYC state, and readiness evidence. It
must never receive or render a mnemonic, private key, signing secret, custody
credential, or raw KYC document.

Account creation is an explicit user action. Reads may refresh status, but page
mounts must not silently create avatars, wallets, verification submissions, or
value-moving instructions. A successful account setup is not authorization to
fund, mint, bridge, swap, release, or pay.

Do not collect arbitrary identity-document URLs. A reference-based provider is
not a user-ready upload flow; it stays unavailable in ArdaNova until Azoa owns
private upload, opaque object identifiers, scanning, retention/deletion,
audited reviewer access, and expiry.

## Status language

The profile presents identity, avatar, and managed-wallet readiness as separate
steps, each shown only as **Ready** / **Not created** — never a wallet
address, an avatar GUID, or the literal word "Azoa" in copy the sponsor reads.
Azoa KYC is authoritative and stays server-side; ArdaNova only reflects the
readiness boolean it returns. Errors should explain which service or
prerequisite is unavailable without exposing tenant keys, internal URLs,
provider payloads, or sensitive identity data.

User-facing copy calls this **account setup** and explains the practical
boundary: drafts and collaboration can continue while setup is pending, but
funding, paid work, rewards, and payouts wait for server-confirmed readiness.
Completed setup is never presented as authorization for a particular payment
or project action.

## No external-wallet surface

Ratified decision #9 and D46: the managed account stays invisible to the
sponsor, with no opt-in path to an external wallet. There is no
`/settings/wallets` route, no public-address entry form, no `shortenAddress`
helper, and no sidebar or payouts-page link into one — none of that is a
toggle sitting one click away for an "advanced" user to find. A "claim to an
external wallet" feature, if ever wanted, is a new, deliberately designed
surface with its own signed-ownership-proof flow — not a dormant page or CTA
to re-enable.
