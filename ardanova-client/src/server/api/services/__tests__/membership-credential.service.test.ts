import { describe, it, expect, vi, beforeEach } from "vitest";
import {
  mintCredential,
  revokeCredential,
  suspendCredential,
  reactivateCredential,
  getCredentialsByProject,
  getCredentialsByUser,
  getCredentialByProjectAndUser,
} from "../membership-credential.service";

// ---------------------------------------------------------------------------
// Prisma mock
// ---------------------------------------------------------------------------

const mockFindUnique = vi.fn();
const mockFindMany = vi.fn();
const mockCreate = vi.fn();
const mockUpdate = vi.fn();

const db = {
  membershipCredential: {
    findUnique: mockFindUnique,
    findMany: mockFindMany,
    create: mockCreate,
    update: mockUpdate,
  },
} as unknown as Parameters<typeof mintCredential>[0]["db"];

beforeEach(() => {
  vi.resetAllMocks();
});

// ===========================================================================
// mintCredential
// ===========================================================================
describe("mintCredential", () => {
  const baseMintInput = {
    db,
    projectId: "project-1",
    userId: "user-1",
    grantedVia: "FOUNDER" as const,
  };

  it("should create a new ACTIVE, non-transferable credential with mintedAt set", async () => {
    mockFindUnique.mockResolvedValue(null); // no existing credential
    const fakeCredential = {
      id: "cred-1",
      projectId: "project-1",
      userId: "user-1",
      status: "ACTIVE",
      isTransferable: false,
      grantedVia: "FOUNDER",
      mintedAt: new Date(),
      createdAt: new Date(),
      updatedAt: new Date(),
    };
    mockCreate.mockResolvedValue(fakeCredential);

    const result = await mintCredential(baseMintInput);

    expect(result).toEqual(fakeCredential);
    expect(mockCreate).toHaveBeenCalledOnce();
    expect(mockCreate).toHaveBeenCalledWith({
      data: expect.objectContaining({
        projectId: "project-1",
        userId: "user-1",
        status: "ACTIVE",
        isTransferable: false,
        grantedVia: "FOUNDER",
        mintedAt: expect.any(Date),
      }),
    });
  });

  it("should throw if user already has an ACTIVE credential for the project", async () => {
    mockFindUnique.mockResolvedValue({
      id: "existing-cred",
      status: "ACTIVE",
    });

    await expect(mintCredential(baseMintInput)).rejects.toThrow(
      "User already has an active membership credential for this project"
    );
    expect(mockCreate).not.toHaveBeenCalled();
  });

  it("should throw if user has a SUSPENDED credential for the project", async () => {
    mockFindUnique.mockResolvedValue({
      id: "existing-cred",
      status: "SUSPENDED",
    });

    await expect(mintCredential(baseMintInput)).rejects.toThrow(
      "User already has an active membership credential for this project"
    );
    expect(mockCreate).not.toHaveBeenCalled();
  });

  it("should allow minting if previous credential was REVOKED", async () => {
    // A revoked credential means the unique constraint row exists,
    // but we should allow re-minting by updating the existing record
    mockFindUnique.mockResolvedValue({
      id: "existing-cred",
      status: "REVOKED",
    });
    const reactivatedCredential = {
      id: "existing-cred",
      status: "ACTIVE",
      isTransferable: false,
      grantedVia: "FOUNDER",
      mintedAt: new Date(),
    };
    mockUpdate.mockResolvedValue(reactivatedCredential);

    const result = await mintCredential(baseMintInput);

    expect(result).toEqual(reactivatedCredential);
    expect(mockUpdate).toHaveBeenCalledOnce();
    expect(mockCreate).not.toHaveBeenCalled();
  });

  it("should pass grantedByProposalId when provided", async () => {
    mockFindUnique.mockResolvedValue(null);
    const fakeCredential = {
      id: "cred-2",
      projectId: "project-1",
      userId: "user-1",
      status: "ACTIVE",
      isTransferable: false,
      grantedVia: "DAO_VOTE",
      grantedByProposalId: "proposal-1",
      mintedAt: new Date(),
      createdAt: new Date(),
      updatedAt: new Date(),
    };
    mockCreate.mockResolvedValue(fakeCredential);

    const result = await mintCredential({
      ...baseMintInput,
      grantedVia: "DAO_VOTE",
      grantedByProposalId: "proposal-1",
    });

    expect(result).toEqual(fakeCredential);
    expect(mockCreate).toHaveBeenCalledWith({
      data: expect.objectContaining({
        grantedVia: "DAO_VOTE",
        grantedByProposalId: "proposal-1",
      }),
    });
  });
});

// ===========================================================================
// revokeCredential
// ===========================================================================
describe("revokeCredential", () => {
  it("should set status to REVOKED and record revokedAt", async () => {
    mockFindUnique.mockResolvedValue({
      id: "cred-1",
      status: "ACTIVE",
    });
    const revokedCred = {
      id: "cred-1",
      status: "REVOKED",
      revokedAt: new Date(),
    };
    mockUpdate.mockResolvedValue(revokedCred);

    const result = await revokeCredential({
      db,
      credentialId: "cred-1",
    });

    expect(result.status).toBe("REVOKED");
    expect(mockUpdate).toHaveBeenCalledWith({
      where: { id: "cred-1" },
      data: expect.objectContaining({
        status: "REVOKED",
        revokedAt: expect.any(Date),
      }),
    });
  });

  it("should include revokeTxHash when provided", async () => {
    mockFindUnique.mockResolvedValue({
      id: "cred-1",
      status: "ACTIVE",
    });
    mockUpdate.mockResolvedValue({
      id: "cred-1",
      status: "REVOKED",
      revokeTxHash: "0xabc",
    });

    await revokeCredential({
      db,
      credentialId: "cred-1",
      revokeTxHash: "0xabc",
    });

    expect(mockUpdate).toHaveBeenCalledWith({
      where: { id: "cred-1" },
      data: expect.objectContaining({
        revokeTxHash: "0xabc",
      }),
    });
  });

  it("should throw if credential not found", async () => {
    mockFindUnique.mockResolvedValue(null);

    await expect(
      revokeCredential({ db, credentialId: "nonexistent" })
    ).rejects.toThrow("Membership credential not found");
  });

  it("should throw if credential is already REVOKED", async () => {
    mockFindUnique.mockResolvedValue({
      id: "cred-1",
      status: "REVOKED",
    });

    await expect(
      revokeCredential({ db, credentialId: "cred-1" })
    ).rejects.toThrow("Credential is already revoked");
  });
});

// ===========================================================================
// suspendCredential
// ===========================================================================
describe("suspendCredential", () => {
  it("should set status to SUSPENDED for an ACTIVE credential", async () => {
    mockFindUnique.mockResolvedValue({
      id: "cred-1",
      status: "ACTIVE",
    });
    mockUpdate.mockResolvedValue({
      id: "cred-1",
      status: "SUSPENDED",
    });

    const result = await suspendCredential({ db, credentialId: "cred-1" });

    expect(result.status).toBe("SUSPENDED");
    expect(mockUpdate).toHaveBeenCalledWith({
      where: { id: "cred-1" },
      data: { status: "SUSPENDED" },
    });
  });

  it("should throw if credential not found", async () => {
    mockFindUnique.mockResolvedValue(null);

    await expect(
      suspendCredential({ db, credentialId: "nonexistent" })
    ).rejects.toThrow("Membership credential not found");
  });

  it("should throw if credential is not ACTIVE", async () => {
    mockFindUnique.mockResolvedValue({
      id: "cred-1",
      status: "REVOKED",
    });

    await expect(
      suspendCredential({ db, credentialId: "cred-1" })
    ).rejects.toThrow("Only ACTIVE credentials can be suspended");
  });
});

// ===========================================================================
// reactivateCredential
// ===========================================================================
describe("reactivateCredential", () => {
  it("should set status to ACTIVE for a SUSPENDED credential", async () => {
    mockFindUnique.mockResolvedValue({
      id: "cred-1",
      status: "SUSPENDED",
    });
    mockUpdate.mockResolvedValue({
      id: "cred-1",
      status: "ACTIVE",
    });

    const result = await reactivateCredential({ db, credentialId: "cred-1" });

    expect(result.status).toBe("ACTIVE");
    expect(mockUpdate).toHaveBeenCalledWith({
      where: { id: "cred-1" },
      data: { status: "ACTIVE" },
    });
  });

  it("should throw if credential not found", async () => {
    mockFindUnique.mockResolvedValue(null);

    await expect(
      reactivateCredential({ db, credentialId: "nonexistent" })
    ).rejects.toThrow("Membership credential not found");
  });

  it("should throw if credential is not SUSPENDED", async () => {
    mockFindUnique.mockResolvedValue({
      id: "cred-1",
      status: "ACTIVE",
    });

    await expect(
      reactivateCredential({ db, credentialId: "cred-1" })
    ).rejects.toThrow("Only SUSPENDED credentials can be reactivated");
  });

  it("should throw if credential is REVOKED (cannot reactivate revoked)", async () => {
    mockFindUnique.mockResolvedValue({
      id: "cred-1",
      status: "REVOKED",
    });

    await expect(
      reactivateCredential({ db, credentialId: "cred-1" })
    ).rejects.toThrow("Only SUSPENDED credentials can be reactivated");
  });
});

// ===========================================================================
// Query operations
// ===========================================================================
describe("getCredentialsByProject", () => {
  it("should return all credentials for a project", async () => {
    const creds = [
      { id: "cred-1", projectId: "project-1", status: "ACTIVE" },
      { id: "cred-2", projectId: "project-1", status: "REVOKED" },
    ];
    mockFindMany.mockResolvedValue(creds);

    const result = await getCredentialsByProject({
      db,
      projectId: "project-1",
    });

    expect(result).toEqual(creds);
    expect(mockFindMany).toHaveBeenCalledWith({
      where: { projectId: "project-1" },
      include: { user: true },
      orderBy: { createdAt: "desc" },
    });
  });
});

describe("getCredentialsByUser", () => {
  it("should return all credentials for a user", async () => {
    const creds = [
      { id: "cred-1", userId: "user-1", status: "ACTIVE" },
    ];
    mockFindMany.mockResolvedValue(creds);

    const result = await getCredentialsByUser({ db, userId: "user-1" });

    expect(result).toEqual(creds);
    expect(mockFindMany).toHaveBeenCalledWith({
      where: { userId: "user-1" },
      include: { project: true },
      orderBy: { createdAt: "desc" },
    });
  });
});

describe("getCredentialByProjectAndUser", () => {
  it("should return the unique credential for a user in a project", async () => {
    const cred = {
      id: "cred-1",
      projectId: "project-1",
      userId: "user-1",
      status: "ACTIVE",
    };
    mockFindUnique.mockResolvedValue(cred);

    const result = await getCredentialByProjectAndUser({
      db,
      projectId: "project-1",
      userId: "user-1",
    });

    expect(result).toEqual(cred);
    expect(mockFindUnique).toHaveBeenCalledWith({
      where: {
        projectId_userId: {
          projectId: "project-1",
          userId: "user-1",
        },
      },
    });
  });

  it("should return null if no credential exists", async () => {
    mockFindUnique.mockResolvedValue(null);

    const result = await getCredentialByProjectAndUser({
      db,
      projectId: "project-1",
      userId: "user-1",
    });

    expect(result).toBeNull();
  });
});
