import { redirect } from "next/navigation";
import { authForPage } from "~/server/auth";
import { buildSignInHref } from "~/lib/auth-navigation";
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

export default async function CreateOpportunityPage({
  searchParams,
}: CreateOpportunityPageProps) {
  const params = await searchParams;
  const callbackSearch = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value) callbackSearch.set(key, value);
  }
  const callbackUrl = `/opportunities/create${callbackSearch.size > 0 ? `?${callbackSearch.toString()}` : ""}`;
  const session = await authForPage();

  if (!session) {
    redirect(buildSignInHref(callbackUrl));
  }

  const entityType = params.projectId
    ? "project"
    : params.guildId
      ? "guild"
      : undefined;
  const entityId = params.projectId ?? params.guildId;
  const entitySlug = params.projectSlug ?? params.guildSlug;

  return (
    <OpportunityForm
      mode="create"
      entityType={entityType}
      entityId={entityId}
      entitySlug={entitySlug}
      defaultProjectRole={params.projectRole}
    />
  );
}
