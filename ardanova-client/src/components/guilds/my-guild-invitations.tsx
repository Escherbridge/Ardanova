"use client";

import Link from "next/link";
import { useSession } from "next-auth/react";
import { Loader2, Mail } from "lucide-react";
import { api } from "~/trpc/react";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";

/** Guild invitations waiting for the signed-in member; renders nothing when there are none. */
export function MyGuildInvitations() {
  const { status } = useSession();
  const utils = api.useUtils();
  const { data: invitations } = api.guild.getMyInvitations.useQuery(undefined, {
    enabled: status === "authenticated",
  });

  const respond = api.guild.respondToInvitation.useMutation({
    onSuccess: () => {
      void utils.guild.getMyInvitations.invalidate();
      void utils.guild.getMembers.invalidate();
    },
  });

  if (!invitations || invitations.length === 0) return null;

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="flex items-center gap-2 text-base">
          <Mail className="size-4" />
          Your invitations
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        {invitations.map((invitation) => {
          const isResponding =
            respond.isPending &&
            respond.variables?.invitationId === invitation.id;
          return (
            <div key={invitation.id} className="space-y-2 text-sm">
              <div className="flex items-center justify-between gap-2">
                {invitation.guildSlug ? (
                  <Link
                    href={`/guilds/${invitation.guildSlug}`}
                    className="font-medium hover:underline"
                  >
                    {invitation.guildName ?? "A guild"}
                  </Link>
                ) : (
                  <span className="font-medium">
                    {invitation.guildName ?? "A guild"}
                  </span>
                )}
                <Badge variant="info">{invitation.role}</Badge>
              </div>
              {invitation.message && (
                <p className="text-muted-foreground">{invitation.message}</p>
              )}
              <div className="flex gap-2">
                <Button
                  size="sm"
                  disabled={isResponding}
                  onClick={() =>
                    respond.mutate({
                      guildId: invitation.guildId,
                      invitationId: invitation.id,
                      accept: true,
                    })
                  }
                >
                  {isResponding && <Loader2 className="mr-2 size-4 animate-spin" />}
                  Accept
                </Button>
                <Button
                  size="sm"
                  variant="outline"
                  disabled={isResponding}
                  onClick={() =>
                    respond.mutate({
                      guildId: invitation.guildId,
                      invitationId: invitation.id,
                      accept: false,
                    })
                  }
                >
                  Decline
                </Button>
              </div>
            </div>
          );
        })}
        {respond.error && (
          <p role="alert" className="text-destructive text-sm">
            {respond.error.message}
          </p>
        )}
      </CardContent>
    </Card>
  );
}
