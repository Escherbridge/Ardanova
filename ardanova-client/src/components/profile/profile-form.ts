/** Editable profile fields as the form holds them. */
export interface ProfileFormValues {
  name: string;
  bio: string;
  location: string;
  phone: string;
  website: string;
  linkedIn: string;
  twitter: string;
}

export type ProfileFormSource = Partial<Record<keyof ProfileFormValues, string | null>>;

const fields: (keyof ProfileFormValues)[] = [
  "name",
  "bio",
  "location",
  "phone",
  "website",
  "linkedIn",
  "twitter",
];

/** Form state seeded from a stored profile; missing values become empty strings. */
export function profileFormFrom(profile: ProfileFormSource | undefined): ProfileFormValues {
  return Object.fromEntries(
    fields.map((field) => [field, profile?.[field] ?? ""]),
  ) as unknown as ProfileFormValues;
}

/** Three-way merge on refetch: an unsaved edit keeps its draft; untouched or now-saved fields take the server value. */
export function reseedProfileForm(
  draft: ProfileFormValues,
  previousSaved: ProfileFormValues,
  nextSaved: ProfileFormValues,
): ProfileFormValues {
  const stillPending = profileUpdateFrom(draft, nextSaved);
  return Object.fromEntries(
    fields.map((field) => [
      field,
      draft[field] !== previousSaved[field] && field in stillPending
        ? draft[field]
        : nextSaved[field],
    ]),
  ) as unknown as ProfileFormValues;
}

/** Prefixes `https://` onto a bare domain so `example.org` passes URL validation. */
export function normalizeWebsite(value: string): string {
  const trimmed = value.trim();
  if (!trimmed) return "";
  return /^[a-z][a-z0-9+.-]*:/i.test(trimmed) ? trimmed : `https://${trimmed}`;
}

/** Only the fields that changed, trimmed; an emptied field is sent as "" so it clears. Name is never cleared. */
export function profileUpdateFrom(
  values: ProfileFormValues,
  saved: ProfileFormValues,
): Partial<ProfileFormValues> {
  const next: ProfileFormValues = {
    ...values,
    ...Object.fromEntries(fields.map((field) => [field, values[field].trim()])),
    website: normalizeWebsite(values.website),
  };
  const update: Partial<ProfileFormValues> = {};
  for (const field of fields) {
    if (next[field] === saved[field].trim()) continue;
    if (field === "name" && next.name === "") continue;
    update[field] = next[field];
  }
  return update;
}
