"use client";

import { useState } from "react";
import { api } from "~/trpc/react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "~/components/ui/card";
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
import { Loader2, Users, Plus, UserMinus, Mail } from "lucide-react";

interface MembersTabProps {
  guildId: string;
  isOwner: boolean;
}

type MemberRole = "OWNER" | "ADMIN" | "MEMBER";

interface Member {
  id: string;
  userId: string;
  guildId: string;
  role: MemberRole;
  joinedAt: string;
  user?: {
    id: string;
    name?: string | null;
    email?: string | null;
    image?: string | null;
  };
}

const getRoleBadgeVariant = (role: MemberRole) => {
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

export function MembersTab({ guildId, isOwner }: MembersTabProps) {
  const [showInviteDialog, setShowInviteDialog] = useState(false);
  const [email, setEmail] = useState("");
  const [role, setRole] = useState<"MEMBER" | "ADMIN">("MEMBER");
  const [memberToRemove, setMemberToRemove] = useState<{
    id: string;
    name: string;
  } | null>(null);

  const utils = api.useUtils();

  // Query for fetching members
  const { data: members, isLoading } = api.guild.getMembers.useQuery({
    guildId,
  });

  // Mutation for sending invitation
  const inviteMemberMutation = api.guild.createInvitation.useMutation({
    onMutate: async (variables) => {
      if (!variables) return;
      const { invitedEmail, role } = variables;
      await utils.guild.getMembers.cancel({ guildId });
      const previous = utils.guild.getMembers.getData({ guildId });

      // Optimistically add a placeholder member (invitation pending)
      const optimisticMember: Member = {
        id: `temp-${Date.now()}`,
        userId: `pending-${Date.now()}`,
        guildId: guildId,
        role: role as MemberRole,
        joinedAt: new Date().toISOString(),
        user: {
          id: `pending-${Date.now()}`,
          name: null,
          email: invitedEmail ?? null,
          image: null,
        },
      };

      utils.guild.getMembers.setData({ guildId }, (old) =>
        old ? [...old, optimisticMember] : [optimisticMember]
      );

      return { previous };
    },
    onSuccess: () => {
      setShowInviteDialog(false);
      setEmail("");
      setRole("MEMBER");
      void utils.guild.getMembers.invalidate({ guildId });
    },
    onError: (err, _vars, context) => {
      if (context?.previous) {
        utils.guild.getMembers.setData({ guildId }, context.previous);
      }
      console.error("Failed to send invitation:", err.message);
    },
    onSettled: () => {
      void utils.guild.getMembers.invalidate({ guildId });
    },
  });

  // Mutation for removing member
  const removeMemberMutation = api.guild.removeMember.useMutation({
    onMutate: async (variables) => {
      if (!variables) return;
      const { memberId } = variables;
      await utils.guild.getMembers.cancel({ guildId });
      const previous = utils.guild.getMembers.getData({ guildId });
      utils.guild.getMembers.setData({ guildId }, (old) =>
        old?.filter((m) => m.id !== memberId)
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

    // Note: This assumes we have a way to get userId from email
    // In a real scenario, you'd need to implement a user search/lookup endpoint
    // For now, we're using email as a temporary placeholder for userId
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
        <Loader2 className="size-6 animate-spin text-muted-foreground" />
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
              <CardDescription>People who are part of this guild</CardDescription>
            </div>
            {isOwner && (
              <Button
                onClick={() => setShowInviteDialog(true)}
                variant="default"
                size="sm"
              >
                <Mail className="size-4 mr-2" />
                Invite Member
              </Button>
            )}
          </div>
        </CardHeader>
        <CardContent>
          {!members || members.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              No members yet.
            </div>
          ) : (
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
              {members.map((member: Member) => (
                <Card
                  key={member.id}
                  className="border border-border hover:bg-muted/50 transition-colors"
                >
                  <CardContent className="pt-6">
                    <div className="flex flex-col items-center text-center space-y-3">
                      <Avatar className="size-16">
                        <AvatarImage src={member.user?.image ?? undefined} />
                        <AvatarFallback className="text-lg">
                          {member.user?.name?.charAt(0).toUpperCase() ?? "U"}
                        </AvatarFallback>
                      </Avatar>
                      <div className="space-y-1">
                        <div className="font-medium">
                          {member.user?.name ?? member.user?.email ?? "Unknown User"}
                        </div>
                        <Badge variant={getRoleBadgeVariant(member.role)}>
                          {member.role}
                        </Badge>
                      </div>
                      <div className="text-xs text-muted-foreground">
                        Joined {new Date(member.joinedAt).toLocaleDateString()}
                      </div>
                      {isOwner && member.role !== "OWNER" && (
                        <Button
                          variant="outline"
                          size="sm"
                          className="w-full text-destructive hover:text-destructive"
                          onClick={() =>
                            setMemberToRemove({
                              id: member.id,
                              name:
                                member.user?.name ??
                                member.user?.email ??
                                "this member",
                            })
                          }
                          disabled={
                            removeMemberMutation.isPending &&
                            memberToRemove?.id === member.id
                          }
                        >
                          {removeMemberMutation.isPending &&
                          memberToRemove?.id === member.id ? (
                            <Loader2 className="size-4 mr-2 animate-spin" />
                          ) : (
                            <UserMinus className="size-4 mr-2" />
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

      {/* Invite Member Dialog */}
      <Dialog open={showInviteDialog} onOpenChange={setShowInviteDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Invite New Member</DialogTitle>
            <DialogDescription>
              Send an invitation to join your guild. They will receive an email
              notification.
            </DialogDescription>
          </DialogHeader>
          <form onSubmit={handleInviteMember}>
            <div className="space-y-4 py-4">
              <div>
                <label
                  htmlFor="email"
                  className="text-sm font-medium block mb-1.5"
                >
                  Email Address
                </label>
                <input
                  id="email"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="user@example.com"
                  className="w-full px-3 py-2 border border-border rounded bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                  required
                  disabled={inviteMemberMutation.isPending}
                />
              </div>

              <div>
                <label htmlFor="role" className="text-sm font-medium block mb-1.5">
                  Role
                </label>
                <Select
                  value={role}
                  onValueChange={(value) => setRole(value as "MEMBER" | "ADMIN")}
                  disabled={inviteMemberMutation.isPending}
                >
                  <SelectTrigger id="role" className="w-full">
                    <SelectValue placeholder="Select a role" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="MEMBER">
                      <div className="flex items-center gap-2">
                        <Badge variant="info">MEMBER</Badge>
                        <span className="text-xs text-muted-foreground">
                          Standard access
                        </span>
                      </div>
                    </SelectItem>
                    <SelectItem value="ADMIN">
                      <div className="flex items-center gap-2">
                        <Badge variant="neon-purple">ADMIN</Badge>
                        <span className="text-xs text-muted-foreground">
                          Management access
                        </span>
                      </div>
                    </SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => {
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
                    <Loader2 className="size-4 mr-2 animate-spin" />
                    Sending...
                  </>
                ) : (
                  <>
                    <Mail className="size-4 mr-2" />
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
              <span className="font-medium text-foreground">
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
                  <Loader2 className="size-4 mr-2 animate-spin" />
                  Removing...
                </>
              ) : (
                <>
                  <UserMinus className="size-4 mr-2" />
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
          <CardDescription>Understanding guild member roles and permissions</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            <div className="flex items-center gap-3 p-3 border border-border rounded">
              <Badge variant="neon-pink-solid">OWNER</Badge>
              <p className="text-sm text-muted-foreground">
                Full control over the guild, including adding/removing members and managing settings
              </p>
            </div>
            <div className="flex items-center gap-3 p-3 border border-border rounded">
              <Badge variant="neon-purple">ADMIN</Badge>
              <p className="text-sm text-muted-foreground">
                Can manage guild content and moderate members
              </p>
            </div>
            <div className="flex items-center gap-3 p-3 border border-border rounded">
              <Badge variant="info">MEMBER</Badge>
              <p className="text-sm text-muted-foreground">
                Standard guild member with basic access
              </p>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
