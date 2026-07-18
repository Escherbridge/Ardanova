import type { Opportunity } from "~/lib/api/ardanova/endpoints/opportunities";

/** View contract shared by the opportunity server pages and client views. */
export interface OpportunityPageData {
  id: string;
  posterId: string;
  title: string;
  slug: string;
  description: string;
  type: string;
  status: string;
  experienceLevel: string;
  requirements?: string;
  skills?: string;
  compensation?: number;
  compensationDetails?: string;
  location?: string;
  isRemote: boolean;
  deadline?: string;
  maxApplications?: number;
  applicationsCount: number;
  projectId?: string;
  guildId?: string;
  taskId?: string;
  projectRole?: string;
  createdAt: string;
  updatedAt: string;
  poster?: { id: string; name?: string; image?: string };
}

export function toOpportunityPageData(
  opportunity: Opportunity,
): OpportunityPageData {
  return {
    id: opportunity.id,
    posterId: opportunity.posterId,
    title: opportunity.title,
    slug: opportunity.slug ?? opportunity.id,
    description: opportunity.description,
    type: opportunity.type ?? "UNSPECIFIED",
    status: opportunity.status ?? "UNSPECIFIED",
    experienceLevel: opportunity.experienceLevel ?? "UNSPECIFIED",
    requirements: opportunity.requirements ?? undefined,
    skills: opportunity.skills ?? undefined,
    compensation: opportunity.compensation ?? undefined,
    compensationDetails: opportunity.compensationDetails ?? undefined,
    location: opportunity.location ?? undefined,
    isRemote: opportunity.isRemote ?? false,
    deadline: opportunity.deadline ?? undefined,
    maxApplications: opportunity.maxApplications ?? undefined,
    applicationsCount: opportunity.applicationsCount ?? 0,
    projectId: opportunity.projectId ?? undefined,
    guildId: opportunity.guildId ?? undefined,
    taskId: opportunity.taskId ?? undefined,
    projectRole: opportunity.projectRole ?? undefined,
    createdAt: opportunity.createdAt,
    updatedAt: opportunity.updatedAt,
    poster: opportunity.poster
      ? {
          id: opportunity.poster.id,
          name: opportunity.poster.name ?? undefined,
          image: opportunity.poster.image ?? undefined,
        }
      : undefined,
  };
}
