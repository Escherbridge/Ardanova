import { redirect } from "next/navigation";
import { auth } from "~/server/auth";
import { GuildForm } from "~/components/guild-form";

export default async function CreateGuildPage() {
  const session = await auth();

  if (!session) {
    redirect("/api/auth/signin");
  }

  return <GuildForm mode="create" />;
}
