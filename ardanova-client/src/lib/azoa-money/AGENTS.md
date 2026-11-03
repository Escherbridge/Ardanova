# Host money integration

`azoa-sdk/money` and `azoa-sdk/react` own the money workflows. ArdaNova supplies authenticated tRPC transport, runtime allowlist projection, durable subject-scoped storage, redirect policy and visual slots. No AZOA bearer or first-party node session enters this tier.

All amounts cross the seam as safe integer minor units. Sales use exact ARDA base units from the durable quote/record, never the USD reference or a floating legacy amount. Whole project units use decimal count zero. The backend prices and authorizes every sale.

An empty or malformed successful response is unconfirmed. Keep request keys across lost replies, consent failure and reload; no local discard proves that a cash-out or sale did not happen. The required checkout, cash-out and quote-bound sale void methods forward explicit server void-or-reveal responses. Only an atomic absent-key tombstone permits forgetting an unresolved key; a revealed named record acknowledges the admitted request. A named immutable terminal sale/cash-out record is evidence; a status code or missing record is not.

Checkout lifecycle values must match the SDK state machine; unknown values stay unconfirmed. A consent revocation receipt requires the server's timestamp. The BFF may return a null timestamp when permission was already absent; do not manufacture a dated receipt from that response. Read account status separately for current permission.

`ArdaConsentStatus` refreshes the shared account read once after a refused revocation action, including the absent-timestamp case. A fresh `consent:none` read renders permission inactive for either ownership mode. A failed read does not trigger another read automatically; the explicit Check permission action permits recovery.

The ownership gate creates only metadata-bound handoffs. `operator_stub` means delivery is pending, never that an email was sent. Raw claim links are visible only to the node operator and recipient, and payout setup uses the member's first-party session on the hosted node. A successful permission grant does not prove the bank destination is ready; the withdrawal read checks it afterward.

SDK 0.6 production imports require publication and a Linux-regenerated lockfile before deployment. Verification may overlay a reviewed built package in an isolated checkout only. Do not add vendor fallbacks or claim unpublished package adoption.

Every host request carries `expectedSubject` as a session continuity guard. The tRPC procedure compares it with the authenticated subject and never forwards it as business identity. Provider cleanup retires its host callbacks before a queued Web Lock can send for a different browser session. Strict SDK and legacy-storage reads distinguish a missing slot from an unreadable prior request; migration cannot overwrite corrupt data.
