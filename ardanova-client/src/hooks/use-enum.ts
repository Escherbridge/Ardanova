"use client";

import { api } from "~/trpc/react";

/**
 * Convert SCREAMING_SNAKE_CASE enum value to a human-readable label.
 * e.g. "SOCIAL_IMPACT" -> "Social Impact", "ARTS_CULTURE" -> "Arts & Culture"
 */
export function formatEnumLabel(value: string): string {
  return value
    .split("_")
    .map((word) => word.charAt(0) + word.slice(1).toLowerCase())
    .join(" ")
    .replace(/ And /g, " & ");
}

export interface EnumOption {
  id: string;
  label: string;
}

/**
 * Hook to fetch enum values from the backend and return them as { id, label } options.
 *
 * @param enumName - The PascalCase enum name (e.g. "ProjectCategory")
 * @param labelOverrides - Optional map of value -> custom label for cases where
 *                         the auto-formatted label isn't ideal
 */
export function useEnumOptions(
  enumName: string,
  labelOverrides?: Record<string, string>
) {
  const query = api.enum.getByName.useQuery(
    { name: enumName },
    { staleTime: Infinity, refetchOnWindowFocus: false }
  );

  const options: EnumOption[] =
    query.data?.map((value) => ({
      id: value,
      label: labelOverrides?.[value] ?? formatEnumLabel(value),
    })) ?? [];

  return {
    options,
    isLoading: query.isLoading,
    error: query.error,
    raw: query.data ?? [],
  };
}
