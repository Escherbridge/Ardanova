"use client";

import { useState, useEffect } from "react";
import { useSession } from "next-auth/react";
import { api, type RouterInputs, type RouterOutputs } from "~/trpc/react";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "~/components/ui/accordion";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "~/components/ui/dropdown-menu";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "~/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "~/components/ui/dialog";
import { CredentialBadge } from "~/components/credentials/credential-badge";
import {
  Loader2,
  Users,
  Plus,
  Check,
  X,
  Shield,
  ChevronUp,
  UserPlus,
  Search,
  PieChart,
  Pencil,
  AlertCircle,
  RefreshCw,
} from "lucide-react";
import Link from "next/link";
import { toast } from "sonner";
import { cn } from "~/lib/utils";

interface TeamTabProps {
  projectId: string;
  projectSlug?: string;
  isOwner: boolean;
}

export type MemberRole = RouterInputs["project"]["updateMemberRole"]["role"];
type ApplicationStatus = RouterInputs["project"]["reviewApplication"]["status"];
type ProjectOpportunity =
  RouterOutputs["opportunity"]["getByProjectId"][number];
type MembershipCredential =
  RouterOutputs["membershipCredential"]["getByProjectId"][number];

export const getRoleBadgeVariant = (role: string) => {
  switch (role) {
    case "FOUNDER":
      return "neon-pink-solid" as const;
    case "LEADER":
      return "neon-purple" as const;
    case "CORE_CONTRIBUTOR":
      return "neon-green" as const;
    case "CONTRIBUTOR":
      return "info" as const;
    case "OBSERVER":
      return "outline" as const;
    default:
      return "default" as const;
  }
};

export const formatRoleName = (role: string) => {
  return role
    .split("_")
    .map((word) => word.charAt(0) + word.slice(1).toLowerCase())
    .join(" ");
};

const TIERS = ["BRONZE", "SILVER", "GOLD", "PLATINUM", "DIAMOND"] as const;

const DEFAULT_ROLES = [
  {
    id: "FOUNDER",
    name: "Founder",
    description:
      "Founding steward with project work-management capability; creator-only controls remain separate",
  },
  {
    id: "LEADER",
    name: "Leader",
    description: "Can manage project work and publish project opportunities",
  },
  {
    id: "CORE_CONTRIBUTOR",
    name: "Core Contributor",
    description: "Can manage project work and publish project opportunities",
  },
  {
    id: "CONTRIBUTOR",
    name: "Contributor",
    description: "Can contribute to assigned work without management access",
  },
  {
    id: "OBSERVER",
    name: "Observer",
    description: "Can follow progress without project-management access",
  },
] as const satisfies ReadonlyArray<{
  id: MemberRole;
  name: string;
  description: string;
}>;

function isMemberRole(value: string): value is MemberRole {
  return DEFAULT_ROLES.some((role) => role.id === value);
}

function getOpportunityPath(opportunity: ProjectOpportunity, suffix = "") {
  return `/opportunities/${opportunity.slug ?? opportunity.id}${suffix}`;
}

// Invite Member Dialog Component
function InviteMemberDialog({
  projectId,
  role,
  existingMemberIds,
  isOwner,
  currentUserId,
}: {
  projectId: string;
  role: MemberRole;
  existingMemberIds: string[];
  isOwner: boolean;
  currentUserId?: string;
}) {
  const [open, setOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [inviteMessage, setInviteMessage] = useState("");
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  const [selectedUserName, setSelectedUserName] = useState<string | null>(null);
  const [sourceTab, setSourceTab] = useState<"search" | "following">("search");
  const utils = api.useUtils();

  // Debounce search input
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchTerm), 300);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  // Paginated user search — only fires when 2+ characters typed
  const { data: searchResults, isLoading: searchLoading } =
    api.user.search.useQuery(
      { query: debouncedSearch, page: 1, limit: 10 },
      { enabled: open && isOwner && debouncedSearch.length >= 2 },
    );

  // Filter out existing members from results
  const availableUsers = (searchResults?.items ?? []).filter(
    (user) => !existingMemberIds.includes(user.id),
  );

  // Fetch followed users when "Following" tab is active
  const { data: followedUsers, isLoading: followingLoading } =
    api.user.getFollowingWithUsers.useQuery(
      { userId: currentUserId! },
      {
        enabled:
          open && isOwner && sourceTab === "following" && !!currentUserId,
      },
    );

  const availableFollowedUsers = (followedUsers ?? []).filter(
    (user) => !existingMemberIds.includes(user.id),
  );

  const createInvitationMutation = api.project.createInvitation.useMutation({
    onSuccess: () => {
      toast.success("Invitation sent successfully");
      setOpen(false);
      setSelectedUserId(null);
      setSelectedUserName(null);
      setInviteMessage("");
      setSearchTerm("");
      setDebouncedSearch("");
      void utils.project.getInvitations.invalidate({ projectId });
    },
    onError: (error) => {
      toast.error(error.message || "Failed to send invitation");
    },
  });

  const handleInvite = () => {
    if (!selectedUserId) return;
    createInvitationMutation.mutate({
      projectId,
      invitedUserId: selectedUserId,
      role,
      message: inviteMessage || undefined,
    });
  };

  if (!isOwner) return null;

  return (
    <Dialog
      open={open}
      onOpenChange={(v) => {
        setOpen(v);
        if (!v) {
          setSearchTerm("");
          setDebouncedSearch("");
          setSelectedUserId(null);
          setSelectedUserName(null);
          setInviteMessage("");
          setSourceTab("search");
        }
      }}
    >
      <DialogTrigger asChild>
        <Button
          variant="ghost"
          size="sm"
          className="min-h-11 px-2 text-xs"
          aria-label={`Invite member as ${formatRoleName(role)}`}
        >
          <UserPlus className="mr-1 size-3.5" aria-hidden="true" />
          Invite
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Invite Member as {formatRoleName(role)}</DialogTitle>
          <DialogDescription>
            Search for a user to invite to this project
          </DialogDescription>
        </DialogHeader>
        <div className="space-y-4">
          {/* Source tab toggle */}
          {!selectedUserId && (
            <div className="border-border flex gap-1 border-b">
              <button
                type="button"
                className={cn(
                  "border-b-2 px-3 py-1.5 text-xs transition-colors",
                  sourceTab === "search"
                    ? "border-primary text-primary font-medium"
                    : "text-muted-foreground hover:text-foreground border-transparent",
                )}
                onClick={() => setSourceTab("search")}
              >
                <Search className="mr-1 inline size-3" />
                Search
              </button>
              <button
                type="button"
                className={cn(
                  "border-b-2 px-3 py-1.5 text-xs transition-colors",
                  sourceTab === "following"
                    ? "border-primary text-primary font-medium"
                    : "text-muted-foreground hover:text-foreground border-transparent",
                )}
                onClick={() => setSourceTab("following")}
              >
                <Users className="mr-1 inline size-3" />
                Following
              </button>
            </div>
          )}

          {/* Selected user chip */}
          {selectedUserId && selectedUserName && (
            <div className="bg-primary/10 border-primary flex items-center gap-2 rounded border px-3 py-1.5">
              <Check className="text-primary size-3.5" />
              <span className="flex-1 text-sm font-medium">
                {selectedUserName}
              </span>
              <button
                onClick={() => {
                  setSelectedUserId(null);
                  setSelectedUserName(null);
                }}
                className="text-muted-foreground hover:text-foreground"
              >
                <X className="size-3.5" />
              </button>
            </div>
          )}

          {/* Search input */}
          {!selectedUserId && sourceTab === "search" && (
            <>
              <div className="relative">
                <Search className="text-muted-foreground absolute top-1/2 left-3 size-4 -translate-y-1/2" />
                <input
                  type="text"
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  placeholder="Search users by name..."
                  className="border-border bg-background text-foreground focus:ring-primary w-full rounded border py-2 pr-3 pl-9 text-sm focus:ring-2 focus:outline-none"
                  autoFocus
                />
              </div>

              <div className="max-h-48 space-y-1 overflow-y-auto">
                {debouncedSearch.length < 2 ? (
                  <div className="text-muted-foreground py-4 text-center text-sm">
                    Type at least 2 characters to search
                  </div>
                ) : searchLoading ? (
                  <div className="flex items-center justify-center py-4">
                    <Loader2 className="text-muted-foreground size-4 animate-spin" />
                  </div>
                ) : availableUsers.length === 0 ? (
                  <div className="text-muted-foreground py-4 text-center text-sm">
                    No matching users found
                  </div>
                ) : (
                  availableUsers.map((user) => (
                    <button
                      key={user.id}
                      onClick={() => {
                        setSelectedUserId(user.id);
                        setSelectedUserName(
                          user.name ?? user.email ?? "Unknown",
                        );
                      }}
                      className="hover:bg-muted/50 flex w-full items-center gap-3 rounded border border-transparent p-2 text-left transition-colors"
                    >
                      <Avatar className="size-8">
                        <AvatarImage src={user.image ?? undefined} />
                        <AvatarFallback className="text-xs">
                          {(user.name ?? user.email ?? "U")
                            .charAt(0)
                            .toUpperCase()}
                        </AvatarFallback>
                      </Avatar>
                      <div className="flex flex-col">
                        <span className="text-sm font-medium">
                          {user.name ?? "Unnamed"}
                        </span>
                        {user.email && (
                          <span className="text-muted-foreground text-xs">
                            {user.email}
                          </span>
                        )}
                      </div>
                    </button>
                  ))
                )}
              </div>
            </>
          )}

          {/* Following list */}
          {!selectedUserId && sourceTab === "following" && (
            <div className="max-h-48 space-y-1 overflow-y-auto">
              {followingLoading ? (
                <div className="flex items-center justify-center py-4">
                  <Loader2 className="text-muted-foreground size-4 animate-spin" />
                </div>
              ) : availableFollowedUsers.length === 0 ? (
                <div className="text-muted-foreground py-4 text-center text-sm">
                  {(followedUsers?.length ?? 0) === 0
                    ? "You're not following anyone yet"
                    : "All followed users are already members"}
                </div>
              ) : (
                availableFollowedUsers.map((user) => (
                  <button
                    key={user.id}
                    onClick={() => {
                      setSelectedUserId(user.id);
                      setSelectedUserName(user.name ?? user.email ?? "Unknown");
                    }}
                    className="hover:bg-muted/50 flex w-full items-center gap-3 rounded border border-transparent p-2 text-left transition-colors"
                  >
                    <Avatar className="size-8">
                      <AvatarImage src={user.image ?? undefined} />
                      <AvatarFallback className="text-xs">
                        {(user.name ?? user.email ?? "U")
                          .charAt(0)
                          .toUpperCase()}
                      </AvatarFallback>
                    </Avatar>
                    <div className="flex flex-col">
                      <span className="text-sm font-medium">
                        {user.name ?? "Unnamed"}
                      </span>
                      {user.email && (
                        <span className="text-muted-foreground text-xs">
                          {user.email}
                        </span>
                      )}
                    </div>
                  </button>
                ))
              )}
            </div>
          )}

          {selectedUserId && (
            <div>
              <label className="mb-1.5 block text-sm font-medium">
                Message (optional)
              </label>
              <textarea
                value={inviteMessage}
                onChange={(e) => setInviteMessage(e.target.value)}
                placeholder="Add a personal message to the invitation..."
                className="border-border bg-background text-foreground focus:ring-primary min-h-[60px] w-full resize-y rounded border px-3 py-2 text-sm focus:ring-2 focus:outline-none"
              />
            </div>
          )}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => setOpen(false)}>
            Cancel
          </Button>
          <Button
            onClick={handleInvite}
            disabled={!selectedUserId || createInvitationMutation.isPending}
          >
            {createInvitationMutation.isPending ? (
              <>
                <Loader2 className="mr-2 size-4 animate-spin" />
                Sending...
              </>
            ) : (
              "Send Invitation"
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export default function TeamTab({
  projectId,
  projectSlug,
  isOwner,
}: TeamTabProps) {
  const [showApplicationForm, setShowApplicationForm] = useState(false);
  const [roleTitle, setRoleTitle] = useState("");
  const [message, setMessage] = useState("");
  const [skills, setSkills] = useState("");
  const [experience, setExperience] = useState("");
  const [availability, setAvailability] = useState("");
  const { data: session } = useSession();

  const utils = api.useUtils();

  // Queries
  const {
    data: members,
    error: membersError,
    isLoading: membersLoading,
    refetch: retryMembers,
  } = api.project.getMembers.useQuery({ projectId });

  const {
    data: opportunities,
    error: opportunitiesError,
    refetch: retryOpportunities,
  } = api.opportunity.getByProjectId.useQuery({ projectId });

  const { data: applications, isLoading: applicationsLoading } =
    api.project.getApplications.useQuery({ projectId }, { enabled: isOwner });

  const { data: projectCredentials } =
    api.membershipCredential.getByProjectId.useQuery({
      projectId,
    });

  const { data: tokenConfig } = api.projectTokens.getConfigByProject.useQuery(
    { projectId },
    { retry: false },
  );

  const credentialsByUserId = new Map<string, MembershipCredential>();
  for (const credential of projectCredentials ?? []) {
    credentialsByUserId.set(credential.userId, credential);
  }

  // Mutations
  const applyMutation = api.project.applyToProject.useMutation({
    onSuccess: () => {
      setShowApplicationForm(false);
      setRoleTitle("");
      setMessage("");
      setSkills("");
      setExperience("");
      setAvailability("");
    },
    onError: (error) => {
      alert(error.message);
    },
  });

  const reviewMutation = api.project.reviewApplication.useMutation({
    onMutate: async (variables) => {
      if (!variables) return;
      const { applicationId, status } = variables;
      await utils.project.getApplications.cancel({ projectId });
      const previous = utils.project.getApplications.getData({ projectId });
      utils.project.getApplications.setData({ projectId }, (old) =>
        old?.map((app) =>
          app.id === applicationId ? { ...app, status } : app,
        ),
      );
      return { previous };
    },
    onError: (err, _vars, context) => {
      if (context?.previous) {
        utils.project.getApplications.setData({ projectId }, context.previous);
      }
      alert(err.message);
    },
    onSettled: () => {
      void utils.project.getApplications.invalidate({ projectId });
      void utils.project.getMembers.invalidate({ projectId });
    },
  });

  const grantCredentialMutation = api.membershipCredential.grant.useMutation({
    onSuccess: () => {
      toast.success("Credential granted successfully");
      void utils.membershipCredential.getByProjectId.invalidate({ projectId });
    },
    onError: (error) => {
      toast.error(error.message || "Failed to grant credential");
    },
  });

  const updateMemberRoleMutation = api.project.updateMemberRole.useMutation({
    onSuccess: () => {
      toast.success("Member role updated");
      void utils.project.getMembers.invalidate({ projectId });
    },
    onError: (error) => {
      toast.error(error.message || "Failed to update member role");
    },
  });

  const updateOpportunityRoleMutation = api.opportunity.update.useMutation({
    onSuccess: () => {
      toast.success("Role assigned successfully");
      void utils.opportunity.getByProjectId.invalidate({ projectId });
    },
    onError: (error) => {
      toast.error(error.message || "Failed to assign role");
    },
  });

  const updateTierMutation = api.membershipCredential.updateTier.useMutation({
    onSuccess: () => {
      toast.success("Credential tier updated");
      void utils.membershipCredential.getByProjectId.invalidate({ projectId });
    },
    onError: (error) => {
      toast.error(error.message || "Failed to update tier");
    },
  });

  const handleApply = (e: React.FormEvent) => {
    e.preventDefault();
    applyMutation.mutate({
      projectId,
      roleTitle,
      message,
      skills: skills || undefined,
      experience: experience || undefined,
      availability: availability || undefined,
    });
  };

  const handleReview = (applicationId: string, status: ApplicationStatus) => {
    reviewMutation.mutate({
      projectId,
      applicationId,
      status,
    });
  };

  const handleGrantCredential = (userId: string) => {
    grantCredentialMutation.mutate({
      projectId,
      userId,
      grantedVia: "FOUNDER",
    });
  };

  if (membersLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="text-muted-foreground size-6 animate-spin" />
      </div>
    );
  }

  if (membersError || opportunitiesError) {
    const teamError = membersError ?? opportunitiesError;
    return (
      <div className="border-destructive/40 bg-destructive/5 flex flex-col items-start gap-4 border-2 p-6 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-start gap-3">
          <AlertCircle className="text-destructive mt-0.5 size-5 shrink-0" />
          <div>
            <p className="text-destructive font-mono text-sm font-bold">
              TEAM COULD NOT BE LOADED
            </p>
            <p className="text-muted-foreground mt-1 text-xs">
              {teamError?.message ?? "Unknown error"}
            </p>
          </div>
        </div>
        <Button
          type="button"
          variant="outline"
          className="min-h-11 shrink-0"
          onClick={() => {
            if (membersError) void retryMembers();
            if (opportunitiesError) void retryOpportunities();
          }}
        >
          <RefreshCw className="mr-2 size-4" />
          Retry
        </Button>
      </div>
    );
  }

  const pendingApplications =
    applications?.filter((app) => app.status === "PENDING") ?? [];

  // Group opportunities by their linked projectRole
  const opportunitiesByRole = new Map<MemberRole, ProjectOpportunity[]>();
  const unlinkedOpportunities: ProjectOpportunity[] = [];
  for (const opportunity of opportunities ?? []) {
    if (opportunity.projectRole && isMemberRole(opportunity.projectRole)) {
      const existing = opportunitiesByRole.get(opportunity.projectRole) ?? [];
      existing.push(opportunity);
      opportunitiesByRole.set(opportunity.projectRole, existing);
    } else {
      unlinkedOpportunities.push(opportunity);
    }
  }

  const existingMemberIds = (members ?? []).map((member) => member.userId);

  const currentUserId = session?.user?.id;
  const isCurrentUserMember = currentUserId
    ? existingMemberIds.includes(currentUserId)
    : false;

  return (
    <div className="space-y-6">
      {/* Team & Roles - Accordion */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <Users className="size-5" />
                Team & Roles
              </CardTitle>
              <CardDescription>
                Team positions, members, and open opportunities
              </CardDescription>
            </div>
            {!isOwner && !isCurrentUserMember && (
              <Button
                onClick={() => setShowApplicationForm(!showApplicationForm)}
                variant="default"
                size="sm"
              >
                <Plus className="mr-2 size-4" />
                Apply to Join
              </Button>
            )}
          </div>
        </CardHeader>
        <CardContent>
          <Accordion type="multiple" defaultValue={["FOUNDER", "LEADER"]}>
            {DEFAULT_ROLES.map((role) => {
              const filledMembers =
                members?.filter((member) => member.role === role.id) ?? [];
              const linkedPositions = opportunitiesByRole.get(role.id) ?? [];
              const openPositions = linkedPositions.filter(
                (opportunity) =>
                  opportunity.status !== "FILLED" &&
                  opportunity.status !== "CLOSED",
              );

              return (
                <AccordionItem key={role.id} value={role.id}>
                  <AccordionTrigger className="hover:no-underline">
                    <div className="flex items-center gap-2">
                      <Badge variant={getRoleBadgeVariant(role.id)}>
                        {role.name}
                      </Badge>
                      <span className="text-muted-foreground text-xs">
                        {filledMembers.length}{" "}
                        {filledMembers.length === 1 ? "member" : "members"}
                        {openPositions.length > 0 && (
                          <>
                            {" "}
                            · {openPositions.length} open{" "}
                            {openPositions.length === 1
                              ? "position"
                              : "positions"}
                          </>
                        )}
                      </span>
                    </div>
                  </AccordionTrigger>
                  <AccordionContent>
                    <div className="space-y-3 pt-1">
                      {/* Role description */}
                      <p className="text-muted-foreground text-sm">
                        {role.description}
                      </p>

                      {/* Members in this role */}
                      {filledMembers.length > 0 && (
                        <div className="space-y-2">
                          {filledMembers.map((member) => {
                            const credential = credentialsByUserId.get(
                              member.userId,
                            );
                            const hasCredential =
                              credential?.status === "ACTIVE";
                            return (
                              <div
                                key={member.id}
                                className="border-border hover:bg-muted/50 flex items-center justify-between rounded border p-3 transition-colors"
                              >
                                <div className="flex items-center gap-3">
                                  <Avatar className="size-8">
                                    <AvatarImage
                                      src={member.user?.image ?? undefined}
                                    />
                                    <AvatarFallback className="text-xs">
                                      {member.user?.name
                                        ?.charAt(0)
                                        .toUpperCase() ?? "U"}
                                    </AvatarFallback>
                                  </Avatar>
                                  <div>
                                    <div className="text-sm font-medium">
                                      {member.user?.name ??
                                        member.user?.email ??
                                        "Unknown User"}
                                    </div>
                                    <div className="text-muted-foreground text-xs">
                                      Joined{" "}
                                      {new Date(
                                        member.joinedAt,
                                      ).toLocaleDateString()}
                                    </div>
                                  </div>
                                </div>
                                <div className="flex items-center gap-2">
                                  {isOwner &&
                                    member.userId !== currentUserId && (
                                      <Select
                                        defaultValue={member.role}
                                        onValueChange={(value) => {
                                          if (!isMemberRole(value)) return;
                                          updateMemberRoleMutation.mutate({
                                            projectId,
                                            memberId: member.id,
                                            role: value,
                                          });
                                        }}
                                      >
                                        <SelectTrigger
                                          className="min-h-11 w-[140px] text-xs"
                                          aria-label={`Change role for ${member.user?.name ?? member.user?.email ?? "member"}`}
                                        >
                                          <SelectValue />
                                        </SelectTrigger>
                                        <SelectContent>
                                          {DEFAULT_ROLES.map((r) => (
                                            <SelectItem key={r.id} value={r.id}>
                                              {r.name}
                                            </SelectItem>
                                          ))}
                                        </SelectContent>
                                      </Select>
                                    )}
                                  {hasCredential && (
                                    <>
                                      <CredentialBadge
                                        tier={credential?.tier}
                                        size="sm"
                                      />
                                      {/* Tier upgrade dropdown (owner only) */}
                                      {isOwner && (
                                        <DropdownMenu>
                                          <DropdownMenuTrigger asChild>
                                            <Button
                                              variant="ghost"
                                              size="sm"
                                              className="size-11 p-0"
                                              aria-label={`Upgrade credential tier for ${member.user?.name ?? member.user?.email ?? "member"}`}
                                              title="Upgrade credential tier"
                                            >
                                              <ChevronUp
                                                className="size-3.5"
                                                aria-hidden="true"
                                              />
                                            </Button>
                                          </DropdownMenuTrigger>
                                          <DropdownMenuContent align="end">
                                            {TIERS.map((tier) => (
                                              <DropdownMenuItem
                                                key={tier}
                                                disabled={
                                                  credential?.tier === tier ||
                                                  updateTierMutation.isPending
                                                }
                                                onClick={() =>
                                                  updateTierMutation.mutate({
                                                    credentialId: credential.id,
                                                    tier,
                                                  })
                                                }
                                              >
                                                <span
                                                  className={
                                                    credential?.tier === tier
                                                      ? "font-bold"
                                                      : ""
                                                  }
                                                >
                                                  {tier.charAt(0) +
                                                    tier.slice(1).toLowerCase()}
                                                </span>
                                                {credential?.tier === tier && (
                                                  <Check
                                                    className="ml-auto size-3.5"
                                                    aria-hidden="true"
                                                  />
                                                )}
                                              </DropdownMenuItem>
                                            ))}
                                          </DropdownMenuContent>
                                        </DropdownMenu>
                                      )}
                                    </>
                                  )}
                                  {isOwner && !hasCredential && (
                                    <Button
                                      variant="ghost"
                                      size="sm"
                                      className="text-muted-foreground hover:text-primary min-h-11 px-2 text-xs"
                                      onClick={() =>
                                        handleGrantCredential(member.userId)
                                      }
                                      disabled={
                                        grantCredentialMutation.isPending
                                      }
                                      aria-label={`Grant membership credential to ${member.user?.name ?? member.user?.email ?? "member"}`}
                                      title="Grant membership credential"
                                    >
                                      <Shield
                                        className="mr-1 size-3.5"
                                        aria-hidden="true"
                                      />
                                      Grant
                                    </Button>
                                  )}
                                  {/* Edit linked opportunity (owner only) */}
                                  {isOwner && linkedPositions.length === 1 && (
                                    <Button
                                      variant="ghost"
                                      size="sm"
                                      className="min-h-11 px-2 text-xs"
                                      asChild
                                    >
                                      <Link
                                        href={getOpportunityPath(
                                          linkedPositions[0],
                                          "/edit",
                                        )}
                                        aria-label={`Edit ${linkedPositions[0]?.title ?? "linked"} position`}
                                      >
                                        <Pencil
                                          className="mr-1 size-3.5"
                                          aria-hidden="true"
                                        />
                                        Edit Position
                                      </Link>
                                    </Button>
                                  )}
                                  {isOwner && linkedPositions.length > 1 && (
                                    <DropdownMenu>
                                      <DropdownMenuTrigger asChild>
                                        <Button
                                          variant="ghost"
                                          size="sm"
                                          className="min-h-11 px-2 text-xs"
                                          aria-label={`Choose a position to edit for ${member.user?.name ?? member.user?.email ?? "member"}`}
                                        >
                                          <Pencil
                                            className="mr-1 size-3.5"
                                            aria-hidden="true"
                                          />
                                          Edit Position
                                        </Button>
                                      </DropdownMenuTrigger>
                                      <DropdownMenuContent align="end">
                                        {linkedPositions.map((opp) => (
                                          <DropdownMenuItem
                                            key={opp.id}
                                            asChild
                                          >
                                            <Link
                                              href={getOpportunityPath(
                                                opp,
                                                "/edit",
                                              )}
                                            >
                                              {opp.title}
                                            </Link>
                                          </DropdownMenuItem>
                                        ))}
                                      </DropdownMenuContent>
                                    </DropdownMenu>
                                  )}
                                </div>
                              </div>
                            );
                          })}
                        </div>
                      )}

                      {/* Define position for role without opportunity (owner only) */}
                      {isOwner &&
                        linkedPositions.length === 0 &&
                        filledMembers.length > 0 && (
                          <div className="pt-1">
                            <Button
                              variant="ghost"
                              size="sm"
                              className="min-h-11 px-2 text-xs"
                              asChild
                            >
                              <Link
                                href={`/opportunities/create?projectId=${projectId}${projectSlug ? `&projectSlug=${projectSlug}` : ""}&projectRole=${role.id}`}
                                aria-label={`Define ${role.name} position`}
                              >
                                <Plus
                                  className="mr-1 size-3.5"
                                  aria-hidden="true"
                                />
                                Define {role.name} Position
                              </Link>
                            </Button>
                          </div>
                        )}

                      {/* Linked team positions */}
                      {linkedPositions.length > 0 && (
                        <div className="space-y-1.5">
                          <div className="text-muted-foreground text-xs font-medium tracking-wide uppercase">
                            Positions
                          </div>
                          {linkedPositions.map((opp) => (
                            <div
                              key={opp.id}
                              className="text-muted-foreground flex items-center gap-2 pl-2 text-xs"
                            >
                              <span className="bg-neon h-1.5 w-1.5 shrink-0 rounded-none" />
                              <Link
                                href={getOpportunityPath(opp)}
                                className="text-foreground hover:text-neon font-medium transition-colors hover:underline"
                              >
                                {opp.title}
                              </Link>
                              {(opp.applicationsCount ?? 0) > 0 && (
                                <span>
                                  ({opp.applicationsCount}{" "}
                                  {(opp.applicationsCount ?? 0) === 1
                                    ? "applicant"
                                    : "applicants"}
                                  )
                                </span>
                              )}
                              <Badge
                                variant={
                                  opp.status === "FILLED" ||
                                  opp.status === "CLOSED"
                                    ? "secondary"
                                    : "outline"
                                }
                                className="h-4 px-1.5 text-[10px]"
                              >
                                {opp.status === "FILLED" ||
                                opp.status === "CLOSED"
                                  ? "Filled"
                                  : "Open"}
                              </Badge>
                              {isOwner && (
                                <Select
                                  defaultValue={opp.projectRole ?? undefined}
                                  onValueChange={(value) => {
                                    if (!isMemberRole(value)) return;
                                    updateOpportunityRoleMutation.mutate({
                                      id: opp.id,
                                      projectRole: value,
                                    });
                                  }}
                                >
                                  <SelectTrigger className="ml-auto min-h-11 w-[120px] text-[10px]">
                                    <SelectValue />
                                  </SelectTrigger>
                                  <SelectContent>
                                    {DEFAULT_ROLES.map((r) => (
                                      <SelectItem key={r.id} value={r.id}>
                                        {r.name}
                                      </SelectItem>
                                    ))}
                                  </SelectContent>
                                </Select>
                              )}
                            </div>
                          ))}
                        </div>
                      )}

                      {/* Invite button (owner only) */}
                      {isOwner && (
                        <div className="pt-1">
                          <InviteMemberDialog
                            projectId={projectId}
                            role={role.id}
                            existingMemberIds={existingMemberIds}
                            isOwner={isOwner}
                            currentUserId={currentUserId}
                          />
                        </div>
                      )}

                      {/* Empty state */}
                      {filledMembers.length === 0 &&
                        linkedPositions.length === 0 &&
                        !isOwner && (
                          <div className="text-muted-foreground text-sm italic">
                            No members or positions yet.
                          </div>
                        )}
                    </div>
                  </AccordionContent>
                </AccordionItem>
              );
            })}
          </Accordion>

          {/* Custom Positions (unlinked to a role) */}
          {unlinkedOpportunities.length > 0 && (
            <div className="mt-4 space-y-2">
              <div className="text-muted-foreground text-xs font-medium tracking-wide uppercase">
                Custom Positions
              </div>
              {unlinkedOpportunities.map((opp) => {
                const isFilled =
                  opp.status === "FILLED" || opp.status === "CLOSED";
                return (
                  <div
                    key={opp.id}
                    className="border-border hover:bg-muted/30 flex items-center justify-between rounded border p-3 transition-colors"
                  >
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <Link href={getOpportunityPath(opp)}>
                          <Badge
                            variant="neon"
                            className="cursor-pointer transition-opacity hover:opacity-80"
                          >
                            {opp.title}
                          </Badge>
                        </Link>
                        {(opp.applicationsCount ?? 0) > 0 && (
                          <span className="text-muted-foreground text-xs">
                            ({opp.applicationsCount}{" "}
                            {(opp.applicationsCount ?? 0) === 1
                              ? "applicant"
                              : "applicants"}
                            )
                          </span>
                        )}
                      </div>
                      {opp.description && (
                        <p className="text-muted-foreground mt-1 line-clamp-2 text-sm">
                          {opp.description}
                        </p>
                      )}
                    </div>
                    <div className="ml-4 flex items-center gap-2">
                      {isOwner && (
                        <Select
                          onValueChange={(value) => {
                            if (!isMemberRole(value)) return;
                            updateOpportunityRoleMutation.mutate({
                              id: opp.id,
                              projectRole: value,
                            });
                          }}
                        >
                          <SelectTrigger className="min-h-11 w-[140px] text-xs">
                            <SelectValue placeholder="Assign role" />
                          </SelectTrigger>
                          <SelectContent>
                            {DEFAULT_ROLES.map((role) => (
                              <SelectItem key={role.id} value={role.id}>
                                {role.name}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      )}
                      <Badge
                        variant={isFilled ? "secondary" : "outline"}
                        className="text-xs"
                      >
                        {isFilled ? "Filled" : "Open"}
                      </Badge>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Application Form */}
      {showApplicationForm && !isOwner && !isCurrentUserMember && (
        <Card>
          <CardHeader>
            <CardTitle>Apply to Join Project</CardTitle>
            <CardDescription>
              Tell the project owner why you&apos;d like to join and what you
              can contribute
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleApply} className="space-y-4">
              <div>
                <label
                  htmlFor="roleTitle"
                  className="mb-1.5 block text-sm font-medium"
                >
                  Desired Role Title *
                </label>
                <input
                  id="roleTitle"
                  type="text"
                  value={roleTitle}
                  onChange={(e) => setRoleTitle(e.target.value)}
                  placeholder="e.g., Frontend Developer, Designer, Marketing Lead"
                  className="border-border bg-background text-foreground focus:ring-primary w-full rounded border px-3 py-2 focus:ring-2 focus:outline-none"
                  required
                />
              </div>

              <div>
                <label
                  htmlFor="message"
                  className="mb-1.5 block text-sm font-medium"
                >
                  Application Message * (min 20 characters)
                </label>
                <textarea
                  id="message"
                  value={message}
                  onChange={(e) => setMessage(e.target.value)}
                  placeholder="Explain why you want to join this project and what you can contribute..."
                  className="border-border bg-background text-foreground focus:ring-primary min-h-[100px] w-full resize-y rounded border px-3 py-2 focus:ring-2 focus:outline-none"
                  required
                  minLength={20}
                />
              </div>

              <div>
                <label
                  htmlFor="skills"
                  className="mb-1.5 block text-sm font-medium"
                >
                  Skills (Optional)
                </label>
                <input
                  id="skills"
                  type="text"
                  value={skills}
                  onChange={(e) => setSkills(e.target.value)}
                  placeholder="e.g., React, TypeScript, UX Design"
                  className="border-border bg-background text-foreground focus:ring-primary w-full rounded border px-3 py-2 focus:ring-2 focus:outline-none"
                />
              </div>

              <div>
                <label
                  htmlFor="experience"
                  className="mb-1.5 block text-sm font-medium"
                >
                  Experience (Optional)
                </label>
                <textarea
                  id="experience"
                  value={experience}
                  onChange={(e) => setExperience(e.target.value)}
                  placeholder="Briefly describe your relevant experience..."
                  className="border-border bg-background text-foreground focus:ring-primary min-h-[80px] w-full resize-y rounded border px-3 py-2 focus:ring-2 focus:outline-none"
                />
              </div>

              <div>
                <label
                  htmlFor="availability"
                  className="mb-1.5 block text-sm font-medium"
                >
                  Availability (Optional)
                </label>
                <input
                  id="availability"
                  type="text"
                  value={availability}
                  onChange={(e) => setAvailability(e.target.value)}
                  placeholder="e.g., 10 hours/week, Weekends, Full-time"
                  className="border-border bg-background text-foreground focus:ring-primary w-full rounded border px-3 py-2 focus:ring-2 focus:outline-none"
                />
              </div>

              <div className="flex gap-2 pt-2">
                <Button type="submit" disabled={applyMutation.isPending}>
                  {applyMutation.isPending ? (
                    <>
                      <Loader2 className="mr-2 size-4 animate-spin" />
                      Submitting...
                    </>
                  ) : (
                    "Submit Application"
                  )}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setShowApplicationForm(false)}
                >
                  Cancel
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      )}

      {/* Equity Pool Allocation */}
      {tokenConfig && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <PieChart className="size-5" />
              Token Pool Allocation
            </CardTitle>
            <CardDescription>
              How project tokens are distributed across holder classes
            </CardDescription>
          </CardHeader>
          <CardContent>
            {(() => {
              const total = Number(tokenConfig.totalSupply) || 1;
              const contributorSupply = Number(tokenConfig.contributorSupply);
              const investorSupply = Number(tokenConfig.investorSupply);
              const founderSupply = Number(tokenConfig.founderSupply);
              const burnedSupply = Number(tokenConfig.burnedSupply);
              const contributorPct = (
                (contributorSupply / total) *
                100
              ).toFixed(1);
              const investorPct = ((investorSupply / total) * 100).toFixed(1);
              const founderPct = ((founderSupply / total) * 100).toFixed(1);
              const burnedPct = ((burnedSupply / total) * 100).toFixed(1);
              const availableSupply =
                total -
                contributorSupply -
                investorSupply -
                founderSupply -
                burnedSupply;
              const availablePct = ((availableSupply / total) * 100).toFixed(1);

              return (
                <div className="space-y-4">
                  {/* Pool bars */}
                  <div className="space-y-3">
                    <div>
                      <div className="mb-1 flex justify-between text-sm">
                        <span className="font-medium">Contributor Pool</span>
                        <span className="text-muted-foreground">
                          {contributorPct}% (
                          {contributorSupply.toLocaleString()} tokens)
                        </span>
                      </div>
                      <div className="bg-muted h-2 overflow-hidden rounded-none">
                        <div
                          className="bg-neon-green h-full rounded-none transition-all"
                          style={{ width: `${contributorPct}%` }}
                        />
                      </div>
                    </div>
                    <div>
                      <div className="mb-1 flex justify-between text-sm">
                        <span className="font-medium">Investor Pool</span>
                        <span className="text-muted-foreground">
                          {investorPct}% ({investorSupply.toLocaleString()}{" "}
                          tokens)
                        </span>
                      </div>
                      <div className="bg-muted h-2 overflow-hidden rounded-none">
                        <div
                          className="bg-neon h-full rounded-none transition-all"
                          style={{ width: `${investorPct}%` }}
                        />
                      </div>
                    </div>
                    <div>
                      <div className="mb-1 flex justify-between text-sm">
                        <span className="font-medium">
                          Founder / Member Pool
                        </span>
                        <span className="text-muted-foreground">
                          {founderPct}% ({founderSupply.toLocaleString()}{" "}
                          tokens)
                        </span>
                      </div>
                      <div className="bg-muted h-2 overflow-hidden rounded-none">
                        <div
                          className="bg-neon-pink h-full rounded-none transition-all"
                          style={{ width: `${founderPct}%` }}
                        />
                      </div>
                    </div>
                    {burnedSupply > 0 && (
                      <div>
                        <div className="mb-1 flex justify-between text-sm">
                          <span className="text-muted-foreground font-medium">
                            Burned
                          </span>
                          <span className="text-muted-foreground">
                            {burnedPct}% ({burnedSupply.toLocaleString()}{" "}
                            tokens)
                          </span>
                        </div>
                        <div className="bg-muted h-2 overflow-hidden rounded-none">
                          <div
                            className="bg-destructive/50 h-full rounded-none transition-all"
                            style={{ width: `${burnedPct}%` }}
                          />
                        </div>
                      </div>
                    )}
                  </div>

                  {/* Summary */}
                  <div className="border-border flex items-center justify-between border-t pt-2 text-sm">
                    <span className="text-muted-foreground">
                      Total Supply:{" "}
                      <span className="text-foreground font-medium">
                        {total.toLocaleString()}
                      </span>{" "}
                      tokens
                    </span>
                    <span className="text-muted-foreground">
                      Unallocated:{" "}
                      <span className="text-foreground font-medium">
                        {availablePct}%
                      </span>{" "}
                      ({availableSupply.toLocaleString()})
                    </span>
                  </div>
                </div>
              );
            })()}
          </CardContent>
        </Card>
      )}

      {/* Pending Applications (Owner Only) */}
      {isOwner && (
        <Card>
          <CardHeader>
            <CardTitle>Pending Applications</CardTitle>
            <CardDescription>
              Review applications from people who want to join your project
            </CardDescription>
          </CardHeader>
          <CardContent>
            {applicationsLoading ? (
              <div className="flex items-center justify-center py-8">
                <Loader2 className="text-muted-foreground size-6 animate-spin" />
              </div>
            ) : pendingApplications.length === 0 ? (
              <div className="text-muted-foreground py-8 text-center">
                No pending applications
              </div>
            ) : (
              <div className="space-y-4">
                {pendingApplications.map((application) => (
                  <div
                    key={application.id}
                    className="border-border space-y-3 rounded border p-4"
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex items-center gap-3">
                        <Avatar>
                          <AvatarImage
                            src={application.user?.image ?? undefined}
                          />
                          <AvatarFallback>
                            {application.user?.name?.charAt(0).toUpperCase() ??
                              "U"}
                          </AvatarFallback>
                        </Avatar>
                        <div>
                          <div className="font-medium">
                            {application.user?.name ??
                              application.user?.email ??
                              "Unknown User"}
                          </div>
                          <div className="text-muted-foreground text-sm">
                            Applied for: {application.roleTitle}
                          </div>
                        </div>
                      </div>
                      <Badge variant="warning">Pending</Badge>
                    </div>

                    <div className="space-y-2">
                      <div>
                        <div className="text-muted-foreground mb-1 text-xs font-medium">
                          Message:
                        </div>
                        <div className="text-sm">{application.message}</div>
                      </div>

                      {application.skills && (
                        <div>
                          <div className="text-muted-foreground mb-1 text-xs font-medium">
                            Skills:
                          </div>
                          <div className="text-sm">{application.skills}</div>
                        </div>
                      )}

                      {application.experience && (
                        <div>
                          <div className="text-muted-foreground mb-1 text-xs font-medium">
                            Experience:
                          </div>
                          <div className="text-sm">
                            {application.experience}
                          </div>
                        </div>
                      )}

                      {application.availability && (
                        <div>
                          <div className="text-muted-foreground mb-1 text-xs font-medium">
                            Availability:
                          </div>
                          <div className="text-sm">
                            {application.availability}
                          </div>
                        </div>
                      )}
                    </div>

                    <div className="flex gap-2 pt-2">
                      <Button
                        size="sm"
                        variant="default"
                        onClick={() => handleReview(application.id, "ACCEPTED")}
                        disabled={reviewMutation.isPending}
                      >
                        <Check className="mr-2 size-4" />
                        Accept
                      </Button>
                      <Button
                        size="sm"
                        variant="destructive"
                        onClick={() => handleReview(application.id, "REJECTED")}
                        disabled={reviewMutation.isPending}
                      >
                        <X className="mr-2 size-4" />
                        Reject
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
