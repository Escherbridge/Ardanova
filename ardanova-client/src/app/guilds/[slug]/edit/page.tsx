import { redirect, notFound } from "next/navigation";
import { auth } from "~/server/auth";
import { api } from "~/trpc/server";
import { GuildForm } from "~/components/guild-form";

interface EditGuildPageProps {
  params: Promise<{
    slug: string;
  }>;
}

export default async function EditGuildPage({ params }: EditGuildPageProps) {
  const session = await auth();

  if (!session) {
    redirect("/api/auth/signin");
  }

  const { slug } = await params;

  let guild;
  try {
    guild = await api.guild.getBySlug({ slug });
  } catch (error) {
    notFound();
  }

  if (!guild) {
    notFound();
  }

  // Check ownership
  if (guild.ownerId !== session.user.id) {
    redirect(`/guilds/${slug}`);
  }

  return <GuildForm mode="edit" guild={guild} />;
}
