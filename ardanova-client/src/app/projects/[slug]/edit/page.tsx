"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useSession } from "next-auth/react";
import { AlertCircle, ArrowLeft, Loader2, Pencil } from "lucide-react";

import { Button } from "~/components/ui/button";
import { ProjectEditForm } from "~/components/projects/project-edit-form";
import { api } from "~/trpc/react";

/**
 * The home page's "Iterate - revise or stop" step: lets a project's manager
 * revise the same Basic Info fields the create wizard's step 0 collects,
 * saved via api.project.update rather than api.project.create. The wizard
 * itself lives at ../../create/page.tsx and is create-only (it also drives
 * resources/roles/milestones and a sessionStorage draft, none of which apply
 * to editing an already-published project).
 */
export default function EditProjectPage() {
  const params = useParams();
  const slug = params.slug as string;
  const { data: session, status: sessionStatus } = useSession();

  const projectQuery = api.project.getById.useQuery({ id: slug });
  const project = projectQuery.data;

  const currentUserId = session?.user?.id;
  const isOwner = Boolean(
    currentUserId && project?.createdById === currentUserId,
  );

  if (projectQuery.isLoading || sessionStatus === "loading") {
    return (
      <div className="flex min-h-[60vh] items-center justify-center p-6">
        <div className="flex items-center gap-3" role="status">
          <Loader2 className="text-primary size-5 animate-spin" />
          <span className="font-mono text-sm">Loading project…</span>
        </div>
      </div>
    );
  }

  if (projectQuery.error || !project) {
    return (
      <div className="mx-auto flex min-h-[60vh] max-w-xl flex-col items-start justify-center gap-5 p-6">
        <AlertCircle className="text-destructive size-10" aria-hidden="true" />
        <div>
          <h1 className="font-mono text-2xl font-bold">Project not found</h1>
          <p className="text-muted-foreground mt-2 text-sm">
            This project could not be loaded, or it no longer exists.
          </p>
        </div>
        <Button asChild variant="outline">
          <Link href="/projects">
            <ArrowLeft className="size-4" />
            Back to projects
          </Link>
        </Button>
      </div>
    );
  }

  if (!isOwner) {
    return (
      <div className="mx-auto flex min-h-[60vh] max-w-xl flex-col items-start justify-center gap-5 p-6">
        <AlertCircle className="text-destructive size-10" aria-hidden="true" />
        <div>
          <h1 className="font-mono text-2xl font-bold">
            You can&apos;t edit this project
          </h1>
          <p className="text-muted-foreground mt-2 text-sm">
            Only the project&apos;s creator can revise its Basic Info here.
          </p>
        </div>
        <Button asChild variant="outline">
          <Link href={`/projects/${project.slug}`}>
            <ArrowLeft className="size-4" />
            Back to project
          </Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="bg-background min-h-screen">
      <div className="mx-auto max-w-4xl px-4 py-8">
        <div className="mb-8">
          <Button variant="ghost" asChild className="mb-4 -ml-2">
            <Link href={`/projects/${project.slug}`}>
              <ArrowLeft className="mr-2 size-4" />
              Back to project
            </Link>
          </Button>
          <h1 className="flex items-center gap-2 text-3xl font-bold">
            <Pencil className="text-primary size-6" aria-hidden="true" />
            Edit Project
          </h1>
          <p className="text-muted-foreground mt-2">
            Revise the problem, solution, and details for{" "}
            <span className="text-foreground font-medium">
              {project.title}
            </span>
            .
          </p>
        </div>

        <ProjectEditForm project={project} />
      </div>
    </div>
  );
}
