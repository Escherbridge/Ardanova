"use client";

import { useSession } from "next-auth/react";
import Link from "next/link";
import { api } from "~/trpc/react";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "~/components/ui/tabs";
import { CredentialList } from "~/components/credentials/credential-list";
import { CredentialBadge } from "~/components/credentials/credential-badge";
import { CustodialAccountCard } from "~/components/azoa/custodial-account-card";
import {
  Settings,
  Calendar,
  FolderKanban,
  Users,
  Shield,
  Mail,
  Check,
  X,
} from "lucide-react";
import {
  getRoleBadgeVariant,
  formatRoleName,
} from "~/components/projects/team-tab";
import { toast } from "sonner";

interface ProfileGuildView {
  id: string;
  slug: string;
  name: string;
}

interface InvitationView {
  id: string;
  projectId: string;
  status: string;
  role: string;
  message?: string;
  project?: { title: string; image?: string };
  invitedByName?: string;
}

function recordFrom(value: unknown): Record<string, unknown> | null {
  return typeof value === "object" && value !== null
    ? (value as Record<string, unknown>)
    : null;
}

function normalizeGuildViews(value: unknown): ProfileGuildView[] {
  if (!Array.isArray(value)) return [];
  return value.flatMap((item) => {
    const record = recordFrom(item);
    if (
      !record ||
      typeof record.id !== "string" ||
      typeof record.slug !== "string" ||
      typeof record.name !== "string"
    ) {
      return [];
    }
    return [{ id: record.id, slug: record.slug, name: record.name }];
  });
}

function normalizeInvitationViews(value: unknown): InvitationView[] {
  if (!Array.isArray(value)) return [];
  return value.flatMap((item) => {
    const record = recordFrom(item);
    if (
      !record ||
      typeof record.id !== "string" ||
      typeof record.projectId !== "string" ||
      typeof record.status !== "string" ||
      typeof record.role !== "string"
    ) {
      return [];
    }

    const projectRecord = recordFrom(record.project);
    const invitedByRecord = recordFrom(record.invitedBy);
    const rawImages = projectRecord?.images;
    const image =
      typeof rawImages === "string"
        ? rawImages.split(",")[0]
        : Array.isArray(rawImages) && typeof rawImages[0] === "string"
          ? rawImages[0]
          : undefined;
    const projectTitle =
      typeof projectRecord?.title === "string"
        ? projectRecord.title
        : undefined;

    return [
      {
        id: record.id,
        projectId: record.projectId,
        status: record.status,
        role: record.role,
        message:
          typeof record.message === "string" ? record.message : undefined,
        project: projectTitle ? { title: projectTitle, image } : undefined,
        invitedByName:
          typeof invitedByRecord?.name === "string"
            ? invitedByRecord.name
            : undefined,
      },
    ];
  });
}

export default function ProfilePage() {
  const { data: session, status: sessionStatus } = useSession();
  const user = session?.user;

  const {
    data: credentials,
    isLoading: credentialsLoading,
    error: credentialsError,
  } = api.membershipCredential.getByUserId.useQuery(
    { userId: user?.id },
    { enabled: !!user?.id },
  );

  const {
    data: invitations,
    isLoading: invitationsLoading,
    error: invitationsError,
  } = api.project.getMyInvitations.useQuery(undefined, {
    enabled: !!user?.id,
  });

  const utils = api.useUtils();
  const custodyStatus = api.azoaCustodialAccount.getStatus.useQuery(undefined, {
    enabled: !!user?.id,
    retry: false,
  });
  const ensureCustody = api.azoaCustodialAccount.ensure.useMutation({
    onSuccess: async () => {
      await utils.azoaCustodialAccount.getStatus.invalidate();
      toast.success("Your secure Azoa account status is up to date.");
    },
    onError: (error) => toast.error(error.message),
  });

  const acceptMutation = api.project.acceptInvitation.useMutation({
    onSuccess: () => {
      void utils.project.getMyInvitations.invalidate();
      toast.success("Invitation accepted — you have joined the project.");
    },
    onError: (err) => {
      toast.error(err.message);
    },
  });

  const rejectMutation = api.project.rejectInvitation.useMutation({
    onSuccess: () => {
      void utils.project.getMyInvitations.invalidate();
      toast.success("Invitation declined.");
    },
    onError: (err) => {
      toast.error(err.message);
    },
  });

  const pendingInvitations = normalizeInvitationViews(invitations).filter(
    (invitation) => invitation.status === "PENDING",
  );

  const {
    data: myProjects,
    isLoading: projectsLoading,
    error: projectsError,
  } = api.project.getMyProjects.useQuery(
    { limit: 100, page: 1 },
    { enabled: !!user?.id },
  );
  const {
    data: myGuilds,
    isLoading: guildsLoading,
    error: guildsError,
  } = api.guild.getMyGuilds.useQuery(undefined, { enabled: !!user?.id });
  const { data: myTasks, error: tasksError } = api.task.getMyTasks.useQuery(
    { limit: 100 },
    { enabled: !!user?.id },
  );

  const { data: meProfile, error: profileError } = api.user.getById.useQuery(
    { id: user?.id ?? "" },
    { enabled: !!user?.id },
  );

  const activeCredentials =
    credentials?.filter((c) => c.status === "ACTIVE") ?? [];
  const highestTier = getHighestTier(
    activeCredentials
      .map((credential) => credential.tier)
      .filter((tier): tier is string => typeof tier === "string"),
  );
  const myGuildRows = normalizeGuildViews(myGuilds);

  if (sessionStatus === "loading") {
    return <p className="text-muted-foreground p-6">Loading profile…</p>;
  }

  if (sessionStatus === "unauthenticated" || !user) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4 p-6">
        <p className="text-muted-foreground">Sign in to view your profile.</p>
        <Button asChild variant="neon">
          <Link href="/auth/signin?callbackUrl=%2Fdashboard%2Fprofile">
            Sign in
          </Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="bg-background">
      <div className="mx-auto max-w-4xl px-4 py-6">
        {/* Profile Header */}
        <Card className="mb-6">
          <CardContent className="pt-6">
            <div className="flex flex-col items-start gap-6 sm:flex-row">
              <Avatar className="border-primary size-24 border-4">
                <AvatarImage
                  src={user?.image || undefined}
                  alt={user?.name || "User"}
                />
                <AvatarFallback className="text-2xl">
                  {user?.name?.charAt(0) || "U"}
                </AvatarFallback>
              </Avatar>

              <div className="flex-1">
                <div className="mb-2 flex flex-wrap items-center gap-3">
                  <h1 className="text-foreground text-2xl font-bold">
                    {user?.name || "Anonymous User"}
                  </h1>
                  <Badge variant="neon">Member</Badge>
                </div>
                <p className="text-muted-foreground mb-4">{user?.email}</p>
                <div className="flex flex-wrap gap-6 text-sm">
                  <div className="flex items-center gap-2">
                    <FolderKanban className="text-primary size-4" />
                    <span>
                      <strong>
                        {projectsLoading
                          ? "…"
                          : projectsError
                            ? "Unavailable"
                            : (myProjects?.items?.length ?? 0)}
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
                            : myGuildRows.length}
                      </strong>{" "}
                      Guilds
                    </span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Calendar className="text-neon-green size-4" />
                    <span>
                      Joined{" "}
                      {profileError
                        ? "unavailable"
                        : meProfile?.createdAt
                          ? new Date(meProfile.createdAt).toLocaleDateString(
                              "en-US",
                              {
                                month: "short",
                                year: "numeric",
                              },
                            )
                          : "—"}
                    </span>
                  </div>
                  {activeCredentials.length > 0 && (
                    <div className="flex items-center gap-2">
                      <Shield className="text-neon-cyan size-4" />
                      <span>
                        <strong>{activeCredentials.length}</strong>{" "}
                        {activeCredentials.length === 1
                          ? "Credential"
                          : "Credentials"}
                      </span>
                      {highestTier && (
                        <CredentialBadge tier={highestTier} size="sm" />
                      )}
                    </div>
                  )}
                </div>
              </div>

              <div className="flex gap-2 sm:ml-auto">
                <Button variant="outline" size="sm" asChild>
                  <Link href="/settings">
                    <Settings className="mr-2 size-4" />
                    Open settings
                  </Link>
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>

        <div className="mb-6">
          <CustodialAccountCard
            status={custodyStatus.data}
            isLoading={custodyStatus.isLoading}
            isEnsuring={ensureCustody.isPending}
            statusError={custodyStatus.error?.message}
            ensureError={ensureCustody.error?.message}
            onEnsure={() => ensureCustody.mutate()}
          />
        </div>

        {/* Profile Tabs */}
        <Tabs defaultValue="projects" className="w-full">
          <TabsList className="w-full justify-start overflow-x-auto">
            <TabsTrigger value="projects">Projects</TabsTrigger>
            <TabsTrigger value="invitations" className="relative">
              Invitations
              {pendingInvitations.length > 0 && (
                <span className="bg-primary text-primary-foreground ml-1.5 inline-flex items-center justify-center rounded-none px-1.5 py-0.5 text-[10px] font-bold">
                  {pendingInvitations.length}
                </span>
              )}
            </TabsTrigger>
            <TabsTrigger value="guilds">Guilds</TabsTrigger>
            <TabsTrigger value="credentials">Credentials</TabsTrigger>
            <TabsTrigger value="contributions">Contributions</TabsTrigger>
          </TabsList>

          <TabsContent value="projects" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle>Your Projects</CardTitle>
              </CardHeader>
              <CardContent>
                {projectsLoading ? (
                  <p className="text-muted-foreground">Loading projects…</p>
                ) : projectsError ? (
                  <p role="alert" className="text-destructive">
                    Projects could not be loaded.
                  </p>
                ) : (myProjects?.items.length ?? 0) === 0 ? (
                  <p className="text-muted-foreground">
                    You haven&apos;t joined any projects yet.
                  </p>
                ) : (
                  <ul className="space-y-2">
                    {myProjects?.items.map((project) => (
                      <li key={project.id}>
                        <Link
                          href={`/projects/${project.slug}`}
                          className="text-primary inline-flex min-h-11 items-center font-medium hover:underline"
                        >
                          {project.title}
                        </Link>
                      </li>
                    ))}
                  </ul>
                )}
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="invitations" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Mail className="size-5" />
                  Project Invitations
                </CardTitle>
              </CardHeader>
              <CardContent>
                {invitationsLoading ? (
                  <p className="text-muted-foreground">Loading invitations…</p>
                ) : invitationsError ? (
                  <p role="alert" className="text-destructive">
                    Invitations could not be loaded.
                  </p>
                ) : pendingInvitations.length === 0 ? (
                  <p className="text-muted-foreground">
                    No pending invitations.
                  </p>
                ) : (
                  <div className="space-y-4">
                    {pendingInvitations.map((inv) => (
                      <div
                        key={inv.id}
                        className="flex flex-col gap-4 rounded-lg border p-4 sm:flex-row sm:items-center sm:justify-between"
                      >
                        <div className="flex items-center gap-4">
                          <Avatar className="size-10">
                            <AvatarImage
                              src={inv.project?.image}
                              alt={inv.project?.title || "Project"}
                            />
                            <AvatarFallback>
                              {inv.project?.title?.charAt(0) || "P"}
                            </AvatarFallback>
                          </Avatar>
                          <div>
                            <p className="font-medium">
                              {inv.project?.title || "Unknown Project"}
                            </p>
                            <div className="text-muted-foreground flex items-center gap-2 text-sm">
                              <span>Role:</span>
                              <Badge variant={getRoleBadgeVariant(inv.role)}>
                                {formatRoleName(inv.role)}
                              </Badge>
                              {inv.invitedByName && (
                                <span>from {inv.invitedByName}</span>
                              )}
                            </div>
                            {inv.message && (
                              <p className="text-muted-foreground mt-1 text-sm italic">
                                &ldquo;{inv.message}&rdquo;
                              </p>
                            )}
                          </div>
                        </div>
                        <div className="flex w-full flex-col gap-2 sm:w-auto sm:flex-row">
                          <Button
                            size="sm"
                            onClick={() =>
                              acceptMutation.mutate({
                                projectId: inv.projectId,
                                invitationId: inv.id,
                              })
                            }
                            disabled={
                              acceptMutation.isPending ||
                              rejectMutation.isPending
                            }
                          >
                            <Check className="mr-1 size-4" />
                            Accept
                          </Button>
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() =>
                              rejectMutation.mutate({
                                projectId: inv.projectId,
                                invitationId: inv.id,
                              })
                            }
                            disabled={
                              acceptMutation.isPending ||
                              rejectMutation.isPending
                            }
                          >
                            <X className="mr-1 size-4" />
                            Decline
                          </Button>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="guilds" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle>Your Guilds</CardTitle>
              </CardHeader>
              <CardContent>
                {guildsLoading ? (
                  <p className="text-muted-foreground">Loading guilds…</p>
                ) : guildsError ? (
                  <p role="alert" className="text-destructive">
                    Guilds could not be loaded.
                  </p>
                ) : myGuildRows.length === 0 ? (
                  <p className="text-muted-foreground">
                    You haven&apos;t joined any guilds yet.
                  </p>
                ) : (
                  <ul className="space-y-2">
                    {myGuildRows.map((g) => (
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

          <TabsContent value="credentials" className="mt-6">
            <CredentialList
              credentials={credentials}
              isLoading={credentialsLoading}
              emptyMessage={
                credentialsError
                  ? "Credentials could not be loaded."
                  : "You don't have any credentials yet. Join a project or guild to earn one!"
              }
            />
          </TabsContent>

          <TabsContent value="contributions" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle>Contributions</CardTitle>
              </CardHeader>
              <CardContent>
                {tasksError || profileError ? (
                  <p role="alert" className="text-destructive">
                    Contribution details could not be loaded.
                  </p>
                ) : (
                  <>
                    <p className="text-muted-foreground">
                      Active tasks:{" "}
                      <strong>{myTasks?.items?.length ?? 0}</strong>
                    </p>
                    <p className="text-muted-foreground mt-2">
                      XP: <strong>{meProfile?.totalXP ?? 0}</strong> · Level{" "}
                      <strong>{meProfile?.level ?? 1}</strong>
                    </p>
                  </>
                )}
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
  }, tiers[0]);
}
