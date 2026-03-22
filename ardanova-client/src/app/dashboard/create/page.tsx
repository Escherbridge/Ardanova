import { redirect } from "next/navigation";

/**
 * Single create path: multi-step wizard at /projects/create.
 * Dashboard and nav links still use /dashboard/create for discoverability.
 */
export default function CreateProjectPage() {
  redirect("/projects/create");
}
