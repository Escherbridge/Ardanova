import { Prisma, PrismaClient } from "@prisma/client";

import { assertLocalDemoSeedAllowed } from "../src/lib/local-demo-seed-guard";

const DAY = 24 * 60 * 60 * 1000;
const now = new Date();
const PREVIEW_USER_ID = "local-preview-user";
const PREVIEW_USER_EMAIL = "preview@local.ardanova.test";

/**
 * Tandem-demo personas (conductor/tracks/mvp-tandem-demo_20261024). Each is a stable,
 * independent identity -- NOT the DEV_AUTH_BYPASS preview row above -- so the creator's
 * project, the sponsor's funding action and the freelancer's bid/application all have a
 * real, distinct `createdById`/`applicantId`/`bidderId` to point at.
 *
 * KNOWN LIMITATION (read before wiring Playwright/API-driven acts to one of these):
 * `DEV_AUTH_BYPASS` resolves to exactly ONE hard-coded identity --
 * `DEVELOPMENT_PREVIEW_USER_ID = "local-preview-user"` in
 * `ardanova-client/src/server/auth/index.ts:28` -- and `auth()`'s ownership/membership
 * checks compare against that literal id. None of these three personas can be "acted as"
 * through the browser bypass today: there is no env var or request header that repoints
 * `DEVELOPMENT_PREVIEW_USER_ID`, so `project.createdById === session.user.id` (see
 * `src/app/projects/[slug]/page.tsx`'s `isOwner`) will never be true for `demo-creator`
 * while signed in through the bypass. Driving the demo as each persona needs ONE of:
 *   (a) a small, additive change to `src/server/auth/index.ts` making
 *       `DEVELOPMENT_PREVIEW_USER_ID`/`DEVELOPMENT_PREVIEW_USER_EMAIL` read from an env var
 *       (default unchanged, so nothing existing breaks) that the Playwright/API harness
 *       sets per persona before each act -- this file is outside WS-B's partition;
 *   (b) real Google sign-in per persona; or
 *   (c) driving the paid/funding steps directly against the .NET API with a signed actor
 *       assertion for the chosen persona's id, bypassing the Next.js session entirely for
 *       the API-driven parts of the flow.
 * Flagged for WS-C, whose job is exactly "fix bring-up breaks" ahead of the golden-path
 * drive.
 */
const DEMO_PERSONAS = {
  creator: {
    id: "demo-creator",
    email: "demo-creator@local.ardanova.test",
    name: "Demo Creator",
    bio: "Publishes the tandem-demo project and runs its funding wizard live.",
    role: "INDIVIDUAL",
    userType: "INNOVATOR",
  },
  sponsor: {
    id: "demo-sponsor",
    email: "demo-sponsor@local.ardanova.test",
    name: "Demo Sponsor",
    bio: "Adds funds and sponsors the tandem-demo project live.",
    role: "INDIVIDUAL",
    userType: "SUPPORTER",
  },
  freelancer: {
    id: "demo-freelancer",
    email: "demo-freelancer@local.ardanova.test",
    name: "Demo Freelancer",
    bio: "Applies to and bids on the tandem-demo project's paid role live.",
    role: "INDIVIDUAL",
    userType: "FREELANCER",
  },
} as const satisfies Record<
  "creator" | "sponsor" | "freelancer",
  Pick<
    Prisma.UserUncheckedCreateInput,
    "id" | "email" | "name" | "bio" | "role" | "userType"
  >
>;
const HEAT_HIERARCHY_IDS = {
  milestone: "local-qa-milestone-heat-evidence",
  epic: "local-qa-epic-heat-evidence",
  sprint: "local-qa-sprint-heat-evidence-01",
  feature: "local-qa-feature-resident-heat-map",
  pbi: "local-qa-pbi-resident-heat-evidence",
  member: "local-qa-member-preview-heat",
  comment: "local-qa-comment-heat-steward-note",
} as const;

async function seedLocalDemo(prisma: PrismaClient) {
  const owner = await prisma.user.upsert({
    where: { email: PREVIEW_USER_EMAIL },
    update: {
      name: "ArdaNova Preview Steward",
      bio: "The synthetic local-only preview identity for repeatable interface QA.",
      location: "Local QA workspace",
      role: "INDIVIDUAL",
      userType: "VOLUNTEER",
      isVerified: true,
      verificationLevel: "VERIFIED",
    },
    create: {
      id: PREVIEW_USER_ID,
      email: PREVIEW_USER_EMAIL,
      name: "ArdaNova Preview Steward",
      bio: "The synthetic local-only preview identity for repeatable interface QA.",
      location: "Local QA workspace",
      role: "INDIVIDUAL",
      userType: "VOLUNTEER",
      isVerified: true,
      verificationLevel: "VERIFIED",
    },
  });

  if (owner.id !== PREVIEW_USER_ID) {
    throw new Error(
      `The local preview email already belongs to ${owner.id}; use a fresh dedicated demo database so it can use ${PREVIEW_USER_ID}.`,
    );
  }

  const projectFixtures = [
    {
      title: "Neighborhood Heat Commons",
      slug: "neighborhood-heat-commons",
      description:
        "Residents are mapping dangerous summer heat and shaping practical shade, water, and cooling interventions.",
      problemStatement:
        "Heat exposure is rising, but neighborhood-level evidence and resident priorities remain fragmented.",
      solution:
        "Combine resident field notes with temperature walks, then turn the findings into a reviewed intervention brief.",
      categories: "ENVIRONMENT,SOCIAL_IMPACT",
      tags: "heat,resilience,public-space",
      projectType: "COMMUNITY",
      status: "PUBLISHED",
      duration: "THREE_SIX_MONTHS",
      expectedImpact:
        "A resident-owned evidence set and three implementation-ready cooling proposals.",
      targetAudience:
        "Residents, public-health workers, and neighborhood groups",
      createdById: owner.id,
      publishedAt: now,
      featured: true,
    },
    {
      title: "Repair Library Pilot",
      slug: "repair-library-pilot",
      description:
        "A lending and learning pilot that helps neighbors repair household goods instead of replacing them.",
      problemStatement:
        "Useful objects become waste because tools, repair knowledge, and trusted help are difficult to access.",
      solution:
        "Run a six-week tool library and repair clinic, documenting demand, safety needs, and a durable operating model.",
      categories: "ENVIRONMENT,EDUCATION",
      tags: "repair,circular-economy,skills",
      projectType: "COMMUNITY",
      status: "SEEKING_SUPPORT",
      duration: "ONE_THREE_MONTHS",
      fundingGoal: 12000,
      currentFunding: 3400,
      supportersCount: 28,
      createdById: owner.id,
      publishedAt: now,
    },
    {
      title: "Community Wi-Fi Kit",
      slug: "community-wifi-kit",
      description:
        "A resident-governed kit for extending affordable connectivity without surveillance-based business models.",
      problemStatement:
        "Connectivity gaps persist while common alternatives trade community autonomy for opaque data collection.",
      solution:
        "Test a small mesh deployment with explicit stewardship, privacy, maintenance, and cost agreements.",
      categories: "TECHNOLOGY,SOCIAL_IMPACT",
      tags: "connectivity,privacy,community-infrastructure",
      projectType: "OPEN_SOURCE",
      status: "IN_PROGRESS",
      duration: "SIX_TWELVE_MONTHS",
      createdById: owner.id,
      publishedAt: new Date(now.getTime() - 30 * DAY),
    },
  ] satisfies Prisma.ProjectUncheckedCreateInput[];

  const projects = new Map<string, string>();
  for (const data of projectFixtures) {
    const project = await prisma.project.upsert({
      where: { slug: data.slug },
      create: data,
      update: data,
    });
    projects.set(project.slug, project.id);
  }

  const heatProjectId = projects.get("neighborhood-heat-commons");
  if (!heatProjectId) {
    throw new Error("Neighborhood Heat Commons was not seeded.");
  }

  const heatMilestoneData = {
    id: HEAT_HIERARCHY_IDS.milestone,
    projectId: heatProjectId,
    title: "Resident heat evidence ready for decision",
    description:
      "Collect lived experience and street-level observations, then turn them into a shared evidence set the neighborhood can review.",
    targetDate: new Date(now.getTime() + 28 * DAY),
    status: "IN_PROGRESS",
    priority: "HIGH",
    equityBudget: 1800,
    order: 1,
    assigneeId: owner.id,
  } satisfies Prisma.ProjectMilestoneUncheckedCreateInput;
  await prisma.projectMilestone.upsert({
    where: { id: heatMilestoneData.id },
    create: heatMilestoneData,
    update: heatMilestoneData,
  });

  const heatEpicData = {
    id: HEAT_HIERARCHY_IDS.epic,
    projectId: heatProjectId,
    milestoneId: HEAT_HIERARCHY_IDS.milestone,
    title: "Define the neighborhood heat response",
    description:
      "Build resident-owned evidence, identify the highest-risk places, and prepare cooling interventions for collective review.",
    status: "IN_PROGRESS",
    priority: "HIGH",
    equityBudget: 1200,
    progress: 35,
    startDate: new Date(now.getTime() - 7 * DAY),
    targetDate: new Date(now.getTime() + 24 * DAY),
    assigneeId: owner.id,
  } satisfies Prisma.EpicUncheckedCreateInput;
  await prisma.epic.upsert({
    where: { id: heatEpicData.id },
    create: heatEpicData,
    update: heatEpicData,
  });

  const heatSprintData = {
    id: HEAT_HIERARCHY_IDS.sprint,
    projectId: heatProjectId,
    milestoneId: HEAT_HIERARCHY_IDS.milestone,
    epicId: HEAT_HIERARCHY_IDS.epic,
    name: "Sprint 01 — Listen and map",
    goal: "Complete two resident heat walks and turn the observations into a reviewable evidence package.",
    startDate: new Date(now.getTime() - 2 * DAY),
    endDate: new Date(now.getTime() + 12 * DAY),
    equityBudget: 700,
    velocity: 8,
    status: "ACTIVE",
    assigneeId: owner.id,
  } satisfies Prisma.SprintUncheckedCreateInput;
  await prisma.sprint.upsert({
    where: { id: heatSprintData.id },
    create: heatSprintData,
    update: heatSprintData,
  });

  const heatFeatureData = {
    id: HEAT_HIERARCHY_IDS.feature,
    projectId: heatProjectId,
    milestoneId: HEAT_HIERARCHY_IDS.milestone,
    epicId: HEAT_HIERARCHY_IDS.epic,
    sprintId: HEAT_HIERARCHY_IDS.sprint,
    title: "Resident heat-risk map",
    description:
      "A shared map that connects observations about shade, water, surface heat, access, and resident priorities.",
    status: "IN_PROGRESS",
    priority: "HIGH",
    equityBudget: 500,
    order: 1,
    assigneeId: owner.id,
  } satisfies Prisma.FeatureUncheckedCreateInput;
  await prisma.feature.upsert({
    where: { id: heatFeatureData.id },
    create: heatFeatureData,
    update: heatFeatureData,
  });

  const heatPbiData = {
    id: HEAT_HIERARCHY_IDS.pbi,
    projectId: heatProjectId,
    milestoneId: HEAT_HIERARCHY_IDS.milestone,
    epicId: HEAT_HIERARCHY_IDS.epic,
    sprintId: HEAT_HIERARCHY_IDS.sprint,
    featureId: HEAT_HIERARCHY_IDS.feature,
    title: "Resident heat evidence package",
    description:
      "Capture, organize, and summarize the first field observations without extracting or obscuring resident ownership of the evidence.",
    type: "FEATURE",
    storyPoints: 8,
    status: "IN_PROGRESS",
    acceptanceCriteria:
      "Two walks are recorded; observations identify place and time; resident notes remain attributable with consent; gaps and uncertainties are explicit; three intervention directions are ready for review.",
    priority: "HIGH",
    equityReward: 240,
    assigneeId: owner.id,
  } satisfies Prisma.ProductBacklogItemUncheckedCreateInput;
  await prisma.productBacklogItem.upsert({
    where: { id: heatPbiData.id },
    create: heatPbiData,
    update: heatPbiData,
  });

  const heatMemberData = {
    id: HEAT_HIERARCHY_IDS.member,
    projectId: heatProjectId,
    userId: owner.id,
    role: "FOUNDER",
    shareBalance: 0,
    votingPower: 1,
    invitedById: owner.id,
  } satisfies Prisma.ProjectMemberUncheckedCreateInput;
  await prisma.projectMember.upsert({
    where: {
      projectId_userId: { projectId: heatProjectId, userId: owner.id },
    },
    create: heatMemberData,
    update: {
      role: heatMemberData.role,
      votingPower: heatMemberData.votingPower,
      invitedById: heatMemberData.invitedById,
    },
  });

  const heatCommentData = {
    id: HEAT_HIERARCHY_IDS.comment,
    projectId: heatProjectId,
    userId: owner.id,
    content:
      "Steward note: the first iteration should make uncertainty visible. Record where evidence is thin, ask residents what the map misses, and revise before proposing interventions.",
    targetType: "PROJECT",
    targetId: heatProjectId,
  } satisfies Prisma.ProjectCommentUncheckedCreateInput;
  await prisma.projectComment.upsert({
    where: { id: heatCommentData.id },
    create: heatCommentData,
    update: heatCommentData,
  });

  const taskFixtures = [
    {
      id: "local-qa-task-heat-walk",
      projectId: heatProjectId,
      pbiId: HEAT_HIERARCHY_IDS.pbi,
      featureId: HEAT_HIERARCHY_IDS.feature,
      sprintId: HEAT_HIERARCHY_IDS.sprint,
      epicId: HEAT_HIERARCHY_IDS.epic,
      milestoneId: HEAT_HIERARCHY_IDS.milestone,
      title: "Run two resident heat walks",
      description:
        "Record shade, water access, surface temperature, and resident observations.",
      status: "IN_PROGRESS",
      priority: "HIGH",
      taskType: "RESEARCH",
      estimatedHours: 8,
      assignedToId: owner.id,
      dueDate: new Date(now.getTime() + 7 * DAY),
    },
    {
      id: "local-qa-task-heat-brief",
      projectId: heatProjectId,
      pbiId: HEAT_HIERARCHY_IDS.pbi,
      featureId: HEAT_HIERARCHY_IDS.feature,
      sprintId: HEAT_HIERARCHY_IDS.sprint,
      epicId: HEAT_HIERARCHY_IDS.epic,
      milestoneId: HEAT_HIERARCHY_IDS.milestone,
      title: "Draft the intervention brief",
      description:
        "Turn field evidence into three reviewable intervention options.",
      status: "TODO",
      priority: "HIGH",
      taskType: "DOCUMENTATION",
      estimatedHours: 6,
      assignedToId: owner.id,
      dueDate: new Date(now.getTime() + 14 * DAY),
    },
    {
      id: "local-qa-task-repair-safety",
      projectId: projects.get("repair-library-pilot")!,
      title: "Review tool-lending safety practices",
      description:
        "Document check-out, training, maintenance, and incident procedures.",
      status: "REVIEW",
      priority: "MEDIUM",
      taskType: "REVIEW",
      estimatedHours: 5,
      dueDate: new Date(now.getTime() + 5 * DAY),
    },
    {
      id: "local-qa-task-wifi-retro",
      projectId: projects.get("community-wifi-kit")!,
      title: "Publish the pilot retrospective",
      description:
        "Record signal findings, resident feedback, open risks, and the next iteration.",
      status: "COMPLETED",
      priority: "MEDIUM",
      taskType: "DOCUMENTATION",
      estimatedHours: 4,
      actualHours: 5,
      completedAt: new Date(now.getTime() - 3 * DAY),
    },
  ] satisfies Prisma.ProjectTaskUncheckedCreateInput[];

  for (const task of taskFixtures) {
    await prisma.projectTask.upsert({
      where: { id: task.id },
      create: task,
      update: task,
    });
  }

  // Tandem-demo personas: see DEMO_PERSONAS above for the DEV_AUTH_BYPASS limitation.
  const personas = new Map<keyof typeof DEMO_PERSONAS, string>();
  for (const [key, persona] of Object.entries(DEMO_PERSONAS)) {
    const data = {
      id: persona.id,
      email: persona.email,
      name: persona.name,
      bio: persona.bio,
      location: "Local QA workspace",
      role: persona.role,
      userType: persona.userType,
      isVerified: true,
      verificationLevel: "VERIFIED",
    } satisfies Prisma.UserUncheckedCreateInput;
    const user = await prisma.user.upsert({
      where: { email: persona.email },
      create: data,
      update: data,
    });
    if (user.id !== persona.id) {
      throw new Error(
        `The ${key} demo persona email already belongs to ${user.id}; use a fresh dedicated demo database so it can use ${persona.id}.`,
      );
    }
    personas.set(key as keyof typeof DEMO_PERSONAS, user.id);
  }
  const creatorId = personas.get("creator")!;

  // Deliberately WITHOUT a ProjectTokenConfig, custodial account, or KYC state: the demo
  // drives funding provisioning live through the project's Funding tab
  // (provisionFundingMutation, see `src/app/projects/create/page.tsx:1062-1112`), the real
  // custodial-account APIs, and the real AZOA KYC flow -- never seeded.
  const fundableProjectData = {
    title: "Solar Micro-Grid Commons",
    slug: "solar-micro-grid-commons",
    description:
      "A resident-owned micro-grid pilot that needs sponsor funding and a paid contributor to finish wiring documentation before install.",
    problemStatement:
      "Two blocks lose power every summer peak, and nobody has funded the wiring survey the utility co-op needs before it will approve a shared micro-grid.",
    solution:
      "Fund the wiring survey and interconnection paperwork, then hire a contributor to turn the survey into an install-ready packet.",
    categories: "ENVIRONMENT,TECHNOLOGY",
    tags: "energy,resilience,micro-grid",
    projectType: "COMMUNITY",
    status: "PUBLISHED",
    duration: "ONE_THREE_MONTHS",
    expectedImpact:
      "A funded wiring survey and an install-ready interconnection packet.",
    targetAudience: "Residents, the utility co-op, and electrical contributors",
    createdById: creatorId,
    publishedAt: now,
    // FundingIntentService refuses checkout on a project whose owner has not switched
    // commerce on; the demo project exists to take sponsorship, so the owner's choice
    // is part of the fixture (mvp-tandem-demo F25).
    commerceEnabled: true,
  } satisfies Prisma.ProjectUncheckedCreateInput;
  const fundableProject = await prisma.project.upsert({
    where: { slug: fundableProjectData.slug },
    create: fundableProjectData,
    update: fundableProjectData,
  });

  // Paid role: OPEN and accepting applications/bids, shaped per `model Opportunity` /
  // `OpportunityApplication` / `OpportunityBid` in prisma/schema.prisma. No application or
  // bid row is pre-seeded -- the freelancer applies and bids live during the demo (spec step 4).
  const paidOpportunityData = {
    title: "Turn the wiring survey into an install-ready packet",
    slug: "solar-micro-grid-wiring-packet",
    description:
      "Take the raw wiring survey notes and produce the interconnection packet the utility co-op requires before install.",
    type: "FREELANCE",
    origin: "TEAM_POSITION",
    status: "OPEN",
    experienceLevel: "MID",
    requirements:
      "Comfortable reading electrical survey notes and utility interconnection requirements.",
    skills: "electrical,technical-writing,permitting",
    benefits: "Paid role; held units on acceptance per the task-commerce award flow.",
    location: "Remote",
    isRemote: true,
    compensation: 1500,
    compensationDetails: "1,500 ARDA on completion, held from the project's contributor allowance.",
    posterId: creatorId,
    projectId: fundableProject.id,
  } satisfies Prisma.OpportunityUncheckedCreateInput;
  await prisma.opportunity.upsert({
    where: { slug: paidOpportunityData.slug },
    create: paidOpportunityData,
    update: paidOpportunityData,
  });

  const guildData = {
    name: "Neighborhood Infrastructure Guild",
    slug: "neighborhood-infrastructure-guild",
    description:
      "Local stewards sharing practical methods for resident-owned infrastructure and accountable delivery.",
    email: "guild@local.ardanova.test",
    specialties: "facilitation,field research,repair,community networks",
    ownerId: owner.id,
    membersCount: 12,
    projectsCount: 3,
    isVerified: true,
  } satisfies Prisma.GuildUncheckedCreateInput;
  const guild = await prisma.guild.upsert({
    where: { slug: guildData.slug },
    create: guildData,
    update: guildData,
  });

  const eventFixtures = [
    {
      title: "Heat evidence review",
      slug: "heat-evidence-review",
      description:
        "Review field notes, identify gaps, and agree on what belongs in the solution brief.",
      type: "CRITIQUE",
      visibility: "PUBLIC",
      status: "SCHEDULED",
      location: "Neighborhood library meeting room",
      timezone: "America/Denver",
      startDate: new Date(now.getTime() + 4 * DAY),
      endDate: new Date(now.getTime() + 4 * DAY + 90 * 60 * 1000),
      organizerId: owner.id,
      projectId: projects.get("neighborhood-heat-commons")!,
      guildId: guild.id,
      maxAttendees: 24,
    },
    {
      title: "Repair clinic rehearsal",
      slug: "repair-clinic-rehearsal",
      description:
        "Walk through intake, tool safety, repair support, and the participant handoff.",
      type: "WORKSHOP",
      visibility: "PUBLIC",
      status: "SCHEDULED",
      location: "Community workshop",
      timezone: "America/Denver",
      startDate: new Date(now.getTime() + 10 * DAY),
      endDate: new Date(now.getTime() + 10 * DAY + 2 * 60 * 60 * 1000),
      organizerId: owner.id,
      projectId: projects.get("repair-library-pilot")!,
      guildId: guild.id,
      maxAttendees: 18,
    },
  ] satisfies Prisma.EventUncheckedCreateInput[];

  for (const event of eventFixtures) {
    await prisma.event.upsert({
      where: { slug: event.slug },
      create: event,
      update: event,
    });
  }

  console.info(
    "Seeded local demo: preview owner, 3 projects, a 5-level Heat Commons hierarchy, 4 tasks, 1 project membership, 1 project comment, 1 guild, 2 events, 3 tandem-demo personas (creator/sponsor/freelancer), 1 fundable project with no funding config, and 1 OPEN paid opportunity.",
  );
}

async function main() {
  assertLocalDemoSeedAllowed(process.env);
  const prisma = new PrismaClient();
  try {
    await seedLocalDemo(prisma);
  } finally {
    await prisma.$disconnect();
  }
}

void main().catch((error: unknown) => {
  console.error(
    error instanceof Error ? error.message : "Local demo seeding failed.",
  );
  process.exitCode = 1;
});
