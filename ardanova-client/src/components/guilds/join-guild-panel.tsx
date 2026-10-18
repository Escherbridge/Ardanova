"use client";

import { useState } from "react";
import { Bell, BellOff, Loader2, LogOut, Send, Undo2 } from "lucide-react";

import { api } from "~/trpc/react";
import { Button } from "~/components/ui/button";
import { Textarea } from "~/components/ui/textarea";
import { Input } from "~/components/ui/input";
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
import { describeMutationError } from "~/components/ui/query-error-copy";

/** Roles a visitor may ask for; ADMIN is appointed by the owner, never requested. */
const REQUESTABLE_ROLES = ["MEMBER", "APPRENTICE", "MANAGER"] as const;
type RequestableRole = (typeof REQUESTABLE_ROLES)[number];

/** The backend's minimum, so the form refuses what the API would refuse. */
const MINIMUM_MESSAGE_LENGTH = 20;

interface JoinGuildPanelProps {
  guildId: string;
  guildName: string;
  /** The viewer's membership row id, when they have one. */
  membershipId?: string;
  isOwner: boolean;
}

/** Follow, apply to join, withdraw a pending application, or leave. See components/guilds/AGENTS.md §Joining a guild. */
export function JoinGuildPanel({
  guildId,
  guildName,
  membershipId,
  isOwner,
}: JoinGuildPanelProps) {
  const utils = api.useUtils();
  const isMember = isOwner || !!membershipId;

  const { data: isFollowing } = api.guild.isFollowing.useQuery({ guildId });
  const { data: myApplications } = api.guild.getMyApplications.useQuery(
    { guildId },
    { enabled: !isMember },
  );
  const pendingApplication = myApplications?.find(
    (application) => application.status === "PENDING",
  );

  const [showApply, setShowApply] = useState(false);
  const [showLeave, setShowLeave] = useState(false);
  const [requestedRole, setRequestedRole] = useState<RequestableRole>("MEMBER");
  const [message, setMessage] = useState("");
  const [skills, setSkills] = useState("");

  const refreshFollow = () =>
    void utils.guild.isFollowing.invalidate({ guildId });
  const follow = api.guild.followGuild.useMutation({
    onSuccess: refreshFollow,
  });
  const unfollow = api.guild.unfollowGuild.useMutation({
    onSuccess: refreshFollow,
  });

  const refreshApplications = () =>
    void utils.guild.getMyApplications.invalidate({ guildId });
  const apply = api.guild.submitApplication.useMutation({
    onSuccess: () => {
      setShowApply(false);
      setMessage("");
      setSkills("");
      refreshApplications();
    },
  });
  const withdraw = api.guild.withdrawApplication.useMutation({
    onSuccess: refreshApplications,
  });

  const leave = api.guild.removeMember.useMutation({
    onSuccess: () => {
      setShowLeave(false);
      void utils.guild.getMembers.invalidate({ guildId });
    },
  });

  const messageTooShort = message.trim().length < MINIMUM_MESSAGE_LENGTH;
  const followPending = follow.isPending || unfollow.isPending;
  const followError = follow.error ?? unfollow.error;

  return (
    <div className="flex flex-col gap-2 sm:items-end">
      <div className="flex flex-wrap gap-2">
        <Button
          variant="outline"
          disabled={followPending}
          aria-pressed={!!isFollowing}
          onClick={() =>
            isFollowing
              ? unfollow.mutate({ guildId })
              : follow.mutate({ guildId })
          }
        >
          {followPending ? (
            <Loader2 className="mr-2 size-4 animate-spin" />
          ) : isFollowing ? (
            <BellOff className="mr-2 size-4" />
          ) : (
            <Bell className="mr-2 size-4" />
          )}
          {isFollowing ? "Unfollow" : "Follow"}
        </Button>

        {!isMember && !pendingApplication && (
          <Button onClick={() => setShowApply(true)}>
            <Send className="mr-2 size-4" />
            Apply to join
          </Button>
        )}

        {!isMember && pendingApplication && (
          <Button
            variant="outline"
            disabled={withdraw.isPending}
            onClick={() =>
              withdraw.mutate({
                guildId,
                applicationId: pendingApplication.id,
              })
            }
          >
            {withdraw.isPending ? (
              <Loader2 className="mr-2 size-4 animate-spin" />
            ) : (
              <Undo2 className="mr-2 size-4" />
            )}
            Withdraw application
          </Button>
        )}

        {!isOwner && membershipId && (
          <Button
            variant="outline"
            className="text-destructive hover:text-destructive"
            onClick={() => setShowLeave(true)}
          >
            <LogOut className="mr-2 size-4" />
            Leave guild
          </Button>
        )}
      </div>

      {!isMember && pendingApplication && (
        <p className="text-muted-foreground text-sm" role="status">
          Your application to join as {pendingApplication.requestedRole} is
          waiting for the guild&apos;s managers.
        </p>
      )}
      {followError && (
        <p role="alert" className="text-destructive text-sm">
          {describeMutationError(followError, "update your follow")}
        </p>
      )}
      {withdraw.error && (
        <p role="alert" className="text-destructive text-sm">
          {describeMutationError(withdraw.error, "withdraw your application")}
        </p>
      )}

      <Dialog open={showApply} onOpenChange={setShowApply}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Apply to join {guildName}</DialogTitle>
            <DialogDescription>
              The guild&apos;s owner or admins review applications. You become a
              member only when one of them accepts.
            </DialogDescription>
          </DialogHeader>
          <form
            onSubmit={(event) => {
              event.preventDefault();
              if (messageTooShort) return;
              apply.mutate({
                guildId,
                requestedRole,
                message: message.trim(),
                skills: skills.trim() || undefined,
              });
            }}
            className="space-y-4"
          >
            <div>
              <label
                htmlFor="guild-apply-role"
                className="mb-1.5 block text-sm font-medium"
              >
                Role you&apos;re asking for
              </label>
              <Select
                value={requestedRole}
                onValueChange={(value) =>
                  setRequestedRole(value as RequestableRole)
                }
                disabled={apply.isPending}
              >
                <SelectTrigger id="guild-apply-role" className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {REQUESTABLE_ROLES.map((role) => (
                    <SelectItem key={role} value={role}>
                      {role}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div>
              <label
                htmlFor="guild-apply-message"
                className="mb-1.5 block text-sm font-medium"
              >
                Why you want to join
              </label>
              <Textarea
                id="guild-apply-message"
                value={message}
                onChange={(event) => setMessage(event.target.value)}
                minLength={MINIMUM_MESSAGE_LENGTH}
                required
                disabled={apply.isPending}
                aria-describedby="guild-apply-message-hint"
              />
              <p
                id="guild-apply-message-hint"
                className="text-muted-foreground mt-1 text-xs"
              >
                At least {MINIMUM_MESSAGE_LENGTH} characters.
              </p>
            </div>
            <div>
              <label
                htmlFor="guild-apply-skills"
                className="mb-1.5 block text-sm font-medium"
              >
                Skills (optional)
              </label>
              <Input
                id="guild-apply-skills"
                value={skills}
                onChange={(event) => setSkills(event.target.value)}
                placeholder="e.g. roofing, permits, solar"
                disabled={apply.isPending}
              />
            </div>
            {apply.error && (
              <p role="alert" className="text-destructive text-sm">
                {describeMutationError(apply.error, "send your application")}
              </p>
            )}
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setShowApply(false)}
                disabled={apply.isPending}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                disabled={apply.isPending || messageTooShort}
              >
                {apply.isPending && (
                  <Loader2 className="mr-2 size-4 animate-spin" />
                )}
                Send application
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={showLeave} onOpenChange={setShowLeave}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Leave {guildName}?</DialogTitle>
            <DialogDescription>
              You will stop being a member. To come back you will need a new
              invitation or an accepted application.
            </DialogDescription>
          </DialogHeader>
          {leave.error && (
            <p role="alert" className="text-destructive text-sm">
              {describeMutationError(leave.error, "leave the guild")}
            </p>
          )}
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => setShowLeave(false)}
              disabled={leave.isPending}
            >
              Stay
            </Button>
            <Button
              type="button"
              variant="destructive"
              disabled={leave.isPending || !membershipId}
              onClick={() =>
                membershipId &&
                leave.mutate({ guildId, memberId: membershipId })
              }
            >
              {leave.isPending && (
                <Loader2 className="mr-2 size-4 animate-spin" />
              )}
              Leave guild
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
