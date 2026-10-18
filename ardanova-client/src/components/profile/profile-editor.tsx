"use client";

import { useEffect, useState, type FormEvent } from "react";
import { Loader2, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Checkbox } from "~/components/ui/checkbox";
import { Input } from "~/components/ui/input";
import { Label } from "~/components/ui/label";
import { describeMutationError } from "~/components/ui/query-error-copy";
import { QueryErrorState } from "~/components/ui/query-error-state";
import { Textarea } from "~/components/ui/textarea";
import { api } from "~/trpc/react";

import {
  profileFormFrom,
  profileUpdateFrom,
  type ProfileFormValues,
} from "./profile-form";
import { ProfileIdentityBadges } from "./profile-identity-badges";

const textFields: {
  field: Exclude<keyof ProfileFormValues, "bio">;
  label: string;
  hint?: string;
  maxLength: number;
  type?: string;
}[] = [
  { field: "name", label: "Display name", maxLength: 100 },
  { field: "location", label: "Location", maxLength: 200 },
  { field: "website", label: "Website", maxLength: 500, type: "url" },
  { field: "linkedIn", label: "LinkedIn", maxLength: 500 },
  { field: "twitter", label: "Twitter / X", maxLength: 100 },
  {
    field: "phone",
    label: "Phone",
    hint: "Only you can see this; it is never shown on your public profile.",
    maxLength: 30,
    type: "tel",
  },
];

const emptyExperience = {
  title: "",
  company: "",
  description: "",
  startDate: "",
  endDate: "",
  isCurrent: false,
};

/** Self-service editor for the signed-in member's profile, skills and experience. */
export function ProfileEditor() {
  const utils = api.useUtils();
  const profile = api.profile.getMyProfile.useQuery(undefined, {
    retry: false,
  });
  const saved = profileFormFrom(profile.data);
  const [values, setValues] = useState<ProfileFormValues>(saved);
  const [skillName, setSkillName] = useState("");
  const [skillLevel, setSkillLevel] = useState("5");
  const [experience, setExperience] = useState(emptyExperience);

  useEffect(() => {
    if (profile.data) setValues(profileFormFrom(profile.data));
  }, [profile.data]);

  const refresh = async () => {
    await Promise.all([
      utils.profile.getMyProfile.invalidate(),
      utils.user.getById.invalidate(),
    ]);
  };

  const updateProfile = api.profile.updateProfile.useMutation({
    onSuccess: async () => {
      await refresh();
      toast.success("Profile saved.");
    },
    onError: (error) =>
      toast.error(describeMutationError(error, "save your profile")),
  });
  const addSkill = api.profile.addSkill.useMutation({
    onSuccess: async () => {
      setSkillName("");
      await refresh();
    },
    onError: (error) =>
      toast.error(describeMutationError(error, "add the skill")),
  });
  const removeSkill = api.profile.removeSkill.useMutation({
    onSuccess: refresh,
    onError: (error) =>
      toast.error(describeMutationError(error, "remove the skill")),
  });
  const addExperience = api.profile.addExperience.useMutation({
    onSuccess: async () => {
      setExperience(emptyExperience);
      await refresh();
    },
    onError: (error) =>
      toast.error(describeMutationError(error, "add the experience")),
  });
  const removeExperience = api.profile.removeExperience.useMutation({
    onSuccess: refresh,
    onError: (error) =>
      toast.error(describeMutationError(error, "remove the experience")),
  });

  if (profile.isLoading) {
    return <p className="text-muted-foreground">Loading your profile…</p>;
  }
  if (profile.error || !profile.data) {
    return (
      <QueryErrorState
        title="Your profile could not be loaded"
        error={profile.error}
        onRetry={() => void profile.refetch()}
      />
    );
  }

  const pendingUpdate = profileUpdateFrom(values, saved);
  const hasChanges = Object.keys(pendingUpdate).length > 0;

  const submitProfile = (event: FormEvent) => {
    event.preventDefault();
    if (hasChanges) updateProfile.mutate(pendingUpdate);
  };

  const submitSkill = (event: FormEvent) => {
    event.preventDefault();
    const skill = skillName.trim();
    if (!skill) return;
    addSkill.mutate({ skill, level: Number(skillLevel) });
  };

  const submitExperience = (event: FormEvent) => {
    event.preventDefault();
    if (
      !experience.title.trim() ||
      !experience.company.trim() ||
      !experience.startDate
    ) {
      return;
    }
    addExperience.mutate({
      title: experience.title.trim(),
      company: experience.company.trim(),
      description: experience.description.trim() || undefined,
      startDate: new Date(experience.startDate),
      endDate:
        !experience.isCurrent && experience.endDate
          ? new Date(experience.endDate)
          : undefined,
      isCurrent: experience.isCurrent,
    });
  };

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle className="flex flex-wrap items-center gap-3">
            Public profile
            <ProfileIdentityBadges
              userType={profile.data.userType}
              verificationLevel={profile.data.verificationLevel}
            />
          </CardTitle>
          <p className="text-muted-foreground text-sm">
            Everything here except phone is visible to anyone who opens your
            profile. Your email comes from your sign-in account and is never
            shown to other members.
          </p>
        </CardHeader>
        <CardContent>
          <form onSubmit={submitProfile} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="profile-bio">Bio</Label>
              <Textarea
                id="profile-bio"
                value={values.bio}
                maxLength={2000}
                rows={4}
                onChange={(event) =>
                  setValues({ ...values, bio: event.target.value })
                }
              />
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              {textFields.map(({ field, label, hint, maxLength, type }) => (
                <div key={field} className="space-y-2">
                  <Label htmlFor={`profile-${field}`}>{label}</Label>
                  <Input
                    id={`profile-${field}`}
                    type={type ?? "text"}
                    value={values[field]}
                    maxLength={maxLength}
                    required={field === "name" && saved.name !== ""}
                    onChange={(event) =>
                      setValues({ ...values, [field]: event.target.value })
                    }
                  />
                  {hint && (
                    <p className="text-muted-foreground text-xs">{hint}</p>
                  )}
                </div>
              ))}
            </div>
            <Button
              type="submit"
              disabled={!hasChanges || updateProfile.isPending}
            >
              {updateProfile.isPending && (
                <Loader2 className="mr-2 size-4 animate-spin" />
              )}
              Save profile
            </Button>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Skills</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {profile.data.skills.length === 0 ? (
            <p className="text-muted-foreground text-sm">
              Add the skills you want project leads to find you for.
            </p>
          ) : (
            <ul className="space-y-2">
              {profile.data.skills.map((skill) => (
                <li
                  key={skill.id}
                  className="border-border flex items-center justify-between border-2 px-3 py-2"
                >
                  <span>
                    {skill.skill}
                    <span className="text-muted-foreground">
                      {" "}
                      · {skill.level}/10
                    </span>
                  </span>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    aria-label={`Remove ${skill.skill}`}
                    disabled={removeSkill.isPending}
                    onClick={() => removeSkill.mutate({ skillId: skill.id })}
                  >
                    <Trash2 className="size-4" />
                  </Button>
                </li>
              ))}
            </ul>
          )}
          <form
            onSubmit={submitSkill}
            className="flex flex-col gap-3 sm:flex-row sm:items-end"
          >
            <div className="flex-1 space-y-2">
              <Label htmlFor="skill-name">Skill</Label>
              <Input
                id="skill-name"
                value={skillName}
                maxLength={100}
                onChange={(event) => setSkillName(event.target.value)}
              />
            </div>
            <div className="w-full space-y-2 sm:w-28">
              <Label htmlFor="skill-level">Level (1–10)</Label>
              <Input
                id="skill-level"
                type="number"
                min={1}
                max={10}
                value={skillLevel}
                onChange={(event) => setSkillLevel(event.target.value)}
              />
            </div>
            <Button
              type="submit"
              disabled={!skillName.trim() || addSkill.isPending}
            >
              <Plus className="mr-2 size-4" />
              Add skill
            </Button>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Experience</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {profile.data.experience.length > 0 && (
            <ul className="space-y-2">
              {profile.data.experience.map((entry) => (
                <li
                  key={entry.id}
                  className="border-border flex items-center justify-between gap-3 border-2 px-3 py-2"
                >
                  <span className="min-w-0 truncate">
                    {entry.title} · {entry.company}
                  </span>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    aria-label={`Remove ${entry.title} at ${entry.company}`}
                    disabled={removeExperience.isPending}
                    onClick={() =>
                      removeExperience.mutate({ experienceId: entry.id })
                    }
                  >
                    <Trash2 className="size-4" />
                  </Button>
                </li>
              ))}
            </ul>
          )}
          <form onSubmit={submitExperience} className="space-y-4">
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="experience-title">Role</Label>
                <Input
                  id="experience-title"
                  value={experience.title}
                  maxLength={200}
                  onChange={(event) =>
                    setExperience({ ...experience, title: event.target.value })
                  }
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="experience-company">Organization</Label>
                <Input
                  id="experience-company"
                  value={experience.company}
                  maxLength={200}
                  onChange={(event) =>
                    setExperience({
                      ...experience,
                      company: event.target.value,
                    })
                  }
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="experience-start">Started</Label>
                <Input
                  id="experience-start"
                  type="date"
                  value={experience.startDate}
                  onChange={(event) =>
                    setExperience({
                      ...experience,
                      startDate: event.target.value,
                    })
                  }
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="experience-end">Ended</Label>
                <Input
                  id="experience-end"
                  type="date"
                  value={experience.endDate}
                  disabled={experience.isCurrent}
                  onChange={(event) =>
                    setExperience({
                      ...experience,
                      endDate: event.target.value,
                    })
                  }
                />
              </div>
            </div>
            <div className="flex items-center gap-2">
              <Checkbox
                id="experience-current"
                checked={experience.isCurrent}
                onCheckedChange={(checked) =>
                  setExperience({ ...experience, isCurrent: checked === true })
                }
              />
              <Label htmlFor="experience-current">I still work here</Label>
            </div>
            <div className="space-y-2">
              <Label htmlFor="experience-description">What you did</Label>
              <Textarea
                id="experience-description"
                value={experience.description}
                maxLength={2000}
                rows={3}
                onChange={(event) =>
                  setExperience({
                    ...experience,
                    description: event.target.value,
                  })
                }
              />
            </div>
            <Button
              type="submit"
              disabled={
                !experience.title.trim() ||
                !experience.company.trim() ||
                !experience.startDate ||
                addExperience.isPending
              }
            >
              <Plus className="mr-2 size-4" />
              Add experience
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
