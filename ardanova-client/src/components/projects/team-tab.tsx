"use client";

import { useState } from "react";
import { api } from "~/trpc/react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "~/components/ui/accordion";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "~/components/ui/dropdown-menu";
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
import { Loader2, Users, Plus, Check, X, Shield, ChevronUp, UserPlus, Search } from "lucide-react";
import { toast } from "sonner";

interface TeamTabProps {
  projectId: string;
  isOwner: boolean;
}

export type MemberRole = "FOUNDER" | "LEADER" | "CORE_CONTRIBUTOR" | "CONTRIBUTOR" | "OBSERVER";
type ApplicationStatus = "PENDING" | "ACCEPTED" | "REJECTED" | "WITHDRAWN";

interface Member {
  id: string;
  userId: string;
  projectId: string;
  role: MemberRole;
  joinedAt: string;
  user?: {
    id: string;
    name?: string | null;
    email?: string | null;
    image?: string | null;
  };
}

interface Application {
  id: string;
  userId: string;
  projectId: string;
  roleTitle: string;
  message: string;
  skills?: string;
  experience?: string;
  availability?: string;
  status: ApplicationStatus;
  submittedAt: string;
  reviewedAt?: string;
  reviewMessage?: string;
  user?: {
    id: string;
    name?: string | null;
    email?: string | null;
    image?: string | null;
  };
}

export const getRoleBadgeVariant = (role: MemberRole | string) => {
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

export const formatRoleName = (role: MemberRole | string) => {
  return role
    .split("_")
    .map((word) => word.charAt(0) + word.slice(1).toLowerCase())
    .join(" ");
};

const TIERS = ["BRONZE", "SILVER", "GOLD", "PLATINUM", "DIAMOND"] as const;

const DEFAULT_ROLES = [
  { id: "FOUNDER", name: "Founder", description: "Project creator with full administrative access" },
  { id: "LEADER", name: "Leader", description: "Team lead with management responsibilities" },
  { id: "CORE_CONTRIBUTOR", name: "Core Contributor", description: "Key contributor with significant ongoing involvement" },
  { id: "CONTRIBUTOR", name: "Contributor", description: "Active contributor to the project" },
  { id: "OBSERVER", name: "Observer", description: "Following the project with limited participation" },
] as const;

// Invite Member Dialog Component
function InviteMemberDialog({
  projectId,
  role,
  existingMemberIds,
  isOwner,
}: {
  projectId: string;
  role: string;
  existingMemberIds: string[];
  isOwner: boolean;
}) {
  const [open, setOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [inviteMessage, setInviteMessage] = useState("");
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  const utils = api.useUtils();

  const { data: following, isLoading: followingLoading } = api.user.getFollowing.useQuery(
    { userId: "" }, // Will be overridden by the session
    { enabled: open && isOwner }
  );

  const createInvitationMutation = api.project.createInvitation.useMutation({
    onSuccess: () => {
      toast.success("Invitation sent successfully");
      setOpen(false);
      setSelectedUserId(null);
      setInviteMessage("");
      setSearchTerm("");
      void utils.project.getInvitations.invalidate({ projectId });
    },
    onError: (error) => {
      toast.error(error.message || "Failed to send invitation");
    },
  });

  // Filter out existing members and search
  const availableUsers = (following ?? []).filter((follow: any) => {
    const user = follow.following ?? follow;
    const userId = user.followingId ?? user.id;
    if (existingMemberIds.includes(userId)) return false;
    if (!searchTerm) return true;
    const name = (user.name ?? user.followingName ?? "").toLowerCase();
    return name.includes(searchTerm.toLowerCase());
  });

  const handleInvite = () => {
    if (!selectedUserId) return;
    createInvitationMutation.mutate({
      projectId,
      invitedUserId: selectedUserId,
      role: role as "FOUNDER" | "LEADER" | "CORE_CONTRIBUTOR" | "CONTRIBUTOR" | "OBSERVER",
      message: inviteMessage || undefined,
    });
  };

  if (!isOwner) return null;

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button variant="ghost" size="sm" className="h-7 px-2 text-xs">
          <UserPlus className="size-3.5 mr-1" />
          Invite
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Invite Member as {formatRoleName(role)}</DialogTitle>
          <DialogDescription>
            Select a user you follow to invite to this project
          </DialogDescription>
        </DialogHeader>
        <div className="space-y-4">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Search followed users..."
              className="w-full pl-9 pr-3 py-2 border border-border rounded bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary text-sm"
            />
          </div>

          <div className="max-h-48 overflow-y-auto space-y-1">
            {followingLoading ? (
              <div className="flex items-center justify-center py-4">
                <Loader2 className="size-4 animate-spin text-muted-foreground" />
              </div>
            ) : availableUsers.length === 0 ? (
              <div className="text-center py-4 text-sm text-muted-foreground">
                {searchTerm ? "No matching users found" : "No followed users available to invite"}
              </div>
            ) : (
              availableUsers.map((follow: any) => {
                const user = follow.following ?? follow;
                const userId = user.followingId ?? user.id;
                const userName = user.name ?? user.followingName ?? "Unknown";
                const userImage = user.image ?? user.followingImage;
                const isSelected = selectedUserId === userId;
                return (
                  <button
                    key={userId}
                    onClick={() => setSelectedUserId(isSelected ? null : userId)}
                    className={`w-full flex items-center gap-3 p-2 rounded transition-colors text-left ${
                      isSelected ? "bg-primary/10 border border-primary" : "hover:bg-muted/50 border border-transparent"
                    }`}
                  >
                    <Avatar className="size-8">
                      <AvatarImage src={userImage ?? undefined} />
                      <AvatarFallback className="text-xs">
                        {userName.charAt(0).toUpperCase()}
                      </AvatarFallback>
                    </Avatar>
                    <span className="text-sm font-medium">{userName}</span>
                    {isSelected && <Check className="size-4 ml-auto text-primary" />}
                  </button>
                );
              })
            )}
          </div>

          {selectedUserId && (
            <div>
              <label className="text-sm font-medium block mb-1.5">
                Message (optional)
              </label>
              <textarea
                value={inviteMessage}
                onChange={(e) => setInviteMessage(e.target.value)}
                placeholder="Add a personal message to the invitation..."
                className="w-full px-3 py-2 border border-border rounded bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary min-h-[60px] resize-y text-sm"
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
                <Loader2 className="size-4 mr-2 animate-spin" />
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

export default function TeamTab({ projectId, isOwner }: TeamTabProps) {
  const [showApplicationForm, setShowApplicationForm] = useState(false);
  const [roleTitle, setRoleTitle] = useState("");
  const [message, setMessage] = useState("");
  const [skills, setSkills] = useState("");
  const [experience, setExperience] = useState("");
  const [availability, setAvailability] = useState("");

  const utils = api.useUtils();

  // Queries
  const { data: members, isLoading: membersLoading } = api.project.getMembers.useQuery({
    projectId,
  });

  const { data: opportunities } = api.opportunity.getByProjectId.useQuery({
    projectId,
  });

  const { data: applications, isLoading: applicationsLoading } = api.project.getApplications.useQuery(
    { projectId },
    { enabled: isOwner }
  );

  const { data: projectCredentials } = api.membershipCredential.getByProjectId.useQuery({
    projectId,
  });

  const credentialsByUserId = new Map(
    (projectCredentials ?? []).map((c: any) => [c.userId, c]),
  );

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
        old?.map((app) => (app.id === applicationId ? { ...app, status } : app))
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
        <Loader2 className="size-6 animate-spin text-muted-foreground" />
      </div>
    );
  }

  const pendingApplications = applications?.filter((app) => app.status === "PENDING") ?? [];

  // Group opportunities by their linked projectRole
  const opportunitiesByRole = new Map<string, any[]>();
  const unlinkedOpportunities: any[] = [];
  (opportunities ?? []).forEach((opp: any) => {
    if (opp.projectRole) {
      const existing = opportunitiesByRole.get(opp.projectRole) ?? [];
      existing.push(opp);
      opportunitiesByRole.set(opp.projectRole, existing);
    } else {
      unlinkedOpportunities.push(opp);
    }
  });

  const existingMemberIds = (members ?? []).map((m: any) => m.userId);

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
              <CardDescription>Team positions, members, and open opportunities</CardDescription>
            </div>
            {!isOwner && (
              <Button
                onClick={() => setShowApplicationForm(!showApplicationForm)}
                variant="default"
                size="sm"
              >
                <Plus className="size-4 mr-2" />
                Apply to Join
              </Button>
            )}
          </div>
        </CardHeader>
        <CardContent>
          <Accordion type="multiple" defaultValue={["FOUNDER", "LEADER"]}>
            {DEFAULT_ROLES.map((role) => {
              const filledMembers = members?.filter((m: any) => m.role === role.id) ?? [];
              const linkedPositions = opportunitiesByRole.get(role.id) ?? [];

              return (
                <AccordionItem key={role.id} value={role.id}>
                  <AccordionTrigger className="hover:no-underline">
                    <div className="flex items-center gap-2">
                      <Badge variant={getRoleBadgeVariant(role.id as MemberRole)}>
                        {role.name}
                      </Badge>
                      <span className="text-xs text-muted-foreground">
                        ({filledMembers.length} {filledMembers.length === 1 ? "member" : "members"})
                      </span>
                    </div>
                  </AccordionTrigger>
                  <AccordionContent>
                    <div className="space-y-3 pt-1">
                      {/* Role description */}
                      <p className="text-sm text-muted-foreground">{role.description}</p>

                      {/* Members in this role */}
                      {filledMembers.length > 0 && (
                        <div className="space-y-2">
                          {filledMembers.map((member: any) => {
                            const credential = credentialsByUserId.get(member.userId);
                            const hasCredential = credential?.status === "ACTIVE";
                            return (
                              <div
                                key={member.id}
                                className="flex items-center justify-between p-3 border border-border rounded hover:bg-muted/50 transition-colors"
                              >
                                <div className="flex items-center gap-3">
                                  <Avatar className="size-8">
                                    <AvatarImage src={member.user?.image ?? undefined} />
                                    <AvatarFallback className="text-xs">
                                      {member.user?.name?.charAt(0).toUpperCase() ?? "U"}
                                    </AvatarFallback>
                                  </Avatar>
                                  <div>
                                    <div className="text-sm font-medium">
                                      {member.user?.name ?? member.user?.email ?? "Unknown User"}
                                    </div>
                                    <div className="text-xs text-muted-foreground">
                                      Joined {new Date(member.joinedAt).toLocaleDateString()}
                                    </div>
                                  </div>
                                </div>
                                <div className="flex items-center gap-2">
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
                                              className="h-6 w-6 p-0"
                                              title="Upgrade credential tier"
                                            >
                                              <ChevronUp className="size-3.5" />
                                            </Button>
                                          </DropdownMenuTrigger>
                                          <DropdownMenuContent align="end">
                                            {TIERS.map((tier) => (
                                              <DropdownMenuItem
                                                key={tier}
                                                disabled={credential?.tier === tier || updateTierMutation.isPending}
                                                onClick={() =>
                                                  updateTierMutation.mutate({
                                                    credentialId: credential.id,
                                                    tier,
                                                  })
                                                }
                                              >
                                                <span className={credential?.tier === tier ? "font-bold" : ""}>
                                                  {tier.charAt(0) + tier.slice(1).toLowerCase()}
                                                </span>
                                                {credential?.tier === tier && (
                                                  <Check className="size-3.5 ml-auto" />
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
                                      className="h-7 px-2 text-xs text-muted-foreground hover:text-primary"
                                      onClick={() => handleGrantCredential(member.userId)}
                                      disabled={grantCredentialMutation.isPending}
                                      title="Grant membership credential"
                                    >
                                      <Shield className="size-3.5 mr-1" />
                                      Grant
                                    </Button>
                                  )}
                                </div>
                              </div>
                            );
                          })}
                        </div>
                      )}

                      {/* Linked team positions */}
                      {linkedPositions.length > 0 && (
                        <div className="space-y-1.5">
                          <div className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
                            Positions
                          </div>
                          {linkedPositions.map((opp: any) => (
                            <div key={opp.id} className="flex items-center gap-2 text-xs text-muted-foreground pl-2">
                              <span className="w-1.5 h-1.5 rounded-full bg-neon shrink-0" />
                              <span className="font-medium text-foreground">{opp.title}</span>
                              {opp.applicationsCount > 0 && (
                                <span>
                                  ({opp.applicationsCount} {opp.applicationsCount === 1 ? "applicant" : "applicants"})
                                </span>
                              )}
                              <Badge
                                variant={opp.status === "FILLED" || opp.status === "CLOSED" ? "secondary" : "outline"}
                                className="text-[10px] h-4 px-1.5"
                              >
                                {opp.status === "FILLED" || opp.status === "CLOSED" ? "Filled" : "Open"}
                              </Badge>
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
                          />
                        </div>
                      )}

                      {/* Empty state */}
                      {filledMembers.length === 0 && linkedPositions.length === 0 && !isOwner && (
                        <div className="text-sm text-muted-foreground italic">
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
              <div className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
                Custom Positions
              </div>
              {unlinkedOpportunities.map((opp: any) => {
                const isFilled = opp.status === "FILLED" || opp.status === "CLOSED";
                return (
                  <div
                    key={opp.id}
                    className="flex items-center justify-between p-3 border border-border rounded hover:bg-muted/30 transition-colors"
                  >
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <Badge variant="neon">{opp.title}</Badge>
                        {opp.applicationsCount > 0 && (
                          <span className="text-xs text-muted-foreground">
                            ({opp.applicationsCount} {opp.applicationsCount === 1 ? "applicant" : "applicants"})
                          </span>
                        )}
                      </div>
                      {opp.description && (
                        <p className="text-sm text-muted-foreground mt-1 line-clamp-2">{opp.description}</p>
                      )}
                    </div>
                    <div className="ml-4">
                      <Badge variant={isFilled ? "secondary" : "outline"} className="text-xs">
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
      {showApplicationForm && !isOwner && (
        <Card>
          <CardHeader>
            <CardTitle>Apply to Join Project</CardTitle>
            <CardDescription>
              Tell the project owner why you'd like to join and what you can contribute
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleApply} className="space-y-4">
              <div>
                <label htmlFor="roleTitle" className="text-sm font-medium block mb-1.5">
                  Desired Role Title *
                </label>
                <input
                  id="roleTitle"
                  type="text"
                  value={roleTitle}
                  onChange={(e) => setRoleTitle(e.target.value)}
                  placeholder="e.g., Frontend Developer, Designer, Marketing Lead"
                  className="w-full px-3 py-2 border border-border rounded bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                  required
                />
              </div>

              <div>
                <label htmlFor="message" className="text-sm font-medium block mb-1.5">
                  Application Message * (min 20 characters)
                </label>
                <textarea
                  id="message"
                  value={message}
                  onChange={(e) => setMessage(e.target.value)}
                  placeholder="Explain why you want to join this project and what you can contribute..."
                  className="w-full px-3 py-2 border border-border rounded bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary min-h-[100px] resize-y"
                  required
                  minLength={20}
                />
              </div>

              <div>
                <label htmlFor="skills" className="text-sm font-medium block mb-1.5">
                  Skills (Optional)
                </label>
                <input
                  id="skills"
                  type="text"
                  value={skills}
                  onChange={(e) => setSkills(e.target.value)}
                  placeholder="e.g., React, TypeScript, UX Design"
                  className="w-full px-3 py-2 border border-border rounded bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                />
              </div>

              <div>
                <label htmlFor="experience" className="text-sm font-medium block mb-1.5">
                  Experience (Optional)
                </label>
                <textarea
                  id="experience"
                  value={experience}
                  onChange={(e) => setExperience(e.target.value)}
                  placeholder="Briefly describe your relevant experience..."
                  className="w-full px-3 py-2 border border-border rounded bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary min-h-[80px] resize-y"
                />
              </div>

              <div>
                <label htmlFor="availability" className="text-sm font-medium block mb-1.5">
                  Availability (Optional)
                </label>
                <input
                  id="availability"
                  type="text"
                  value={availability}
                  onChange={(e) => setAvailability(e.target.value)}
                  placeholder="e.g., 10 hours/week, Weekends, Full-time"
                  className="w-full px-3 py-2 border border-border rounded bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                />
              </div>

              <div className="flex gap-2 pt-2">
                <Button type="submit" disabled={applyMutation.isPending}>
                  {applyMutation.isPending ? (
                    <>
                      <Loader2 className="size-4 mr-2 animate-spin" />
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

      {/* Pending Applications (Owner Only) */}
      {isOwner && (
        <Card>
          <CardHeader>
            <CardTitle>Pending Applications</CardTitle>
            <CardDescription>Review applications from people who want to join your project</CardDescription>
          </CardHeader>
          <CardContent>
            {applicationsLoading ? (
              <div className="flex items-center justify-center py-8">
                <Loader2 className="size-6 animate-spin text-muted-foreground" />
              </div>
            ) : pendingApplications.length === 0 ? (
              <div className="text-center py-8 text-muted-foreground">
                No pending applications
              </div>
            ) : (
              <div className="space-y-4">
                {pendingApplications.map((application: any) => (
                  <div
                    key={application.id}
                    className="p-4 border border-border rounded space-y-3"
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex items-center gap-3">
                        <Avatar>
                          <AvatarImage src={application.user?.image ?? undefined} />
                          <AvatarFallback>
                            {application.user?.name?.charAt(0).toUpperCase() ?? "U"}
                          </AvatarFallback>
                        </Avatar>
                        <div>
                          <div className="font-medium">
                            {application.user?.name ?? application.user?.email ?? "Unknown User"}
                          </div>
                          <div className="text-sm text-muted-foreground">
                            Applied for: {application.roleTitle}
                          </div>
                        </div>
                      </div>
                      <Badge variant="warning">Pending</Badge>
                    </div>

                    <div className="space-y-2">
                      <div>
                        <div className="text-xs font-medium text-muted-foreground mb-1">
                          Message:
                        </div>
                        <div className="text-sm">{application.message}</div>
                      </div>

                      {application.skills && (
                        <div>
                          <div className="text-xs font-medium text-muted-foreground mb-1">
                            Skills:
                          </div>
                          <div className="text-sm">{application.skills}</div>
                        </div>
                      )}

                      {application.experience && (
                        <div>
                          <div className="text-xs font-medium text-muted-foreground mb-1">
                            Experience:
                          </div>
                          <div className="text-sm">{application.experience}</div>
                        </div>
                      )}

                      {application.availability && (
                        <div>
                          <div className="text-xs font-medium text-muted-foreground mb-1">
                            Availability:
                          </div>
                          <div className="text-sm">{application.availability}</div>
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
                        <Check className="size-4 mr-2" />
                        Accept
                      </Button>
                      <Button
                        size="sm"
                        variant="destructive"
                        onClick={() => handleReview(application.id, "REJECTED")}
                        disabled={reviewMutation.isPending}
                      >
                        <X className="size-4 mr-2" />
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
