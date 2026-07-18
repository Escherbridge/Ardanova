import { describe, expect, it } from "vitest";

import {
  createProjectDtoSchema,
  isProjectDuration,
  isProjectRole,
  isProjectType,
  projectMilestoneDtoSchema,
  projectDtoSchema,
} from "./project-contract";

const projectDto = {
  id: "project-1",
  title: "Repair the neighborhood tool library",
  slug: "repair-the-tool-library",
  description: "A practical repair and access project.",
  problemStatement: "Useful tools are expensive and underused.",
  solution: "Build a member-run lending and repair library.",
  categories: ["COMMUNITY"],
  status: "PUBLISHED",
  fundingGoal: null,
  currentFunding: 0,
  supportersCount: 3,
  votesCount: 2,
  viewsCount: 12,
  featured: false,
  tags: null,
  images: null,
  videos: null,
  documents: null,
  targetAudience: null,
  expectedImpact: null,
  timeline: null,
  projectType: "COMMUNITY",
  duration: "ONGOING",
  createdAt: "2026-07-17T12:00:00Z",
  updatedAt: "2026-07-17T12:00:00Z",
  publishedAt: "2026-07-17T12:00:00Z",
  fundedAt: null,
  completedAt: null,
  commerceEnabled: false,
  storefrontDescription: null,
  createdById: "user-1",
  createdBy: { id: "user-1", name: null, image: null },
  assignedGuildId: null,
} as const;

describe("project API contract", () => {
  it("accepts the complete .NET ProjectDto shape", () => {
    expect(projectDtoSchema.parse(projectDto)).toEqual(projectDto);
  });

  it("rejects the removed assignedAgency field in place of assignedGuildId", () => {
    const legacy = { ...projectDto, assignedAgencyId: "agency-1" };
    Reflect.deleteProperty(legacy, "assignedGuildId");
    expect(projectDtoSchema.safeParse(legacy).success).toBe(false);
  });

  it("keeps actor identity server-derived while commerce stays explicit", () => {
    expect(
      createProjectDtoSchema.safeParse({
        title: "A project",
        description: "Project description",
        problemStatement: "A defined problem",
        solution: "A defined solution",
        categories: ["COMMUNITY"],
        projectType: "COMMUNITY",
        duration: null,
        commerceEnabled: true,
        storefrontDescription: "Member-made goods",
      }).success,
    ).toBe(true);
  });

  it("narrows project enums without casting", () => {
    expect(isProjectType("OPEN_SOURCE")).toBe(true);
    expect(isProjectDuration("ONE_THREE_MONTHS")).toBe(true);
    expect(isProjectRole("CORE_CONTRIBUTOR")).toBe(true);
    expect(isProjectType("SIDE_HUSTLE")).toBe(false);
  });

  it("accepts the exact .NET ProjectMilestoneDto status shape", () => {
    expect(
      projectMilestoneDtoSchema.parse({
        id: "milestone-1",
        projectId: "project-1",
        title: "Open the first repair night",
        description: null,
        targetDate: "2026-08-01T18:00:00Z",
        completedAt: null,
        status: "IN_PROGRESS",
        priority: "HIGH",
        order: 2,
        createdAt: "2026-07-17T12:00:00Z",
        updatedAt: "2026-07-18T12:00:00Z",
      }).status,
    ).toBe("IN_PROGRESS");
  });

  it("accepts nullable targets and the offset-less UTC form emitted by .NET", () => {
    expect(
      projectMilestoneDtoSchema.safeParse({
        id: "milestone-1",
        projectId: "project-1",
        title: "Collect the open questions",
        description: null,
        targetDate: null,
        completedAt: null,
        status: "PLANNED",
        priority: "MEDIUM",
        order: 0,
        createdAt: "2026-07-18T08:08:22.971",
        updatedAt: "2026-07-18T08:08:22.971",
      }).success,
    ).toBe(true);
  });

  it("rejects the removed milestone isCompleted projection", () => {
    expect(
      projectMilestoneDtoSchema.safeParse({
        id: "milestone-1",
        projectId: "project-1",
        title: "Open the first repair night",
        description: null,
        targetDate: "2026-08-01T18:00:00Z",
        completedAt: null,
        isCompleted: false,
        priority: "HIGH",
        order: 2,
        createdAt: "2026-07-17T12:00:00Z",
        updatedAt: "2026-07-18T12:00:00Z",
      }).success,
    ).toBe(false);
  });
});
