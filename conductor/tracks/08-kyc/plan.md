# Track 08 — KYC & Identity Verification

## 1. Schema — KYC Submission & Document Tables

- [x] **[P0] DBML: Add KycSubmission table** [cad695c]
    - `id varchar [pk, default: cuid()]`
    - `userId varchar [not null, FK → User]`
    - `provider KycProvider [not null, default: 'MANUAL']` — MANUAL or VERIFF
    - `status KycStatus [not null, default: 'PENDING']` — PENDING, IN_REVIEW, APPROVED, REJECTED, EXPIRED
    - `reviewerId varchar [FK → User]` — admin who reviewed (nullable)
    - `reviewNotes text` — admin notes on decision
    - `rejectionReason varchar` — user-facing reason if rejected
    - `providerSessionId varchar` — Veriff session ID (nullable, for VERIFF provider)
    - `providerResult text` — raw JSON response from provider (nullable)
    - `submittedAt datetime [not null, default: now()]`
    - `reviewedAt datetime` — when admin reviewed
    - `expiresAt datetime` — KYC approval expiration (nullable)
    - `createdAt datetime [not null, default: now()]`
    - `updatedAt datetime [not null]`
    - Index: `(userId) [unique]` — one active submission per user
    - Run `npm run generate:prisma` + `npm run generate:csharp`

- [x] **[P0] DBML: Add KycDocument table** [cad695c]
    - `id varchar [pk, default: cuid()]`
    - `submissionId varchar [not null, FK → KycSubmission]`
    - `type KycDocumentType [not null]` — GOVERNMENT_ID, PASSPORT, DRIVERS_LICENSE, SELFIE, PROOF_OF_ADDRESS
    - `fileUrl varchar [not null]` — S3/storage URL (via existing AttachmentService)
    - `fileName varchar [not null]`
    - `mimeType varchar`
    - `fileSizeBytes int`
    - `metadata text` — JSON metadata (e.g., document country, expiry)
    - `createdAt datetime [not null, default: now()]`
    - Index: `(submissionId, type) [unique]` — one document per type per submission

- [x] **[P0] DBML: Add KYC enums** [cad695c]
    - `KycStatus`: PENDING, IN_REVIEW, APPROVED, REJECTED, EXPIRED
    - `KycDocumentType`: GOVERNMENT_ID, PASSPORT, DRIVERS_LICENSE, SELFIE, PROOF_OF_ADDRESS
    - `KycProvider`: MANUAL, VERIFF

- [x] **[P0] Run generators** [cad695c]
    - `npm run generate:prisma` from `ardanova-client/`
    - `npm run generate:csharp` from `ardanova-client/`

## 2. Backend — KYC Service + Provider Abstraction

- [x] **[P0] DTOs: KYC data transfer objects** [c13d6fb]
    - `KycSubmissionDto` — full submission with status, documents, provider
    - `SubmitKycDto` — userId (from auth), documentIds (pre-uploaded via AttachmentService)
    - `ReviewKycDto` — approved/rejected, reviewNotes, rejectionReason
    - `KycStatusDto` — lightweight status check response (status, submittedAt, reviewedAt)
    - `KycDocumentDto` — document metadata for display

- [x] **[P0] IKycProviderService: Provider abstraction interface** [c13d6fb]
    - Create `ArdaNova.Application/Services/Interfaces/IKycProviderService.cs`
    - `CreateSessionAsync(userId, documents, ct)` → providerSessionId
    - `GetSessionStatusAsync(providerSessionId, ct)` → KycStatus
    - `HandleWebhookAsync(payload, ct)` → KycStatus + result data
    - `ValidateDocumentsAsync(documents, ct)` → validation result

- [x] **[P0] ManualKycProviderService: Internal review implementation** [c13d6fb]
    - Create `ArdaNova.Application/Services/Implementations/ManualKycProviderService.cs`
    - `CreateSessionAsync` → stores documents, returns internal submission ID
    - `GetSessionStatusAsync` → returns current DB status
    - `HandleWebhookAsync` → no-op for manual (admin uses review endpoint)
    - `ValidateDocumentsAsync` → basic file type + size validation

- [x] **[P1] VeriffKycProviderService: Veriff integration (behind feature flag)** [c13d6fb]
    - Create `ArdaNova.Infrastructure/Kyc/VeriffKycProviderService.cs`
    - `CreateSessionAsync` → POST to Veriff API, create session, return sessionId + redirect URL
    - `GetSessionStatusAsync` → GET from Veriff API, map to KycStatus
    - `HandleWebhookAsync` → parse Veriff webhook payload, map decision to KycStatus
    - `ValidateDocumentsAsync` → Veriff handles validation
    - Behind `KYC_PROVIDER=veriff` environment variable

- [x] **[P0] IKycService / KycService: Core KYC business logic** [c13d6fb]
    - Create `ArdaNova.Application/Services/Interfaces/IKycService.cs`
    - Create `ArdaNova.Application/Services/Implementations/KycService.cs`
    - `SubmitAsync(userId, documents, ct)` → creates KycSubmission + KycDocuments, calls provider
    - `GetByUserIdAsync(userId, ct)` → current submission status
    - `GetByIdAsync(id, ct)` → full submission details
    - `GetPendingAsync(ct)` → all submissions awaiting review (admin)
    - `ApproveAsync(id, reviewerId, notes, ct)` → approve + auto-upgrade user to PRO
    - `RejectAsync(id, reviewerId, notes, rejectionReason, ct)` → reject with reason
    - `ExpireAsync(id, ct)` → mark expired (for timed KYC, future use)
    - On approve: call `UserService.UpdateVerificationLevelAsync(userId, PRO)`
    - Prevent duplicate active submissions per user
    - Rejected users can re-submit (creates new submission)

- [x] **[P0] KycSettings: Configuration POCO** [c13d6fb]
    - Create `ArdaNova.Application/Settings/KycSettings.cs`
    - `Provider`: "manual" (default) or "veriff"
    - `VeriffApiKey`: API key (nullable, only for veriff provider)
    - `VeriffBaseUrl`: API base URL (nullable)
    - `SubmissionExpiryDays`: how long approval lasts (nullable, default null = never)
    - Bind from `KYC_PROVIDER`, `VERIFF_API_KEY`, `VERIFF_BASE_URL` environment variables

- [x] **[P0] DependencyInjection: Register KYC services** [c13d6fb]
    - Register `IKycService` → `KycService` (always)
    - Conditional registration based on `KycSettings.Provider`:
      - `"manual"` → `IKycProviderService` → `ManualKycProviderService`
      - `"veriff"` → `IKycProviderService` → `VeriffKycProviderService`
    - Register `IKycGateService` → `KycGateService`

- [x] **[P0] MappingProfile: Add KYC mappings** [c13d6fb]
    - `KycSubmission` → `KycSubmissionDto`
    - `KycDocument` → `KycDocumentDto`

## 3. KYC Gate Service — Reusable Verification Checks

- [x] **[P0] IKycGateService / KycGateService: Verification gate** [3092a41]
    - Create `ArdaNova.Application/Services/Interfaces/IKycGateService.cs`
    - Create `ArdaNova.Application/Services/Implementations/KycGateService.cs`
    - `RequireProAsync(userId, ct)` → `Result<bool>` — returns success if PRO+, error with redirect message if not
    - `RequireVerifiedAsync(userId, ct)` → success if VERIFIED+
    - `GetVerificationLevelAsync(userId, ct)` → current VerificationLevel
    - Reads `User.verificationLevel` via `IRepository<User>` (direct repository access)
    - Error messages include KYC flow URL for frontend redirect

- [x] **[P0] Gate: ProjectService.CreateAsync() — block non-PRO** [3092a41]
    - Inject `IKycGateService` into `ProjectService`
    - Add `RequireProAsync(dto.CreatedById)` check at top of `CreateAsync()`
    - Return `Result.Forbidden("KYC verification required to create projects")`

- [x] **[P0] Gate: MembershipCredentialService.GrantAsync() — block non-PRO** [3092a41]
    - Inject `IKycGateService` into `MembershipCredentialService`
    - Add `RequireProAsync(dto.UserId)` check at top of `GrantAsync()`
    - Return `Result.Forbidden("KYC verification required for membership credentials")`
    - Exception: FOUNDER grants during project creation are pre-validated (creator already passed PRO check)

- [ ] **[P1] Gate: Future CredentialUtilityService integration**
    - `GrantAndMintAsync` will call `RequireProAsync` before proceeding
    - `CheckAndAutoGrantAsync` will verify KYC status before auto-granting

## 4. Controller + API Client + tRPC Router

- [x] **[P1] KycController: REST endpoints** [5de1f09]
    - Create `ArdaNova.API/Controllers/KycController.cs`
    - `POST /api/Kyc/submit` — submit KYC with document references
    - `GET /api/Kyc/status/{userId}` — check KYC status (own or admin)
    - `GET /api/Kyc/{id}` — get full submission details
    - `GET /api/Kyc/pending` — get all pending submissions (admin only)
    - `POST /api/Kyc/{id}/approve` — approve submission (admin only)
    - `POST /api/Kyc/{id}/reject` — reject submission (admin only)
    - `POST /api/Kyc/webhook` — Veriff webhook endpoint (when enabled)

- [x] **[P1] API Client: kyc.ts endpoint wrapper** [5de1f09]
    - Create `ardanova-client/src/lib/api/ardanova/endpoints/kyc.ts`
    - `submit(data: SubmitKycDto)` → POST /api/Kyc/submit
    - `getStatus(userId: string)` → GET /api/Kyc/status/{userId}
    - `getById(id: string)` → GET /api/Kyc/{id}
    - `getPending()` → GET /api/Kyc/pending
    - `approve(id: string, data: ReviewKycDto)` → POST /api/Kyc/{id}/approve
    - `reject(id: string, data: ReviewKycDto)` → POST /api/Kyc/{id}/reject
    - TypeScript interfaces mirroring DTOs

- [x] **[P1] Register in ArdaNovaApiClient** [5de1f09]
    - Add `kyc` endpoint to `ardanova-client/src/lib/api/ardanova/index.ts`

- [x] **[P1] tRPC Router: kyc.ts thin proxy** [5de1f09]
    - Create `ardanova-client/src/server/api/routers/kyc.ts`
    - `submit` — protectedProcedure, calls apiClient.kyc.submit
    - `getMyStatus` — protectedProcedure, calls apiClient.kyc.getStatus(ctx.session.user.id)
    - `getById` — protectedProcedure, calls apiClient.kyc.getById
    - `getPending` — adminProcedure, calls apiClient.kyc.getPending
    - `approve` — adminProcedure, calls apiClient.kyc.approve
    - `reject` — adminProcedure, calls apiClient.kyc.reject
    - Register in `src/server/api/root.ts`

## 5. Frontend UI

- [x] **[P0] Admin role bootstrap**
    - `manlytaco3@gmail.com` auto-assigned ADMIN role on sign-in
    - `PLATFORM_ADMIN_EMAILS` constant in auth config
    - Existing users auto-promoted on next sign-in

- [x] **[P2] KYC submission page**
    - Created `ardanova-client/src/app/settings/verification/page.tsx`
    - Settings layout + redirect from `/settings` → `/settings/verification`
    - Document upload form (government ID, passport, driver's license)
    - Status display (pending/in-review/approved/rejected)
    - Re-submission flow after rejection with rejection reason display
    - Swiss brutalist design with neon accents

- [x] **[P2] KYC status banner**
    - Created `ardanova-client/src/components/kyc/kyc-status-banner.tsx`
    - Verification status indicator with 6 states
    - Compact mode for inline usage
    - "Get Verified" CTA for non-submitted users

- [x] **[P2] Blocked-state UIs**
    - Project creation: KYC gate in `ProjectForm` checks `session.user.verificationLevel`
    - Shows "Verification Required" card with link to `/settings/verification`
    - `meetsVerificationLevel()` helper for verification level comparison

- [x] **[P2] Admin KYC review dashboard**
    - Created `ardanova-client/src/app/admin/layout.tsx` with ADMIN role guard
    - Created `ardanova-client/src/app/admin/kyc/page.tsx`
    - Lists pending submissions with documents
    - Approve/reject with inline rejection form
    - Query invalidation after mutations
    - Admin nav link in sidebar (conditional on ADMIN role)

## 6. Tests

- [x] **[P0] KycService unit tests**
    - Create `tests/ArdaNova.Application.Tests/Services/KycServiceTests.cs`
    - Submit KYC — creates submission + documents, calls provider
    - Approve — sets status APPROVED, upgrades user to PRO
    - Reject — sets status REJECTED, stores rejection reason
    - Duplicate prevention — cannot submit if PENDING/IN_REVIEW exists
    - Re-submit after rejection — creates new submission
    - Status transitions — only valid transitions allowed (PENDING → IN_REVIEW → APPROVED/REJECTED)
    - 17 tests: SubmitAsync (5), GetByIdAsync (2), GetByUserIdAsync (2), GetPendingAsync (1), ApproveAsync (4), RejectAsync (3)
    - Follow existing pattern: xUnit + Moq + FluentAssertions

- [x] **[P0] KycGateService unit tests** [3092a41]
    - Create `tests/ArdaNova.Application.Tests/Services/KycGateServiceTests.cs`
    - PRO user passes `RequireProAsync`
    - EXPERT user passes `RequireProAsync`
    - VERIFIED user blocked by `RequireProAsync`
    - ANONYMOUS user blocked by `RequireProAsync`
    - VERIFIED user passes `RequireVerifiedAsync`
    - Error messages include KYC redirect info
    - 13 tests: RequireProAsync (5), RequireVerifiedAsync (5), GetVerificationLevelAsync (3)

- [x] **[P0] Gate integration tests** [3092a41]
    - Project creation returns 403 for non-PRO user
    - Project creation succeeds for PRO user (via default mock setup)
    - Membership credential grant returns 403 for non-PRO user
    - Membership credential grant succeeds for PRO user (via default mock setup)

- [x] **[P1] ManualKycProviderService tests**
    - Document validation (accepted types, size limits)
    - Session creation stores documents correctly
    - Status returns current DB state
    - 15 tests: ValidateDocumentsAsync (8), CreateSessionAsync (3), GetSessionStatusAsync (2), HandleWebhookAsync (2)

- [ ] **[P1] KycController integration tests**
    - Submit endpoint creates submission
    - Status endpoint returns correct status
    - Approve endpoint restricted to admin role
    - Reject endpoint restricted to admin role
    - Webhook endpoint (placeholder for Veriff)
