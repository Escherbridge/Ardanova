import { Badge } from "~/components/ui/badge";

import { profileIdentityLabels } from "./profile-labels";

/** A member's real userType and verification level, never a placeholder. */
export function ProfileIdentityBadges({
  userType,
  verificationLevel,
}: {
  userType?: string | null;
  verificationLevel?: string | null;
}) {
  const labels = profileIdentityLabels({ userType, verificationLevel });
  return (
    <>
      <Badge variant="neon">{labels.userType}</Badge>
      {labels.verification && (
        <Badge variant="outline">{labels.verification}</Badge>
      )}
    </>
  );
}
