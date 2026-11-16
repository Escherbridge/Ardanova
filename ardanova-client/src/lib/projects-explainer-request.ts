import { headers } from "next/headers";

import { PROJECTS_EXPLAINER_HEADER } from "~/lib/auth-navigation";
import { authForPage } from "~/server/auth";

/** Signed-out GET /projects, as marked by middleware. Other methods and routes stay gated. */
export async function isSignedOutProjectsExplainer(): Promise<boolean> {
  if (await authForPage()) return false;
  const requestHeaders = await headers();
  return requestHeaders.get(PROJECTS_EXPLAINER_HEADER) === "1";
}
