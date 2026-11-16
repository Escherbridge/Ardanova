import type { Metadata } from "next";

import { isSignedOutProjectsExplainer } from "~/lib/projects-explainer-request";

import { PROJECTS_DESCRIPTION, ProjectsExplainer } from "./projects-explainer";
import ProjectsBrowser from "./projects-browser";

export async function generateMetadata(): Promise<Metadata> {
  if (!(await isSignedOutProjectsExplainer())) return {};
  return {
    title: "Projects",
    description: PROJECTS_DESCRIPTION,
  };
}

export default async function ProjectsPage() {
  if (await isSignedOutProjectsExplainer()) {
    return <ProjectsExplainer />;
  }
  return <ProjectsBrowser />;
}
