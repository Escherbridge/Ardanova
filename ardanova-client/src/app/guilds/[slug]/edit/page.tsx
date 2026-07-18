import { redirect, notFound } from "next/navigation";
import { auth } from "~/server/auth";
import { apiClient } from "~/lib/api";
import { buildSignInHref } from "~/lib/auth-navigation";
import { GuildForm } from "~/components/guild-form";

interface EditGuildPageProps {
  params: Promise<{
    slug: string;
  }>;
}

export default async function EditGuildPage({ params }: EditGuildPageProps) {
  const { slug } = await params;
  const session = await auth();

  if (!session) {
    redirect(buildSignInHref(`/guilds/${slug}/edit`));
  }

  const response = await apiClient.guilds.getBySlug(slug);
  if (response.error || !response.data) {
    notFound();
  }
  const guild = response.data;

  // Check ownership
  if (guild.ownerId !== session.user.id) {
    redirect(`/guilds/${slug}`);
  }

  return <GuildForm mode="edit" guild={guild} />;
}
