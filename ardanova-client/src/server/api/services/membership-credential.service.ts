import type { PrismaClient, MembershipCredential } from "@prisma/client";
import type { MembershipGrantType } from "@prisma/client";

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

type DB = Pick<PrismaClient, "membershipCredential">;

interface MintCredentialInput {
  db: DB;
  projectId: string;
  userId: string;
  grantedVia: MembershipGrantType;
  grantedByProposalId?: string;
}

interface CredentialIdInput {
  db: DB;
  credentialId: string;
}

interface RevokeCredentialInput extends CredentialIdInput {
  revokeTxHash?: string;
}

interface ProjectInput {
  db: DB;
  projectId: string;
}

interface UserInput {
  db: DB;
  userId: string;
}

interface ProjectAndUserInput {
  db: DB;
  projectId: string;
  userId: string;
}

// ---------------------------------------------------------------------------
// Mutations
// ---------------------------------------------------------------------------

/**
 * Mint a new non-transferable membership credential for a user in a project.
 *
 * - Enforces uniqueness: one credential per (projectId, userId).
 * - If a REVOKED credential already exists, re-activates it instead of creating a new row.
 * - Rejects if an ACTIVE or SUSPENDED credential already exists.
 */
export async function mintCredential(
  input: MintCredentialInput
): Promise<MembershipCredential> {
  const { db, projectId, userId, grantedVia, grantedByProposalId } = input;

  // Check for existing credential (unique constraint: projectId + userId)
  const existing = await db.membershipCredential.findUnique({
    where: { projectId_userId: { projectId, userId } },
  });

  if (existing) {
    if (existing.status === "REVOKED") {
      // Re-mint: update the existing revoked credential back to ACTIVE
      return db.membershipCredential.update({
        where: { id: existing.id },
        data: {
          status: "ACTIVE",
          isTransferable: false,
          grantedVia,
          grantedByProposalId: grantedByProposalId ?? null,
          mintedAt: new Date(),
          revokedAt: null,
          revokeTxHash: null,
        },
      });
    }

    throw new Error(
      "User already has an active membership credential for this project"
    );
  }

  return db.membershipCredential.create({
    data: {
      projectId,
      userId,
      status: "ACTIVE",
      isTransferable: false,
      grantedVia,
      grantedByProposalId: grantedByProposalId ?? undefined,
      mintedAt: new Date(),
    },
  });
}

/**
 * Revoke a membership credential. Sets status to REVOKED with timestamp.
 * Cannot revoke an already-revoked credential.
 */
export async function revokeCredential(
  input: RevokeCredentialInput
): Promise<MembershipCredential> {
  const { db, credentialId, revokeTxHash } = input;

  const credential = await db.membershipCredential.findUnique({
    where: { id: credentialId },
  });

  if (!credential) {
    throw new Error("Membership credential not found");
  }

  if (credential.status === "REVOKED") {
    throw new Error("Credential is already revoked");
  }

  return db.membershipCredential.update({
    where: { id: credentialId },
    data: {
      status: "REVOKED",
      revokedAt: new Date(),
      ...(revokeTxHash ? { revokeTxHash } : {}),
    },
  });
}

/**
 * Suspend an ACTIVE credential. Only ACTIVE credentials can be suspended.
 */
export async function suspendCredential(
  input: CredentialIdInput
): Promise<MembershipCredential> {
  const { db, credentialId } = input;

  const credential = await db.membershipCredential.findUnique({
    where: { id: credentialId },
  });

  if (!credential) {
    throw new Error("Membership credential not found");
  }

  if (credential.status !== "ACTIVE") {
    throw new Error("Only ACTIVE credentials can be suspended");
  }

  return db.membershipCredential.update({
    where: { id: credentialId },
    data: { status: "SUSPENDED" },
  });
}

/**
 * Reactivate a SUSPENDED credential back to ACTIVE.
 * Only SUSPENDED credentials can be reactivated (not REVOKED).
 */
export async function reactivateCredential(
  input: CredentialIdInput
): Promise<MembershipCredential> {
  const { db, credentialId } = input;

  const credential = await db.membershipCredential.findUnique({
    where: { id: credentialId },
  });

  if (!credential) {
    throw new Error("Membership credential not found");
  }

  if (credential.status !== "SUSPENDED") {
    throw new Error("Only SUSPENDED credentials can be reactivated");
  }

  return db.membershipCredential.update({
    where: { id: credentialId },
    data: { status: "ACTIVE" },
  });
}

// ---------------------------------------------------------------------------
// Queries
// ---------------------------------------------------------------------------

/**
 * Get all membership credentials for a project, ordered by creation date (newest first).
 */
export async function getCredentialsByProject(input: ProjectInput) {
  const { db, projectId } = input;

  return db.membershipCredential.findMany({
    where: { projectId },
    include: { user: true },
    orderBy: { createdAt: "desc" },
  });
}

/**
 * Get all membership credentials for a user, ordered by creation date (newest first).
 */
export async function getCredentialsByUser(input: UserInput) {
  const { db, userId } = input;

  return db.membershipCredential.findMany({
    where: { userId },
    include: { project: true },
    orderBy: { createdAt: "desc" },
  });
}

/**
 * Get the unique credential for a specific user in a specific project.
 * Returns null if none exists.
 */
export async function getCredentialByProjectAndUser(
  input: ProjectAndUserInput
) {
  const { db, projectId, userId } = input;

  return db.membershipCredential.findUnique({
    where: {
      projectId_userId: { projectId, userId },
    },
  });
}
