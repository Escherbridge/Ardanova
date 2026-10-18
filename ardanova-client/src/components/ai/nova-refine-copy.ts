import type {
  NovaRefineResponse,
  NovaStatus,
} from "~/lib/api/ardanova/endpoints/nova";

/** What the sheet header and footer may truthfully say about refinement; see AGENTS.md §Mode-honest copy. */
export interface RefineDisclosure {
  subtitle: string;
  description: string;
  /** True only when ideas really go to the provider, so the credit never names one that answers nothing. */
  showsProviderCredit: boolean;
}

/** Header and footer copy for the Refine surface, keyed to what this deployment actually does with the idea. */
export function refineDisclosure(
  availability: NovaStatus["refinement"] | null | undefined,
  replyMode: NovaRefineResponse["mode"] | undefined,
  savedInTab: boolean,
): RefineDisclosure {
  const storage = savedInTab
    ? "kept in this tab, not in records"
    : "tab storage unavailable, a reload loses it";
  const mode = replyMode ?? availability;
  if (mode === "local-preview")
    return {
      subtitle: `Project refinement · local preview, nothing is sent · ${storage}`,
      description:
        "Nova AI is not connected here. Practise with fixed questions; nothing you write is analysed, and you retain every consequential decision.",
      showsProviderCredit: false,
    };
  if (mode === "off")
    return {
      subtitle: "Project refinement · not available on this deployment",
      description:
        "Nova AI project refinement is not available on this deployment, so nothing you type here is sent.",
      showsProviderCredit: false,
    };
  return {
    subtitle: `Project refinement · idea and answers go to Nova AI · ${storage}`,
    description:
      "Nova AI tests a project idea with hard questions and a feasibility verdict. It reads only what you type, not the page records, and you retain every consequential decision.",
    showsProviderCredit: mode === "live",
  };
}

/** The URL when it is an absolute http(s) link a member can safely open, else null (rendered as plain text). */
export function linkableSourceUrl(url: string): string | null {
  try {
    const parsed = new URL(url);
    return parsed.protocol === "https:" || parsed.protocol === "http:"
      ? parsed.toString()
      : null;
  } catch {
    return null;
  }
}
