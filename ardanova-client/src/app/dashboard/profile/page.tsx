"use client";

import { useSession } from "next-auth/react";
import { api } from "~/trpc/react";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "~/components/ui/tabs";
import { CredentialList } from "~/components/credentials/credential-list";
import { CredentialBadge } from "~/components/credentials/credential-badge";
import { Settings, Share2, Calendar, FolderKanban, Users, Shield } from "lucide-react";
import type { MembershipCredential } from "~/lib/api/ardanova/endpoints/membership-credentials";

export default function ProfilePage() {
  const { data: session } = useSession();
  const user = session?.user;

  const { data: credentials, isLoading: credentialsLoading } =
    api.membershipCredential.getByUserId.useQuery(
      { userId: user?.id },
      { enabled: !!user?.id },
    );

  const activeCredentials = credentials?.filter((c) => c.status === "ACTIVE") ?? [];
  const highestTier = getHighestTier(activeCredentials.map((c) => c.tier).filter(Boolean) as string[]);

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-4xl mx-auto p-6">
        {/* Profile Header */}
        <Card className="mb-6">
          <CardContent className="pt-6">
            <div className="flex items-start gap-6">
              <Avatar className="size-24 border-4 border-primary">
                <AvatarImage src={user?.image || undefined} alt={user?.name || "User"} />
                <AvatarFallback className="text-2xl">
                  {user?.name?.charAt(0) || "U"}
                </AvatarFallback>
              </Avatar>

              <div className="flex-1">
                <div className="flex items-center gap-3 mb-2">
                  <h1 className="text-2xl font-bold text-foreground">
                    {user?.name || "Anonymous User"}
                  </h1>
                  <Badge variant="neon">Member</Badge>
                </div>
                <p className="text-muted-foreground mb-4">
                  {user?.email}
                </p>
                <div className="flex gap-6 text-sm">
                  <div className="flex items-center gap-2">
                    <FolderKanban className="size-4 text-primary" />
                    <span><strong>3</strong> Projects</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Users className="size-4 text-neon-pink" />
                    <span><strong>2</strong> Guilds</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Calendar className="size-4 text-neon-green" />
                    <span>Joined Jan 2024</span>
                  </div>
                  {activeCredentials.length > 0 && (
                    <div className="flex items-center gap-2">
                      <Shield className="size-4 text-neon-cyan" />
                      <span>
                        <strong>{activeCredentials.length}</strong>{" "}
                        {activeCredentials.length === 1 ? "Credential" : "Credentials"}
                      </span>
                      {highestTier && (
                        <CredentialBadge tier={highestTier} size="sm" />
                      )}
                    </div>
                  )}
                </div>
              </div>

              <div className="flex gap-2">
                <Button variant="outline" size="sm">
                  <Share2 className="size-4 mr-2" />
                  Share
                </Button>
                <Button variant="outline" size="sm">
                  <Settings className="size-4 mr-2" />
                  Edit Profile
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Profile Tabs */}
        <Tabs defaultValue="activity" className="w-full">
          <TabsList className="w-full justify-start">
            <TabsTrigger value="activity">Activity</TabsTrigger>
            <TabsTrigger value="projects">Projects</TabsTrigger>
            <TabsTrigger value="guilds">Guilds</TabsTrigger>
            <TabsTrigger value="credentials">Credentials</TabsTrigger>
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
                <CardTitle>Your Projects</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-muted-foreground">You haven't joined any projects yet.</p>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="guilds" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle>Your Guilds</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-muted-foreground">You haven't joined any guilds yet.</p>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="credentials" className="mt-6">
            <CredentialList
              credentials={credentials as MembershipCredential[] | undefined}
              isLoading={credentialsLoading}
              emptyMessage="You don't have any credentials yet. Join a project or guild to earn one!"
            />
          </TabsContent>

          <TabsContent value="contributions" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle>Contributions</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-muted-foreground">No contributions to show.</p>
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  );
}

const TIER_RANK: Record<string, number> = {
  DIAMOND: 5,
  PLATINUM: 4,
  GOLD: 3,
  SILVER: 2,
  BRONZE: 1,
};

function getHighestTier(tiers: string[]): string | null {
  if (tiers.length === 0) return null;
  return tiers.reduce((highest, tier) => {
    const rank = TIER_RANK[tier.toUpperCase()] ?? 0;
    const highestRank = TIER_RANK[highest.toUpperCase()] ?? 0;
    return rank > highestRank ? tier : highest;
  }, tiers[0]!);
}
