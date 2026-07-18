import { redirect } from "next/navigation";
import { authForPage } from "~/server/auth";
import { buildSignInHref } from "~/lib/auth-navigation";
import { GuildForm } from "~/components/guild-form";

export default async function CreateGuildPage() {
  const session = await authForPage();

  if (!session) {
    redirect(buildSignInHref("/guilds/create"));
  }

  return <GuildForm mode="create" />;
}
