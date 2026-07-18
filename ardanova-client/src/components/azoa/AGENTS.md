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
steps. Azoa KYC is authoritative. External wallets in ArdaNova settings remain
separate non-custodial references and need their own signed ownership proof.
Errors should explain which service or prerequisite is unavailable without
exposing tenant keys, internal URLs, provider payloads, or sensitive identity
data.
