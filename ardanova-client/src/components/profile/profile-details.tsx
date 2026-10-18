import { Briefcase, Globe, Link2, MapPin, Sparkles } from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";

export interface ProfileDetailsData {
  location?: string | null;
  website?: string | null;
  linkedIn?: string | null;
  twitter?: string | null;
  skills: { id: string; skill: string; level: number }[];
  experience: {
    id: string;
    title: string;
    company: string;
    description?: string | null;
    startDate: string;
    endDate?: string | null;
    isCurrent: boolean;
  }[];
}

function formatMonth(value: string | null | undefined): string {
  if (!value) return "";
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? ""
    : date.toLocaleDateString("en-US", { month: "short", year: "numeric" });
}

/** Only http(s) links are rendered as anchors; anything else is shown as text. */
function safeExternalHref(value: string): string | null {
  try {
    const url = new URL(value);
    return url.protocol === "https:" || url.protocol === "http:" ? url.href : null;
  } catch {
    return null;
  }
}

/** Read-only skills, experience and links for a profile view. */
export function ProfileDetails({
  profile,
  emptySkillsMessage = "No skills listed yet.",
  emptyExperienceMessage = "No experience listed yet.",
}: {
  profile: ProfileDetailsData;
  emptySkillsMessage?: string;
  emptyExperienceMessage?: string;
}) {
  const links = [
    { label: "Website", value: profile.website, icon: Globe },
    { label: "LinkedIn", value: profile.linkedIn, icon: Link2 },
    { label: "Twitter / X", value: profile.twitter, icon: Link2 },
  ].filter((link): link is typeof link & { value: string } => !!link.value);

  return (
    <div className="grid gap-6 md:grid-cols-2">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Sparkles className="size-5" aria-hidden="true" />
            Skills
          </CardTitle>
        </CardHeader>
        <CardContent>
          {profile.skills.length === 0 ? (
            <p className="text-muted-foreground">{emptySkillsMessage}</p>
          ) : (
            <ul className="flex flex-wrap gap-2">
              {profile.skills.map((skill) => (
                <li
                  key={skill.id}
                  className="border-border border-2 px-2 py-1 font-mono text-xs"
                >
                  {skill.skill}
                  <span className="text-muted-foreground"> · {skill.level}/10</span>
                </li>
              ))}
            </ul>
          )}
          {(profile.location || links.length > 0) && (
            <ul className="text-muted-foreground mt-6 space-y-2 text-sm">
              {profile.location && (
                <li className="flex items-center gap-2">
                  <MapPin className="size-4" aria-hidden="true" />
                  {profile.location}
                </li>
              )}
              {links.map(({ label, value, icon: Icon }) => {
                const href = safeExternalHref(value);
                return (
                  <li key={label} className="flex min-w-0 items-center gap-2">
                    <Icon className="size-4 shrink-0" aria-hidden="true" />
                    <span className="sr-only">{label}:</span>
                    {href ? (
                      <a
                        href={href}
                        target="_blank"
                        rel="noopener noreferrer nofollow"
                        className="text-primary truncate hover:underline"
                      >
                        {value}
                      </a>
                    ) : (
                      <span className="truncate">{value}</span>
                    )}
                  </li>
                );
              })}
            </ul>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Briefcase className="size-5" aria-hidden="true" />
            Experience
          </CardTitle>
        </CardHeader>
        <CardContent>
          {profile.experience.length === 0 ? (
            <p className="text-muted-foreground">{emptyExperienceMessage}</p>
          ) : (
            <ul className="space-y-4">
              {profile.experience.map((entry) => (
                <li key={entry.id} className="border-border border-l-2 pl-3">
                  <p className="font-medium">
                    {entry.title} · {entry.company}
                  </p>
                  <p className="text-muted-foreground text-xs">
                    {formatMonth(entry.startDate)} –{" "}
                    {entry.isCurrent ? "present" : formatMonth(entry.endDate) || "—"}
                  </p>
                  {entry.description && (
                    <p className="text-muted-foreground mt-1 text-sm">
                      {entry.description}
                    </p>
                  )}
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
