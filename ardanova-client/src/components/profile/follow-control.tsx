"use client";

import Link from "next/link";
import { useSession } from "next-auth/react";
import { UserCheck, UserPlus } from "lucide-react";
import { toast } from "sonner";

import { Button } from "~/components/ui/button";
import { buildSignInHref } from "~/lib/auth-navigation";
import { api } from "~/trpc/react";

/** Follower counts plus a Follow/Unfollow toggle for another member's profile. */
export function FollowControl({ userId }: { userId: string }) {
  const { data: session, status } = useSession();
  const viewerId = session?.user.id;
  const canFollow = status === "authenticated" && !!viewerId && viewerId !== userId;

  const utils = api.useUtils();
  const counts = api.user.getFollowCounts.useQuery({ userId }, { retry: false });
  const following = api.user.isFollowing.useQuery(
    { userId },
    { enabled: canFollow, retry: false },
  );
  const onSettled = async () => {
    await Promise.all([
      utils.user.isFollowing.invalidate({ userId }),
      utils.user.getFollowCounts.invalidate({ userId }),
    ]);
  };
  const follow = api.user.follow.useMutation({
    onSettled,
    onError: () => toast.error("Could not follow this member."),
  });
  const unfollow = api.user.unfollow.useMutation({
    onSettled,
    onError: () => toast.error("Could not unfollow this member."),
  });
  const isPending = follow.isPending || unfollow.isPending;

  return (
    <div className="flex flex-wrap items-center gap-4 text-sm">
      {counts.data && (
        <span className="text-muted-foreground">
          <strong className="text-foreground">{counts.data.followersCount}</strong>{" "}
          followers ·{" "}
          <strong className="text-foreground">{counts.data.followingCount}</strong>{" "}
          following
        </span>
      )}
      {status === "unauthenticated" && (
        <Button asChild variant="outline" size="sm">
          <Link href={buildSignInHref(`/dashboard/profile/${userId}`)}>
            Sign in to follow
          </Link>
        </Button>
      )}
      {canFollow && !following.isLoading && !following.error && (
        following.data ? (
          <Button
            variant="outline"
            size="sm"
            disabled={isPending}
            onClick={() => unfollow.mutate({ userId })}
          >
            <UserCheck className="mr-1.5 size-4" />
            Following
          </Button>
        ) : (
          <Button
            variant="neon"
            size="sm"
            disabled={isPending}
            onClick={() => follow.mutate({ userId })}
          >
            <UserPlus className="mr-1.5 size-4" />
            Follow
          </Button>
        )
      )}
    </div>
  );
}
