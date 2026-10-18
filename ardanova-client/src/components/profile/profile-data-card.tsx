"use client";

import { useState } from "react";
import { Download, Loader2 } from "lucide-react";
import { toast } from "sonner";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { describeMutationError } from "~/components/ui/query-error-copy";
import { api } from "~/trpc/react";

/** Lets the signed-in member download their own profile record; account closure is not self-service yet. */
export function ProfileDataCard() {
  const utils = api.useUtils();
  const [isExporting, setIsExporting] = useState(false);

  const downloadProfile = async () => {
    setIsExporting(true);
    try {
      const profile = await utils.profile.getMyProfile.fetch();
      const exportedAt = new Date().toISOString();
      const blob = new Blob(
        [JSON.stringify({ exportedAt, profile }, null, 2)],
        { type: "application/json" },
      );
      const href = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = href;
      link.download = `ardanova-profile-${exportedAt.slice(0, 10)}.json`;
      link.click();
      URL.revokeObjectURL(href);
    } catch (error) {
      toast.error(describeMutationError(error, "prepare your download"));
    } finally {
      setIsExporting(false);
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Your data</CardTitle>
        <p className="text-muted-foreground text-sm">
          Download your profile, skills and experience, including the private
          fields only you can see, as a JSON file.
        </p>
      </CardHeader>
      <CardContent className="space-y-4">
        <Button
          type="button"
          variant="outline"
          disabled={isExporting}
          onClick={() => void downloadProfile()}
        >
          {isExporting ? (
            <Loader2 className="mr-2 size-4 animate-spin" />
          ) : (
            <Download className="mr-2 size-4" />
          )}
          Download my profile data
        </Button>
        <p className="text-muted-foreground text-xs">
          Closing an account is not available yet. Projects, guilds and stakes
          you hold have to be handed over or settled first, and that closure
          process is still being built.
        </p>
      </CardContent>
    </Card>
  );
}
