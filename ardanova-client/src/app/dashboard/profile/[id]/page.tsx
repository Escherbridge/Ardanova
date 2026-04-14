"use client";

import { useParams } from "next/navigation";
import Link from "next/link";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "~/components/ui/tabs";
import { Share2, Calendar, FolderKanban, Users, UserPlus } from "lucide-react";
import { api } from "~/trpc/react";

export default function UserProfilePage() {
  const params = useParams();
  const userId = params.id as string;

  const { data: profileUser, isLoading: userLoading, error: userError } = api.user.getById.useQuery(
    { id: userId },
    { enabled: !!userId },
  );

  const { data: userProjects, isLoading: projectsLoading } = api.project.getByUserId.useQuery(
    { userId, limit: 50 },
    { enabled: !!userId },
  );

  const { data: ownedGuilds, isLoading: guildsLoading } = api.guild.getGuildsForOwner.useQuery(
    { ownerId: userId },
    { enabled: !!userId },
  );

  const joinedDate = profileUser?.createdAt
    ? new Date(profileUser.createdAt).toLocaleDateString("en-US", { month: "short", year: "numeric" })
    : "—";

  if (userLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center p-6">
        <p className="text-muted-foreground">Loading profile…</p>
      </div>
    );
  }

  if (userError || !profileUser) {
    return (
      <div className="min-h-screen bg-background flex flex-col items-center justify-center gap-4 p-6">
        <p className="text-destructive">{userError?.message ?? "User not found."}</p>
        <Button asChild variant="outline">
          <Link href="/people">Back to People</Link>
        </Button>
      </div>
    );
  }

  const projectCount = userProjects?.items?.length ?? 0;
  const guildCount = Array.isArray(ownedGuilds) ? ownedGuilds.length : 0;

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-4xl mx-auto p-6">
        <Card className="mb-6">
          <CardContent className="pt-6">
            <div className="flex items-start gap-6">
              <Avatar className="size-24 border-4 border-primary">
                <AvatarImage src={profileUser.image ?? undefined} alt={profileUser.name ?? "User"} />
                <AvatarFallback className="text-2xl">{profileUser.name?.charAt(0) ?? "U"}</AvatarFallback>
              </Avatar>

              <div className="flex-1">
                <div className="flex items-center gap-3 mb-2">
                  <h1 className="text-2xl font-bold text-foreground">{profileUser.name ?? "Member"}</h1>
                  <Badge variant="neon">{profileUser.userType ?? "Member"}</Badge>
                </div>
                <p className="text-muted-foreground mb-4">{profileUser.bio ?? "No bio yet."}</p>
                <div className="flex gap-6 text-sm">
                  <div className="flex items-center gap-2">
                    <FolderKanban className="size-4 text-primary" />
                    <span>
                      <strong>{projectCount}</strong> Projects
                    </span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Users className="size-4 text-neon-pink" />
                    <span>
                      <strong>{guildCount}</strong> Guilds
                    </span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Calendar className="size-4 text-neon-green" />
                    <span>Joined {joinedDate}</span>
                  </div>
                </div>
              </div>

              <div className="flex gap-2">
                <Button variant="outline" size="sm" type="button">
                  <Share2 className="size-4 mr-2" />
                  Share
                </Button>
                <Button variant="default" size="sm" type="button">
                  <UserPlus className="size-4 mr-2" />
                  Follow
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>

        <Tabs defaultValue="activity" className="w-full">
          <TabsList className="w-full justify-start">
            <TabsTrigger value="activity">Activity</TabsTrigger>
            <TabsTrigger value="projects">Projects</TabsTrigger>
            <TabsTrigger value="guilds">Guilds</TabsTrigger>
            <TabsTrigger value="contributions">Contributions</TabsTrigger>
          </TabsList>

          <TabsContent value="activity" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle>Recent Activity</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-muted-foreground">No recent activity to show.</p>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="projects" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle>Projects</CardTitle>
              </CardHeader>
              <CardContent>
                {projectsLoading ? (
                  <p className="text-muted-foreground">Loading…</p>
                ) : (userProjects?.items?.length ?? 0) === 0 ? (
                  <p className="text-muted-foreground">No projects yet.</p>
                ) : (
                  <ul className="space-y-2">
                    {userProjects!.items.map((p) => (
                      <li key={p.id}>
                        <Link href={`/projects/${p.slug}`} className="text-primary hover:underline font-medium">
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
                ) : !ownedGuilds?.length ? (
                  <p className="text-muted-foreground">No guilds listed.</p>
                ) : (
                  <ul className="space-y-2">
                    {ownedGuilds.map((g) => (
                      <li key={g.id}>
                        <Link href={`/guilds/${g.slug}`} className="text-primary hover:underline font-medium">
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
                  Total XP: <strong>{profileUser.totalXP ?? 0}</strong> · Level {profileUser.level ?? 1}
                </p>
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  );
}
