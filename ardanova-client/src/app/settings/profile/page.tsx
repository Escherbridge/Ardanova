import type { Metadata } from "next";
import { UserRound } from "lucide-react";

import { ProfileEditor } from "~/components/profile/profile-editor";

export const metadata: Metadata = { title: "Profile settings" };

export default function ProfileSettingsPage() {
  return (
    <div className="container mx-auto max-w-3xl space-y-8 py-8">
      <div className="space-y-2 border-b-2 border-white/10 pb-6">
        <div className="flex items-center gap-3">
          <UserRound className="text-neon-cyan h-7 w-7" />
          <h1 className="font-mono text-2xl font-bold tracking-tight uppercase">
            Profile
          </h1>
        </div>
        <p className="text-muted-foreground text-sm">
          How other members see you: name, bio, links, skills and experience.
          Identity verification is a separate step under Verification.
        </p>
      </div>
      <ProfileEditor />
    </div>
  );
}
