# KYC & Identity Verification — Technical Specification

## Overview

The KYC system gates access to project creation and membership credential issuance by requiring users to reach `VerificationLevel.PRO` through identity verification. It uses a **provider abstraction pattern** to support manual review (default) with a path to Veriff integration behind a feature flag.

## Design Principles

- **PRO gates credentials** — users must be PRO to create projects or receive membership credentials
- **Provider abstraction** — Strategy pattern allows swapping MANUAL ↔ VERIFF without changing business logic
- **Feature flag** — `KYC_PROVIDER` env var controls active provider; Veriff behind dev flag
- **Reuse existing infrastructure** — S3/AttachmentService for document storage, UserService for level updates
- **Graceful UX** — blocked states show clear CTAs directing users to KYC flow

---

## Verification Level Gating

| Level | Access |
|-------|--------|
| `ANONYMOUS` | Browse projects, view public content |
| `VERIFIED` | Apply to opportunities, join guilds as observer, basic participation |
| `PRO` | **Create projects**, **receive membership credentials**, vote in governance, full platform access |
| `EXPERT` | Reserved for future premium features |

### How Users Reach PRO

```
User Signs Up (Google OAuth)
    │
    ▼
VerificationLevel = ANONYMOUS
    │
    ▼ (email verified via OAuth)
VerificationLevel = VERIFIED (auto, via NextAuth signIn callback)
    │
    ▼ (submits KYC documents)
KycSubmission.status = PENDING
    │
    ▼ (admin reviews / Veriff verifies)
KycSubmission.status = APPROVED
    │
    ▼ (auto-upgrade on approval)
VerificationLevel = PRO
    │
    ▼ (can now create projects, receive credentials)
Platform fully unlocked
```

**Note:** The signIn callback in `src/server/auth/config.ts` already sets `emailVerified` on Google OAuth. A small update will auto-set `verificationLevel = VERIFIED` when `emailVerified` is set and current level is ANONYMOUS.

---

## Schema Design

### KycSubmission Table

```dbml
Table KycSubmission {
  id varchar [not null, pk, default: `cuid()`]
  userId varchar [not null]
  provider KycProvider [not null, default: 'MANUAL']
  status KycStatus [not null, default: 'PENDING']
  reviewerId varchar [note: 'Admin who reviewed']
  reviewNotes text [note: 'Internal admin notes']
  rejectionReason varchar [note: 'User-facing rejection reason']
  providerSessionId varchar [note: 'External provider session ID']
  providerResult text [note: 'Raw JSON from provider']
  submittedAt datetime [not null, default: `now()`]
  reviewedAt datetime
  expiresAt datetime [note: 'KYC approval expiration']
  createdAt datetime [not null, default: `now()`]
  updatedAt datetime [not null, note: 'Updated at']

  indexes {
    (userId, status) [note: 'Query active submissions per user']
  }
}
Ref: KycSubmission.userId > User.id
Ref: KycSubmission.reviewerId > User.id
```

### KycDocument Table

```dbml
Table KycDocument {
  id varchar [not null, pk, default: `cuid()`]
  submissionId varchar [not null]
  type KycDocumentType [not null]
  fileUrl varchar [not null, note: 'S3/storage URL']
  fileName varchar [not null]
  mimeType varchar
  fileSizeBytes int
  metadata text [note: 'JSON: country, expiryDate, etc.']
  createdAt datetime [not null, default: `now()`]

  indexes {
    (submissionId, type) [unique, note: 'One document per type per submission']
  }
}
Ref: KycDocument.submissionId > KycSubmission.id
```

### Enums

```dbml
Enum KycStatus {
  PENDING
  IN_REVIEW
  APPROVED
  REJECTED
  EXPIRED
}

Enum KycDocumentType {
  GOVERNMENT_ID
  PASSPORT
  DRIVERS_LICENSE
  SELFIE
  PROOF_OF_ADDRESS
}

Enum KycProvider {
  MANUAL
  VERIFF
}
```

---

## Provider Abstraction (Strategy Pattern)

```
┌──────────────────────────────────────────────────────────────────┐
│                   KYC PROVIDER ARCHITECTURE                       │
├──────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌─────────────────────────────────────────────────────────┐     │
│  │                    IKycService                           │     │
│  │                  (Business Logic)                        │     │
│  │                                                          │     │
│  │  • SubmitAsync → validate + store + call provider       │     │
│  │  • ApproveAsync → update status + upgrade user to PRO   │     │
│  │  • RejectAsync → update status + store reason           │     │
│  │  • GetByUserIdAsync → current submission                │     │
│  │  • GetPendingAsync → admin queue                        │     │
│  └───────────────────────┬─────────────────────────────────┘     │
│                           │                                       │
│                           ▼                                       │
│  ┌─────────────────────────────────────────────────────────┐     │
│  │              IKycProviderService                         │     │
│  │             (Provider Abstraction)                        │     │
│  │                                                          │     │
│  │  • CreateSessionAsync(userId, documents)                 │     │
│  │  • GetSessionStatusAsync(sessionId)                      │     │
│  │  • HandleWebhookAsync(payload)                           │     │
│  │  • ValidateDocumentsAsync(documents)                     │     │
│  └──────────────┬────────────────────┬─────────────────────┘     │
│                  │                    │                            │
│        ┌─────────▼──────────┐  ┌─────▼──────────────────┐       │
│        │ ManualKycProvider  │  │ VeriffKycProvider       │       │
│        │   Service          │  │   Service               │       │
│        │ (Default)          │  │ (Feature Flag)          │       │
│        │                    │  │                          │       │
│        │ • Store docs in S3 │  │ • Create Veriff session │       │
│        │ • Admin reviews    │  │ • Redirect to Veriff UI │       │
│        │ • Manual approve   │  │ • Webhook → auto-decide │       │
│        └────────────────────┘  └──────────────────────────┘       │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────┐     │
│  │              IKycGateService                             │     │
│  │           (Reusable Verification Checks)                 │     │
│  │                                                          │     │
│  │  • RequireProAsync(userId) → pass or 403                │     │
│  │  • RequireVerifiedAsync(userId) → pass or 403           │     │
│  │                                                          │     │
│  │  Injected into:                                          │     │
│  │  ├── ProjectService.CreateAsync()                        │     │
│  │  ├── MembershipCredentialService.GrantAsync()            │     │
│  │  └── CredentialUtilityService.GrantAndMintAsync()        │     │
│  └─────────────────────────────────────────────────────────┘     │
│                                                                   │
└──────────────────────────────────────────────────────────────────┘
```

### DI Registration (Conditional)

```csharp
// In DependencyInjection.cs
public static IServiceCollection AddKycServices(
    this IServiceCollection services, IConfiguration config)
{
    services.AddScoped<IKycService, KycService>();
    services.AddScoped<IKycGateService, KycGateService>();

    var kycSettings = config.GetSection("Kyc").Get<KycSettings>();

    if (kycSettings?.Provider == "veriff")
        services.AddScoped<IKycProviderService, VeriffKycProviderService>();
    else
        services.AddScoped<IKycProviderService, ManualKycProviderService>();

    return services;
}
```

---

## Feature Flag: KYC_PROVIDER

| Value | Behavior |
|-------|----------|
| `manual` (default) | Documents uploaded to S3, admin reviews in dashboard, manual approve/reject |
| `veriff` | Veriff SDK session created, user redirected to Veriff UI, webhook auto-decides |

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `KYC_PROVIDER` | Active KYC provider | `manual` |
| `VERIFF_API_KEY` | Veriff API key (required if provider=veriff) | — |
| `VERIFF_BASE_URL` | Veriff API base URL | `https://stationapi.veriff.com/v1` |
| `KYC_SUBMISSION_EXPIRY_DAYS` | Days until approved KYC expires (0 = never) | `0` |

---

## KYC Submission State Machine

```
                    ┌─────────┐
                    │ PENDING │ ← User submits documents
                    └────┬────┘
                         │
                    ┌────▼─────┐
              ┌─────│ IN_REVIEW│ ← Admin picks up / Veriff processing
              │     └────┬─────┘
              │          │
         ┌────▼────┐ ┌──▼──────┐
         │REJECTED │ │APPROVED │ → User upgraded to PRO
         └────┬────┘ └────┬────┘
              │           │
              ▼           ▼ (if expiry configured)
         Can re-submit  ┌───────┐
                        │EXPIRED│ → User must re-verify
                        └───────┘
```

### Valid Transitions

| From | To | Trigger |
|------|----|---------|
| PENDING | IN_REVIEW | Admin opens submission / Veriff starts processing |
| IN_REVIEW | APPROVED | Admin approves / Veriff decision = approved |
| IN_REVIEW | REJECTED | Admin rejects / Veriff decision = declined |
| APPROVED | EXPIRED | Expiry timer (if configured) |

### On Approval Side Effects

1. `KycSubmission.status` → APPROVED
2. `KycSubmission.reviewedAt` → now
3. `User.verificationLevel` → PRO (via `UserService.UpdateVerificationLevelAsync`)
4. User's JWT will reflect PRO on next token refresh (NextAuth JWT callback reads from DB)

---

## Veriff Integration Points (Future)

When `KYC_PROVIDER=veriff` is enabled:

### Session Creation
```
POST https://stationapi.veriff.com/v1/sessions
{
  "verification": {
    "person": { "firstName": "...", "lastName": "..." },
    "vendorData": "user_cuid_xxx",
    "callback": "https://api.ardanova.com/api/Kyc/webhook"
  }
}
→ Returns: { sessionUrl, sessionId }
→ Frontend redirects user to sessionUrl
```

### Webhook Handler
```
POST /api/Kyc/webhook
{
  "status": "success",
  "verification": {
    "id": "session_xxx",
    "status": "approved" | "declined" | "resubmission_requested",
    "vendorData": "user_cuid_xxx"
  }
}
→ Map to KycStatus, update submission, auto-approve/reject
```

### Decision Mapping

| Veriff Status | KycStatus | Action |
|---------------|-----------|--------|
| `approved` | APPROVED | Auto-upgrade user to PRO |
| `declined` | REJECTED | Store reason, allow re-submit |
| `resubmission_requested` | REJECTED | Store reason, prompt re-submit |
| `expired` | EXPIRED | Prompt new submission |

---

## KYC Gate Integration Points

### ProjectService.CreateAsync() (line ~166)

```csharp
public async Task<Result<ProjectDto>> CreateAsync(CreateProjectDto dto, CancellationToken ct = default)
{
    // NEW: KYC gate check
    var gateResult = await _kycGateService.RequireProAsync(dto.CreatedById, ct);
    if (!gateResult.IsSuccess) return Result<ProjectDto>.Forbidden(gateResult.Error);

    // ... existing logic ...
}
```

### MembershipCredentialService.GrantAsync() (line ~59)

```csharp
public async Task<Result<MembershipCredentialDto>> GrantAsync(GrantMembershipCredentialDto dto, CancellationToken ct = default)
{
    // NEW: KYC gate check
    var gateResult = await _kycGateService.RequireProAsync(dto.UserId, ct);
    if (!gateResult.IsSuccess) return Result<MembershipCredentialDto>.Forbidden(gateResult.Error);

    // ... existing logic ...
}
```

---

## Security Considerations

1. **Document Storage**: All KYC documents stored via existing S3/AttachmentService with presigned URLs (time-limited access)
2. **No PII in Logs**: Never log document contents, file URLs, or personal data. Log only submission IDs and status transitions
3. **Admin-Only Review**: Approve/reject endpoints require ADMIN role check
4. **Webhook Verification**: Veriff webhooks must be verified with HMAC signature before processing
5. **Rate Limiting**: Limit KYC submissions per user (prevent spam — one active submission at a time)
6. **Document Retention**: Consider retention policy for rejected/expired submissions (GDPR/compliance)
7. **Encryption**: Documents encrypted at rest in S3 (existing infra)

---

## File Manifest

### New Files

| File | Layer | Purpose |
|------|-------|---------|
| `ArdaNova.Application/DTOs/KycDtos.cs` | Application | KYC data transfer objects |
| `ArdaNova.Application/Settings/KycSettings.cs` | Application | Configuration POCO |
| `ArdaNova.Application/Services/Interfaces/IKycService.cs` | Application | KYC business logic interface |
| `ArdaNova.Application/Services/Interfaces/IKycProviderService.cs` | Application | Provider abstraction |
| `ArdaNova.Application/Services/Interfaces/IKycGateService.cs` | Application | Verification gate interface |
| `ArdaNova.Application/Services/Implementations/KycService.cs` | Application | KYC business logic |
| `ArdaNova.Application/Services/Implementations/ManualKycProviderService.cs` | Application | Manual review provider |
| `ArdaNova.Application/Services/Implementations/KycGateService.cs` | Application | Verification gate |
| `ArdaNova.Infrastructure/Kyc/VeriffKycProviderService.cs` | Infrastructure | Veriff integration (behind flag) |
| `ArdaNova.API/Controllers/KycController.cs` | API | REST endpoints |
| `ardanova-client/src/lib/api/ardanova/endpoints/kyc.ts` | Frontend | API client |
| `ardanova-client/src/server/api/routers/kyc.ts` | Frontend | tRPC thin proxy |
| `tests/ArdaNova.Application.Tests/Services/KycServiceTests.cs` | Tests | Service tests |
| `tests/ArdaNova.Application.Tests/Services/KycGateServiceTests.cs` | Tests | Gate tests |

### Modified Files

| File | Changes |
|------|---------|
| `ardanova-client/prisma/database-architecture.dbml` | Add KycSubmission, KycDocument tables + 3 enums |
| `ArdaNova.Application/Services/Implementations/ProjectServices.cs` | Inject IKycGateService, add PRO check in CreateAsync |
| `ArdaNova.Application/Services/Implementations/MembershipCredentialServices.cs` | Inject IKycGateService, add PRO check in GrantAsync |
| `ArdaNova.Application/DependencyInjection.cs` | Register KYC services with conditional provider |
| `ArdaNova.Application/Mappings/MappingProfile.cs` | Add KYC entity → DTO mappings |
| `ardanova-client/src/lib/api/ardanova/index.ts` | Register kyc endpoint |
| `ardanova-client/src/server/api/root.ts` | Register kyc router |
| `ardanova-client/src/server/auth/config.ts` | Auto-set VERIFIED on OAuth with email |

### Integration with Track 07 (Credential Utility)

When Track 07 is implemented, `CredentialUtilityService.GrantAndMintAsync()` and `CheckAndAutoGrantAsync()` will call `IKycGateService.RequireProAsync()` before proceeding. The KYC gate is designed as a standalone injectable service specifically for this cross-track reuse.
