import type { PrismaClient, User, UserSkill, UserExperience } from "@prisma/client";

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

type DB = Pick<PrismaClient, "user" | "userSkill" | "userExperience">;

interface UserIdInput {
  db: DB;
  userId: string;
}

interface UpdateProfileInput extends UserIdInput {
  data: {
    name?: string;
    bio?: string;
    location?: string;
    phone?: string;
    website?: string;
    linkedIn?: string;
    twitter?: string;
  };
}

interface UpdateProfileImageInput extends UserIdInput {
  imageUrl: string;
}

interface AddSkillInput extends UserIdInput {
  skill: string;
  level?: number;
}

interface SkillOwnerInput {
  db: DB;
  skillId: string;
  userId: string;
}

interface UpdateSkillInput extends SkillOwnerInput {
  data: {
    skill?: string;
    level?: number;
  };
}

interface AddExperienceInput extends UserIdInput {
  data: {
    title: string;
    company: string;
    description?: string;
    startDate: Date;
    endDate?: Date;
    isCurrent?: boolean;
  };
}

interface ExperienceOwnerInput {
  db: DB;
  experienceId: string;
  userId: string;
}

interface UpdateExperienceInput extends ExperienceOwnerInput {
  data: {
    title?: string;
    company?: string;
    description?: string;
    startDate?: Date;
    endDate?: Date | null;
    isCurrent?: boolean;
  };
}

// ---------------------------------------------------------------------------
// Profile include config (reusable)
// ---------------------------------------------------------------------------

const profileInclude = {
  userSkills: true,
  userExperiences: {
    orderBy: { startDate: "desc" as const },
  },
} as const;

// ---------------------------------------------------------------------------
// Queries
// ---------------------------------------------------------------------------

/**
 * Get a user profile including skills and experiences.
 * Throws if user not found.
 */
export async function getProfile(input: UserIdInput) {
  const { db, userId } = input;

  const user = await db.user.findUnique({
    where: { id: userId },
    include: profileInclude,
  });

  if (!user) {
    throw new Error("User not found");
  }

  return user;
}

/**
 * Get a user's trust score and related reputation data.
 * Throws if user not found.
 */
export async function getTrustScore(input: UserIdInput) {
  const { db, userId } = input;

  const user = await db.user.findUnique({
    where: { id: userId },
    select: {
      trustScore: true,
      isVerified: true,
      verificationLevel: true,
      totalXP: true,
      level: true,
      tier: true,
    },
  });

  if (!user) {
    throw new Error("User not found");
  }

  return user;
}

// ---------------------------------------------------------------------------
// Mutations - Profile
// ---------------------------------------------------------------------------

/**
 * Update user profile fields (name, bio, location, phone, website, linkedIn, twitter).
 * Returns updated user with skills and experiences.
 */
export async function updateProfile(input: UpdateProfileInput) {
  const { db, userId, data } = input;

  return db.user.update({
    where: { id: userId },
    data,
    include: profileInclude,
  });
}

/**
 * Update user profile image.
 */
export async function updateProfileImage(input: UpdateProfileImageInput) {
  const { db, userId, imageUrl } = input;

  return db.user.update({
    where: { id: userId },
    data: { image: imageUrl },
  });
}

// ---------------------------------------------------------------------------
// Mutations - Skills
// ---------------------------------------------------------------------------

/**
 * Add a skill to the user's profile.
 * Defaults skill level to 1 if not provided.
 */
export async function addSkill(input: AddSkillInput): Promise<UserSkill> {
  const { db, userId, skill, level = 1 } = input;

  return db.userSkill.create({
    data: {
      userId,
      skill,
      level,
    },
  });
}

/**
 * Remove a skill from the user's profile.
 * Verifies ownership before deletion.
 */
export async function removeSkill(input: SkillOwnerInput): Promise<UserSkill> {
  const { db, skillId, userId } = input;

  const skill = await db.userSkill.findUnique({
    where: { id: skillId },
  });

  if (!skill) {
    throw new Error("Skill not found");
  }

  if (skill.userId !== userId) {
    throw new Error("You can only remove your own skills");
  }

  return db.userSkill.delete({
    where: { id: skillId },
  });
}

/**
 * Update a skill's data (level, name).
 * Verifies ownership before update.
 */
export async function updateSkill(input: UpdateSkillInput): Promise<UserSkill> {
  const { db, skillId, userId, data } = input;

  const skill = await db.userSkill.findUnique({
    where: { id: skillId },
  });

  if (!skill) {
    throw new Error("Skill not found");
  }

  if (skill.userId !== userId) {
    throw new Error("You can only update your own skills");
  }

  return db.userSkill.update({
    where: { id: skillId },
    data,
  });
}

// ---------------------------------------------------------------------------
// Mutations - Experience
// ---------------------------------------------------------------------------

/**
 * Add a work experience entry to the user's profile.
 */
export async function addExperience(
  input: AddExperienceInput
): Promise<UserExperience> {
  const { db, userId, data } = input;

  return db.userExperience.create({
    data: {
      userId,
      ...data,
    },
  });
}

/**
 * Remove an experience entry from the user's profile.
 * Verifies ownership before deletion.
 */
export async function removeExperience(
  input: ExperienceOwnerInput
): Promise<UserExperience> {
  const { db, experienceId, userId } = input;

  const experience = await db.userExperience.findUnique({
    where: { id: experienceId },
  });

  if (!experience) {
    throw new Error("Experience not found");
  }

  if (experience.userId !== userId) {
    throw new Error("You can only remove your own experience");
  }

  return db.userExperience.delete({
    where: { id: experienceId },
  });
}

/**
 * Update an experience entry.
 * Verifies ownership before update.
 */
export async function updateExperience(
  input: UpdateExperienceInput
): Promise<UserExperience> {
  const { db, experienceId, userId, data } = input;

  const experience = await db.userExperience.findUnique({
    where: { id: experienceId },
  });

  if (!experience) {
    throw new Error("Experience not found");
  }

  if (experience.userId !== userId) {
    throw new Error("You can only update your own experience");
  }

  return db.userExperience.update({
    where: { id: experienceId },
    data,
  });
}
