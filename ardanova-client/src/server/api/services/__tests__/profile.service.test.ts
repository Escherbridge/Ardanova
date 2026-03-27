import { describe, it, expect, vi, beforeEach } from "vitest";
import {
  getProfile,
  updateProfile,
  updateProfileImage,
  getTrustScore,
  addSkill,
  removeSkill,
  updateSkill,
  addExperience,
  removeExperience,
  updateExperience,
} from "../profile.service";

// ---------------------------------------------------------------------------
// Prisma mock
// ---------------------------------------------------------------------------

const mockUserFindUnique = vi.fn();
const mockUserUpdate = vi.fn();

const mockSkillCreate = vi.fn();
const mockSkillFindUnique = vi.fn();
const mockSkillUpdate = vi.fn();
const mockSkillDelete = vi.fn();

const mockExperienceCreate = vi.fn();
const mockExperienceFindUnique = vi.fn();
const mockExperienceUpdate = vi.fn();
const mockExperienceDelete = vi.fn();

const db = {
  user: {
    findUnique: mockUserFindUnique,
    update: mockUserUpdate,
  },
  userSkill: {
    create: mockSkillCreate,
    findUnique: mockSkillFindUnique,
    update: mockSkillUpdate,
    delete: mockSkillDelete,
  },
  userExperience: {
    create: mockExperienceCreate,
    findUnique: mockExperienceFindUnique,
    update: mockExperienceUpdate,
    delete: mockExperienceDelete,
  },
} as unknown as Parameters<typeof getProfile>[0]["db"];

beforeEach(() => {
  vi.resetAllMocks();
});

// ===========================================================================
// getProfile
// ===========================================================================
describe("getProfile", () => {
  it("should return user profile with skills and experiences", async () => {
    const fakeUser = {
      id: "user-1",
      name: "John Doe",
      email: "john@example.com",
      bio: "A bio",
      location: "Lagos",
      phone: "+234",
      website: "https://john.dev",
      linkedIn: "https://linkedin.com/in/john",
      twitter: "@john",
      image: "https://img.com/john.jpg",
      role: "INDIVIDUAL",
      userType: "INNOVATOR",
      isVerified: false,
      totalXP: 100,
      level: 2,
      tier: "BRONZE",
      trustScore: 0.5,
      verificationLevel: "ANONYMOUS",
      createdAt: new Date(),
      updatedAt: new Date(),
      userSkills: [{ id: "skill-1", skill: "TypeScript", level: 3 }],
      userExperiences: [
        {
          id: "exp-1",
          title: "Developer",
          company: "Acme",
          startDate: new Date(),
          isCurrent: true,
        },
      ],
    };
    mockUserFindUnique.mockResolvedValue(fakeUser);

    const result = await getProfile({ db, userId: "user-1" });

    expect(result).toEqual(fakeUser);
    expect(mockUserFindUnique).toHaveBeenCalledWith({
      where: { id: "user-1" },
      include: {
        userSkills: true,
        userExperiences: {
          orderBy: { startDate: "desc" },
        },
      },
    });
  });

  it("should throw if user not found", async () => {
    mockUserFindUnique.mockResolvedValue(null);

    await expect(getProfile({ db, userId: "nonexistent" })).rejects.toThrow(
      "User not found"
    );
  });
});

// ===========================================================================
// updateProfile
// ===========================================================================
describe("updateProfile", () => {
  it("should update profile fields and return updated user", async () => {
    const updatedUser = {
      id: "user-1",
      name: "Jane Doe",
      bio: "Updated bio",
      location: "Abuja",
    };
    mockUserUpdate.mockResolvedValue(updatedUser);

    const result = await updateProfile({
      db,
      userId: "user-1",
      data: {
        name: "Jane Doe",
        bio: "Updated bio",
        location: "Abuja",
      },
    });

    expect(result).toEqual(updatedUser);
    expect(mockUserUpdate).toHaveBeenCalledWith({
      where: { id: "user-1" },
      data: {
        name: "Jane Doe",
        bio: "Updated bio",
        location: "Abuja",
      },
      include: {
        userSkills: true,
        userExperiences: {
          orderBy: { startDate: "desc" },
        },
      },
    });
  });

  it("should allow partial updates", async () => {
    const updatedUser = { id: "user-1", name: "Only Name" };
    mockUserUpdate.mockResolvedValue(updatedUser);

    const result = await updateProfile({
      db,
      userId: "user-1",
      data: { name: "Only Name" },
    });

    expect(result).toEqual(updatedUser);
    expect(mockUserUpdate).toHaveBeenCalledWith({
      where: { id: "user-1" },
      data: { name: "Only Name" },
      include: {
        userSkills: true,
        userExperiences: {
          orderBy: { startDate: "desc" },
        },
      },
    });
  });

  it("should update social links", async () => {
    const updatedUser = {
      id: "user-1",
      linkedIn: "https://linkedin.com/in/jane",
      twitter: "@jane",
    };
    mockUserUpdate.mockResolvedValue(updatedUser);

    const result = await updateProfile({
      db,
      userId: "user-1",
      data: {
        linkedIn: "https://linkedin.com/in/jane",
        twitter: "@jane",
      },
    });

    expect(result).toEqual(updatedUser);
    expect(mockUserUpdate).toHaveBeenCalledWith({
      where: { id: "user-1" },
      data: {
        linkedIn: "https://linkedin.com/in/jane",
        twitter: "@jane",
      },
      include: {
        userSkills: true,
        userExperiences: {
          orderBy: { startDate: "desc" },
        },
      },
    });
  });
});

// ===========================================================================
// updateProfileImage
// ===========================================================================
describe("updateProfileImage", () => {
  it("should update user image and return updated user", async () => {
    const updatedUser = { id: "user-1", image: "https://img.com/new.jpg" };
    mockUserUpdate.mockResolvedValue(updatedUser);

    const result = await updateProfileImage({
      db,
      userId: "user-1",
      imageUrl: "https://img.com/new.jpg",
    });

    expect(result).toEqual(updatedUser);
    expect(mockUserUpdate).toHaveBeenCalledWith({
      where: { id: "user-1" },
      data: { image: "https://img.com/new.jpg" },
    });
  });
});

// ===========================================================================
// getTrustScore
// ===========================================================================
describe("getTrustScore", () => {
  it("should return user trust score", async () => {
    mockUserFindUnique.mockResolvedValue({
      trustScore: 0.85,
      isVerified: true,
      verificationLevel: "VERIFIED",
      totalXP: 500,
      level: 5,
      tier: "SILVER",
    });

    const result = await getTrustScore({ db, userId: "user-1" });

    expect(result).toEqual({
      trustScore: 0.85,
      isVerified: true,
      verificationLevel: "VERIFIED",
      totalXP: 500,
      level: 5,
      tier: "SILVER",
    });
    expect(mockUserFindUnique).toHaveBeenCalledWith({
      where: { id: "user-1" },
      select: {
        trustScore: true,
        isVerified: true,
        verificationLevel: true,
        totalXP: true,
        level: true,
        tier: true,
      },
    });
  });

  it("should throw if user not found", async () => {
    mockUserFindUnique.mockResolvedValue(null);

    await expect(getTrustScore({ db, userId: "nonexistent" })).rejects.toThrow(
      "User not found"
    );
  });
});

// ===========================================================================
// addSkill
// ===========================================================================
describe("addSkill", () => {
  it("should create a new skill for the user", async () => {
    const fakeSkill = {
      id: "skill-1",
      userId: "user-1",
      skill: "TypeScript",
      level: 3,
    };
    mockSkillCreate.mockResolvedValue(fakeSkill);

    const result = await addSkill({
      db,
      userId: "user-1",
      skill: "TypeScript",
      level: 3,
    });

    expect(result).toEqual(fakeSkill);
    expect(mockSkillCreate).toHaveBeenCalledWith({
      data: {
        userId: "user-1",
        skill: "TypeScript",
        level: 3,
      },
    });
  });

  it("should default skill level to 1", async () => {
    const fakeSkill = {
      id: "skill-2",
      userId: "user-1",
      skill: "React",
      level: 1,
    };
    mockSkillCreate.mockResolvedValue(fakeSkill);

    const result = await addSkill({
      db,
      userId: "user-1",
      skill: "React",
    });

    expect(result).toEqual(fakeSkill);
    expect(mockSkillCreate).toHaveBeenCalledWith({
      data: {
        userId: "user-1",
        skill: "React",
        level: 1,
      },
    });
  });
});

// ===========================================================================
// removeSkill
// ===========================================================================
describe("removeSkill", () => {
  it("should delete the skill if owned by user", async () => {
    mockSkillFindUnique.mockResolvedValue({
      id: "skill-1",
      userId: "user-1",
      skill: "TypeScript",
      level: 3,
    });
    mockSkillDelete.mockResolvedValue({
      id: "skill-1",
      userId: "user-1",
      skill: "TypeScript",
      level: 3,
    });

    const result = await removeSkill({
      db,
      skillId: "skill-1",
      userId: "user-1",
    });

    expect(result).toEqual(
      expect.objectContaining({ id: "skill-1", userId: "user-1" })
    );
    expect(mockSkillDelete).toHaveBeenCalledWith({
      where: { id: "skill-1" },
    });
  });

  it("should throw if skill not found", async () => {
    mockSkillFindUnique.mockResolvedValue(null);

    await expect(
      removeSkill({ db, skillId: "nonexistent", userId: "user-1" })
    ).rejects.toThrow("Skill not found");
  });

  it("should throw if skill does not belong to user", async () => {
    mockSkillFindUnique.mockResolvedValue({
      id: "skill-1",
      userId: "other-user",
    });

    await expect(
      removeSkill({ db, skillId: "skill-1", userId: "user-1" })
    ).rejects.toThrow("You can only remove your own skills");
  });
});

// ===========================================================================
// updateSkill
// ===========================================================================
describe("updateSkill", () => {
  it("should update skill level if owned by user", async () => {
    mockSkillFindUnique.mockResolvedValue({
      id: "skill-1",
      userId: "user-1",
      skill: "TypeScript",
      level: 3,
    });
    const updatedSkill = {
      id: "skill-1",
      userId: "user-1",
      skill: "TypeScript",
      level: 5,
    };
    mockSkillUpdate.mockResolvedValue(updatedSkill);

    const result = await updateSkill({
      db,
      skillId: "skill-1",
      userId: "user-1",
      data: { level: 5 },
    });

    expect(result).toEqual(updatedSkill);
    expect(mockSkillUpdate).toHaveBeenCalledWith({
      where: { id: "skill-1" },
      data: { level: 5 },
    });
  });

  it("should throw if skill not found", async () => {
    mockSkillFindUnique.mockResolvedValue(null);

    await expect(
      updateSkill({ db, skillId: "nonexistent", userId: "user-1", data: { level: 5 } })
    ).rejects.toThrow("Skill not found");
  });

  it("should throw if skill does not belong to user", async () => {
    mockSkillFindUnique.mockResolvedValue({
      id: "skill-1",
      userId: "other-user",
    });

    await expect(
      updateSkill({ db, skillId: "skill-1", userId: "user-1", data: { level: 5 } })
    ).rejects.toThrow("You can only update your own skills");
  });
});

// ===========================================================================
// addExperience
// ===========================================================================
describe("addExperience", () => {
  it("should create a new experience entry for the user", async () => {
    const startDate = new Date("2023-01-01");
    const fakeExperience = {
      id: "exp-1",
      userId: "user-1",
      title: "Senior Developer",
      company: "TechCorp",
      description: "Built things",
      startDate,
      endDate: null,
      isCurrent: true,
    };
    mockExperienceCreate.mockResolvedValue(fakeExperience);

    const result = await addExperience({
      db,
      userId: "user-1",
      data: {
        title: "Senior Developer",
        company: "TechCorp",
        description: "Built things",
        startDate,
        isCurrent: true,
      },
    });

    expect(result).toEqual(fakeExperience);
    expect(mockExperienceCreate).toHaveBeenCalledWith({
      data: {
        userId: "user-1",
        title: "Senior Developer",
        company: "TechCorp",
        description: "Built things",
        startDate,
        isCurrent: true,
      },
    });
  });
});

// ===========================================================================
// removeExperience
// ===========================================================================
describe("removeExperience", () => {
  it("should delete experience if owned by user", async () => {
    mockExperienceFindUnique.mockResolvedValue({
      id: "exp-1",
      userId: "user-1",
    });
    mockExperienceDelete.mockResolvedValue({
      id: "exp-1",
      userId: "user-1",
    });

    const result = await removeExperience({
      db,
      experienceId: "exp-1",
      userId: "user-1",
    });

    expect(result).toEqual(
      expect.objectContaining({ id: "exp-1", userId: "user-1" })
    );
    expect(mockExperienceDelete).toHaveBeenCalledWith({
      where: { id: "exp-1" },
    });
  });

  it("should throw if experience not found", async () => {
    mockExperienceFindUnique.mockResolvedValue(null);

    await expect(
      removeExperience({ db, experienceId: "nonexistent", userId: "user-1" })
    ).rejects.toThrow("Experience not found");
  });

  it("should throw if experience does not belong to user", async () => {
    mockExperienceFindUnique.mockResolvedValue({
      id: "exp-1",
      userId: "other-user",
    });

    await expect(
      removeExperience({ db, experienceId: "exp-1", userId: "user-1" })
    ).rejects.toThrow("You can only remove your own experience");
  });
});

// ===========================================================================
// updateExperience
// ===========================================================================
describe("updateExperience", () => {
  it("should update experience if owned by user", async () => {
    mockExperienceFindUnique.mockResolvedValue({
      id: "exp-1",
      userId: "user-1",
    });
    const updatedExperience = {
      id: "exp-1",
      userId: "user-1",
      title: "Lead Developer",
      company: "TechCorp",
    };
    mockExperienceUpdate.mockResolvedValue(updatedExperience);

    const result = await updateExperience({
      db,
      experienceId: "exp-1",
      userId: "user-1",
      data: { title: "Lead Developer" },
    });

    expect(result).toEqual(updatedExperience);
    expect(mockExperienceUpdate).toHaveBeenCalledWith({
      where: { id: "exp-1" },
      data: { title: "Lead Developer" },
    });
  });

  it("should throw if experience not found", async () => {
    mockExperienceFindUnique.mockResolvedValue(null);

    await expect(
      updateExperience({
        db,
        experienceId: "nonexistent",
        userId: "user-1",
        data: { title: "New Title" },
      })
    ).rejects.toThrow("Experience not found");
  });

  it("should throw if experience does not belong to user", async () => {
    mockExperienceFindUnique.mockResolvedValue({
      id: "exp-1",
      userId: "other-user",
    });

    await expect(
      updateExperience({
        db,
        experienceId: "exp-1",
        userId: "user-1",
        data: { title: "New Title" },
      })
    ).rejects.toThrow("You can only update your own experience");
  });
});
