"use client";

import { useState } from "react";
import { api } from "~/trpc/react";
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
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "~/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "~/components/ui/select";
import { CredentialBadge } from "~/components/credentials/credential-badge";
import { describeMutationError } from "~/components/ui/query-error-copy";
import { Loader2, Users, UserMinus, Mail } from "lucide-react";
import { ApplicationsPanel } from "./applications-panel";

interface MembersTabProps {
  guildId: string;
  /** Owner of record; only the owner appoints, demotes or removes admins. */
  isOwner: boolean;
  /** Owner or ADMIN: the people the API lets invite, remove, change roles and review applications. */
  canManage: boolean;
}

/** Roles a manager can set on an existing member; ADMIN is offered to the owner only. */
const ASSIGNABLE_ROLES = ["ADMIN", "MANAGER", "MEMBER", "APPRENTICE"] as const;
type AssignableRole = (typeof ASSIGNABLE_ROLES)[number];

type MemberRole = "OWNER" | "ADMIN" | "MEMBER";

const getRoleBadgeVariant = (role: string) => {
  switch (role) {
    case "OWNER":
      return "neon-pink-solid" as const;
    case "ADMIN":
      return "neon-purple" as const;
    case "MEMBER":
      return "info" as const;
    default:
      return "default" as const;
  }
};

export function MembersTab({ guildId, isOwner, canManage }: MembersTabProps) {
  const [showInviteDialog, setShowInviteDialog] = useState(false);
  const [email, setEmail] = useState("");
  const [role, setRole] = useState<"MEMBER" | "ADMIN">("MEMBER");
  const [inviteSentTo, setInviteSentTo] = useState<string | null>(null);
  const [memberToRemove, setMemberToRemove] = useState<{
    id: string;
    name: string;
  } | null>(null);

  const utils = api.useUtils();

  // Query for fetching members
  const { data: members, isLoading } = api.guild.getMembers.useQuery({
    guildId,
  });

  const { data: guildCredentials } =
    api.membershipCredential.getByGuildId.useQuery({
      guildId,
    });

  const credentialsByUserId = new Map(
    (guildCredentials ?? []).map((c) => [c.userId, c]),
  );

  // Pending invitations are shown apart from members: an invitee is not a member until they accept.
  const { data: invitations } = api.guild.getInvitations.useQuery(
    { guildId },
    { enabled: canManage },
  );
  const pendingInvitations = (invitations ?? []).filter(
    (invitation) => invitation.status === "PENDING",
  );

  const inviteMemberMutation = api.guild.createInvitation.useMutation({
    onSuccess: (invitation) => {
      setInviteSentTo(invitation.invitedEmail ?? null);
      setShowInviteDialog(false);
      setEmail("");
      setRole("MEMBER");
      void utils.guild.getInvitations.invalidate({ guildId });
    },
  });

  const updateRoleMutation = api.guild.updateMember.useMutation({
    onSettled: () => {
      void utils.guild.getMembers.invalidate({ guildId });
    },
  });

  /** Mirrors the API: the owner row never changes here, and only the owner touches ADMIN. */
  const canChangeMember = (memberRole: string | null | undefined) =>
    canManage && memberRole !== "OWNER" && (memberRole !== "ADMIN" || isOwner);

  // Mutation for removing member
  const removeMemberMutation = api.guild.removeMember.useMutation({
    onMutate: async (variables) => {
      if (!variables) return;
      const { memberId } = variables;
      await utils.guild.getMembers.cancel({ guildId });
      const previous = utils.guild.getMembers.getData({ guildId });
      utils.guild.getMembers.setData({ guildId }, (old) =>
        old?.filter((m) => m.id !== memberId),
      );
      return { previous };
    },
    onError: (err, _vars, context) => {
      if (context?.previous) {
        utils.guild.getMembers.setData({ guildId }, context.previous);
      }
      console.error("Failed to remove member:", err.message);
    },
    onSettled: () => {
      void utils.guild.getMembers.invalidate({ guildId });
    },
  });

  const handleInviteMember = (e: React.FormEvent) => {
    e.preventDefault();
    if (!email.trim()) return;

    // The API answers the same whether or not an account uses this email (Services/AGENTS.md §Email invitations).
    inviteMemberMutation.mutate({
      guildId,
      invitedEmail: email.trim(),
      role: role,
      message: `You have been invited to join as a ${role}`,
    });
  };

  const handleRemoveMember = (memberId: string) => {
    removeMemberMutation.mutate({ guildId, memberId });
    setMemberToRemove(null);
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="text-muted-foreground size-6 animate-spin" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Team Members */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <Users className="size-5" />
                Guild Members
              </CardTitle>
              <CardDescription>
                People who are part of this guild
              </CardDescription>
            </div>
            {canManage && (
              <Button
                onClick={() => setShowInviteDialog(true)}
                variant="default"
                size="sm"
              >
                <Mail className="mr-2 size-4" />
                Invite Member
              </Button>
            )}
          </div>
        </CardHeader>
        <CardContent>
          {inviteSentTo && (
            <p role="status" className="text-muted-foreground mb-4 text-sm">
              Invitation recorded for {inviteSentTo}. If an ArdaNova account
              uses that email, its owner will find it under their guild
              invitations.
            </p>
          )}
          {updateRoleMutation.error && (
            <p role="alert" className="text-destructive mb-4 text-sm">
              {describeMutationError(
                updateRoleMutation.error,
                "change that role",
              )}
            </p>
          )}
          {!members || members.length === 0 ? (
            <div className="text-muted-foreground py-8 text-center">
              No members yet.
            </div>
          ) : (
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
              {members.map((member) => (
                <Card
                  key={member.id}
                  className="border-border hover:bg-muted/50 border transition-colors"
                >
                  <CardContent className="pt-6">
                    <div className="flex flex-col items-center space-y-3 text-center">
                      <Avatar className="size-16">
                        <AvatarImage src={member.user?.image ?? undefined} />
                        <AvatarFallback className="text-lg">
                          {member.user?.name?.charAt(0).toUpperCase() ?? "U"}
                        </AvatarFallback>
                      </Avatar>
                      <div className="space-y-1">
                        <div className="font-medium">
                          {member.user?.name ?? "Unknown User"}
                        </div>
                        <div className="flex items-center justify-center gap-1.5">
                          <Badge
                            variant={getRoleBadgeVariant(
                              (member.role ?? "MEMBER") as MemberRole,
                            )}
                          >
                            {member.role ?? "MEMBER"}
                          </Badge>
                          {credentialsByUserId.get(member.userId)?.status ===
                            "ACTIVE" && (
                            <CredentialBadge
                              tier={
                                credentialsByUserId.get(member.userId)?.tier
                              }
                              size="sm"
                            />
                          )}
                        </div>
                      </div>
                      <div className="text-muted-foreground text-xs">
                        Joined {new Date(member.joinedAt).toLocaleDateString()}
                      </div>
                      {canChangeMember(member.role) && (
                        <Select
                          value={member.role ?? "MEMBER"}
                          onValueChange={(value) =>
                            updateRoleMutation.mutate({
                              guildId,
                              memberId: member.id,
                              role: value as AssignableRole,
                            })
                          }
                          disabled={
                            updateRoleMutation.isPending &&
                            updateRoleMutation.variables?.memberId === member.id
                          }
                        >
                          <SelectTrigger
                            className="w-full"
                            aria-label={`Role for ${member.user?.name ?? "this member"}`}
                          >
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            {ASSIGNABLE_ROLES.filter(
                              (option) => option !== "ADMIN" || isOwner,
                            ).map((option) => (
                              <SelectItem key={option} value={option}>
                                {option}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      )}
                      {canChangeMember(member.role) && (
                        <Button
                          variant="outline"
                          size="sm"
                          className="text-destructive hover:text-destructive w-full"
                          onClick={() =>
                            setMemberToRemove({
                              id: member.id,
                              name: member.user?.name ?? "this member",
                            })
                          }
                          disabled={
                            removeMemberMutation.isPending &&
                            memberToRemove?.id === member.id
                          }
                        >
                          {removeMemberMutation.isPending &&
                          memberToRemove?.id === member.id ? (
                            <Loader2 className="mr-2 size-4 animate-spin" />
                          ) : (
                            <UserMinus className="mr-2 size-4" />
                          )}
                          Remove
                        </Button>
                      )}
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {canManage && <ApplicationsPanel guildId={guildId} isOwner={isOwner} />}

      {canManage && pendingInvitations.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Mail className="size-5" />
              Pending Invitations
            </CardTitle>
            <CardDescription>
              Sent, but not yet accepted. These people are not members yet.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <ul className="space-y-2">
              {pendingInvitations.map((invitation) => (
                <li
                  key={invitation.id}
                  className="border-border flex items-center justify-between rounded border p-3 text-sm"
                >
                  <span>{invitation.invitedEmail ?? "Invited member"}</span>
                  <Badge variant={getRoleBadgeVariant(invitation.role)}>
                    {invitation.role}
                  </Badge>
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>
      )}

      {/* Invite Member Dialog */}
      <Dialog open={showInviteDialog} onOpenChange={setShowInviteDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Invite New Member</DialogTitle>
            <DialogDescription>
              Invite someone by the email on their ArdaNova account. They join
              only when they accept it from their guild invitations.
            </DialogDescription>
          </DialogHeader>
          <form onSubmit={handleInviteMember}>
            <div className="space-y-4 py-4">
              <div>
                <label
                  htmlFor="email"
                  className="mb-1.5 block text-sm font-medium"
                >
                  Email Address
                </label>
                <input
                  id="email"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="user@example.com"
                  className="border-border bg-background text-foreground focus:ring-primary w-full rounded border px-3 py-2 focus:ring-2 focus:outline-none"
                  required
                  disabled={inviteMemberMutation.isPending}
                />
              </div>

              <div>
                <label
                  htmlFor="role"
                  className="mb-1.5 block text-sm font-medium"
                >
                  Role
                </label>
                <Select
                  value={role}
                  onValueChange={(value) =>
                    setRole(value as "MEMBER" | "ADMIN")
                  }
                  disabled={inviteMemberMutation.isPending}
                >
                  <SelectTrigger id="role" className="w-full">
                    <SelectValue placeholder="Select a role" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="MEMBER">
                      <div className="flex items-center gap-2">
                        <Badge variant="info">MEMBER</Badge>
                        <span className="text-muted-foreground text-xs">
                          Standard access
                        </span>
                      </div>
                    </SelectItem>
                    {isOwner && (
                      <SelectItem value="ADMIN">
                        <div className="flex items-center gap-2">
                          <Badge variant="neon-purple">ADMIN</Badge>
                          <span className="text-muted-foreground text-xs">
                            Management access
                          </span>
                        </div>
                      </SelectItem>
                    )}
                  </SelectContent>
                </Select>
              </div>
              {inviteMemberMutation.error && (
                <p role="alert" className="text-destructive text-sm">
                  {inviteMemberMutation.error.message}
                </p>
              )}
            </div>
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => {
                  inviteMemberMutation.reset();
                  setShowInviteDialog(false);
                  setEmail("");
                  setRole("MEMBER");
                }}
                disabled={inviteMemberMutation.isPending}
              >
                Cancel
              </Button>
              <Button type="submit" disabled={inviteMemberMutation.isPending}>
                {inviteMemberMutation.isPending ? (
                  <>
                    <Loader2 className="mr-2 size-4 animate-spin" />
                    Sending...
                  </>
                ) : (
                  <>
                    <Mail className="mr-2 size-4" />
                    Send Invitation
                  </>
                )}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Remove Member Confirmation Dialog */}
      <Dialog
        open={!!memberToRemove}
        onOpenChange={(open) => !open && setMemberToRemove(null)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Remove Member</DialogTitle>
            <DialogDescription>
              Are you sure you want to remove{" "}
              <span className="text-foreground font-medium">
                {memberToRemove?.name}
              </span>{" "}
              from the guild? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => setMemberToRemove(null)}
              disabled={removeMemberMutation.isPending}
            >
              Cancel
            </Button>
            <Button
              type="button"
              variant="destructive"
              onClick={() =>
                memberToRemove && handleRemoveMember(memberToRemove.id)
              }
              disabled={removeMemberMutation.isPending}
            >
              {removeMemberMutation.isPending ? (
                <>
                  <Loader2 className="mr-2 size-4 animate-spin" />
                  Removing...
                </>
              ) : (
                <>
                  <UserMinus className="mr-2 size-4" />
                  Remove Member
                </>
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Member Roles Info */}
      <Card>
        <CardHeader>
          <CardTitle>Member Roles</CardTitle>
          <CardDescription>
            Understanding guild member roles and permissions
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            <div className="border-border flex items-center gap-3 rounded border p-3">
              <Badge variant="neon-pink-solid">OWNER</Badge>
              <p className="text-muted-foreground text-sm">
                Full control over the guild, including adding/removing members
                and managing settings
              </p>
            </div>
            <div className="border-border flex items-center gap-3 rounded border p-3">
              <Badge variant="neon-purple">ADMIN</Badge>
              <p className="text-muted-foreground text-sm">
                Can edit the guild, post updates, invite, review applications
                and manage members (but not other admins)
              </p>
            </div>
            <div className="border-border flex items-center gap-3 rounded border p-3">
              <Badge variant="info">MEMBER</Badge>
              <p className="text-muted-foreground text-sm">
                Standard guild member with basic access
              </p>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
