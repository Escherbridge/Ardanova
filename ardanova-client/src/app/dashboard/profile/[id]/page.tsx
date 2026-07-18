"use client";

import { useParams } from "next/navigation";
import Link from "next/link";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "~/components/ui/tabs";
import { Calendar, FolderKanban, Users } from "lucide-react";
import { api } from "~/trpc/react";

export default function UserProfilePage() {
  const params = useParams();
  const userId = params.id as string;

  const {
    data: profileUser,
    isLoading: userLoading,
    error: userError,
  } = api.user.getById.useQuery({ id: userId }, { enabled: !!userId });

  const {
    data: userProjects,
    isLoading: projectsLoading,
    error: projectsError,
  } = api.project.getByUserId.useQuery(
    { userId, limit: 50 },
    { enabled: !!userId },
  );

  const {
    data: ownedGuilds,
    isLoading: guildsLoading,
    error: guildsError,
  } = api.guild.getGuildsForOwner.useQuery(
    { ownerId: userId },
    { enabled: !!userId },
  );

  const joinedDate = profileUser?.createdAt
    ? new Date(profileUser.createdAt).toLocaleDateString("en-US", {
        month: "short",
        year: "numeric",
      })
    : "—";

  if (userLoading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center p-6">
        <p className="text-muted-foreground">Loading profile…</p>
      </div>
    );
  }

  if (userError || !profileUser) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4 p-6">
        <p className="text-destructive">
          {userError?.message ?? "User not found."}
        </p>
        <Button asChild variant="outline">
          <Link href="/people">Back to People</Link>
        </Button>
      </div>
    );
  }

  const projectCount = userProjects?.items?.length ?? 0;
  const guildCount = Array.isArray(ownedGuilds) ? ownedGuilds.length : 0;

  return (
    <div className="bg-background">
      <div className="mx-auto max-w-4xl px-4 py-6">
        <Card className="mb-6">
          <CardContent className="pt-6">
            <div className="flex flex-col items-start gap-6 sm:flex-row">
              <Avatar className="border-primary size-24 border-4">
                <AvatarImage
                  src={profileUser.image ?? undefined}
                  alt={profileUser.name ?? "User"}
                />
                <AvatarFallback className="text-2xl">
                  {profileUser.name?.charAt(0) ?? "U"}
                </AvatarFallback>
              </Avatar>

              <div className="flex-1">
                <div className="mb-2 flex flex-wrap items-center gap-3">
                  <h1 className="text-foreground text-2xl font-bold">
                    {profileUser.name ?? "Member"}
                  </h1>
                  <Badge variant="neon">
                    {profileUser.userType ?? "Member"}
                  </Badge>
                </div>
                <p className="text-muted-foreground mb-4">
                  {profileUser.bio ?? "No bio yet."}
                </p>
                <div className="flex flex-wrap gap-x-6 gap-y-3 text-sm">
                  <div className="flex items-center gap-2">
                    <FolderKanban className="text-primary size-4" />
                    <span>
                      <strong>
                        {projectsLoading
                          ? "…"
                          : projectsError
                            ? "Unavailable"
                            : projectCount}
                      </strong>{" "}
                      Projects
                    </span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Users className="text-neon-pink size-4" />
                    <span>
                      <strong>
                        {guildsLoading
                          ? "…"
                          : guildsError
                            ? "Unavailable"
                            : guildCount}
                      </strong>{" "}
                      Guilds
                    </span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Calendar className="text-neon-green size-4" />
                    <span>Joined {joinedDate}</span>
                  </div>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        <Tabs defaultValue="projects" className="w-full">
          <TabsList className="w-full justify-start overflow-x-auto">
            <TabsTrigger value="projects">Projects</TabsTrigger>
            <TabsTrigger value="guilds">Guilds</TabsTrigger>
            <TabsTrigger value="contributions">Contributions</TabsTrigger>
          </TabsList>

          <TabsContent value="projects" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle>Projects</CardTitle>
              </CardHeader>
              <CardContent>
                {projectsLoading ? (
                  <p className="text-muted-foreground">Loading…</p>
                ) : projectsError ? (
                  <p role="alert" className="text-destructive">
                    Projects could not be loaded.
                  </p>
                ) : (userProjects?.items?.length ?? 0) === 0 ? (
                  <p className="text-muted-foreground">No projects yet.</p>
                ) : (
                  <ul className="space-y-2">
                    {userProjects?.items?.map((p) => (
                      <li key={p.id}>
                        <Link
                          href={`/projects/${p.slug}`}
                          className="text-primary inline-flex min-h-11 items-center font-medium hover:underline"
                        >
                          {p.title}
                        </Link>
                      </li>
                    ))}
                  </ul>
                )}
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="guilds" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle>Guilds</CardTitle>
              </CardHeader>
              <CardContent>
                {guildsLoading ? (
                  <p className="text-muted-foreground">Loading…</p>
                ) : guildsError ? (
                  <p role="alert" className="text-destructive">
                    Guilds could not be loaded.
                  </p>
                ) : !ownedGuilds?.length ? (
                  <p className="text-muted-foreground">No guilds listed.</p>
                ) : (
                  <ul className="space-y-2">
                    {ownedGuilds.map((g) => (
                      <li key={g.id}>
                        <Link
                          href={`/guilds/${g.slug}`}
                          className="text-primary inline-flex min-h-11 items-center font-medium hover:underline"
                        >
                          {g.name}
                        </Link>
                      </li>
                    ))}
                  </ul>
                )}
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="contributions" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle>Contributions</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-muted-foreground">
                  Total XP: <strong>{profileUser.totalXP ?? 0}</strong> · Level{" "}
                  {profileUser.level ?? 1}
                </p>
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  );
}
