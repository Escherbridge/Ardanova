import Link from "next/link";

import { NotesShell } from "~/components/brand/notes-shell";
import { SimulationNotice } from "~/components/brand/simulation-notice";
import { publicPageMetadata } from "~/lib/page-metadata";

const description =
  "Release notes and writing dispatches from the ArdaNova engineering workspace.";

export const metadata = publicPageMetadata({
  path: "/news",
  title: "News",
  description,
  noindex: true,
});

export default function NewsPage() {
  return (
    <NotesShell
      kicker="Updates · ArdaNova Workspace"
      title="News"
      trail={[
        { href: "/", label: "Home" },
        { href: "/news", label: "News" },
      ]}
    >
      <p className="text-lg leading-relaxed">
        Product dispatches, pattern releases, and workspace notes are published
        directly in the engineering shelves below rather than as separate press
        releases.
      </p>
      <ul className="list-disc space-y-2 pl-5">
        <li>
          <Link className="underline underline-offset-4" href="/patterns">
            Architecture patterns (15 guides)
          </Link>{" "}
          — Multi-ledger reconciliation, append-only amendments, decision logs,
          and minutes hygiene.
        </li>
        <li>
          <Link className="underline underline-offset-4" href="/notes">
            Engineering notes
          </Link>{" "}
          — Longer essays on why shared work breaks between organizations and
          how Record A / Record B prevent drift.
        </li>
        <li>
          <Link className="underline underline-offset-4" href="/feed.xml">
            RSS feed
          </Link>{" "}
          — Subscribe for new pattern releases and field essays.
        </li>
      </ul>
      <SimulationNotice includeAzoa includeLegalAdvice />
    </NotesShell>
  );
}
