import { redirect } from "next/navigation";
import { auth } from "~/server/auth";
import { OpportunityForm } from "~/components/opportunity-form";

interface CreateOpportunityPageProps {
  searchParams: Promise<{
    projectId?: string;
    projectSlug?: string;
    guildId?: string;
    guildSlug?: string;
    projectRole?: string;
  }>;
}

export default async function CreateOpportunityPage({ searchParams }: CreateOpportunityPageProps) {
  const session = await auth();

  if (!session) {
    redirect("/api/auth/signin");
  }

  const params = await searchParams;
  const entityType = params.projectId ? "project" : params.guildId ? "guild" : undefined;
  const entityId = params.projectId ?? params.guildId;
  const entitySlug = params.projectSlug ?? params.guildSlug;

  const defaultOpportunity = params.projectRole
    ? { projectRole: params.projectRole }
    : undefined;

  return (
    <OpportunityForm
      mode="create"
      entityType={entityType}
      entityId={entityId}
      entitySlug={entitySlug}
      opportunity={defaultOpportunity as any}
    />
  );
}
