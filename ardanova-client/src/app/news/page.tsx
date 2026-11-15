import Link from "next/link";

import { NotesShell } from "~/components/brand/notes-shell";
import { SimulationNotice } from "~/components/brand/simulation-notice";
import { publicPageMetadata } from "~/lib/page-metadata";

const description =
  "Dispatches and writing updates from ArdaNova on solutionary projects, social ecology, and shared ownership.";

export const metadata = publicPageMetadata({
  path: "/news",
  title: "News",
  description,
  noindex: true,
});

export default function NewsPage() {
  return (
    <NotesShell
      kicker="Updates · Solutionary Commons"
      title="News"
      trail={[
        { href: "/", label: "Home" },
        { href: "/news", label: "News" },
      ]}
    >
      <p className="text-lg leading-relaxed">
        Essays on cooperative ownership, solutionary thinking, and social
        ecology are published directly on the shelves below rather than wrapped
        in corporate press releases.
      </p>
      <ul className="list-disc space-y-2 pl-5">
        <li>
          <Link className="underline underline-offset-4" href="/notes">
            Essays &amp; field notes
          </Link>{" "}
          — Long-form writing on why communities stall in outrage feeds, how
          social ecology informs shared guilds, and why ownership never requires
          surveillance.
        </li>
        <li>
          <Link className="underline underline-offset-4" href="/patterns">
            Stewardship &amp; governance patterns (15 guides)
          </Link>{" "}
          — Practical guides to keeping shared value (Record A) apart from
          community voice (Record B), preserving open decisions, and writing
          honest agreements.
        </li>
        <li>
          <Link className="underline underline-offset-4" href="/feed.xml">
            RSS feed
          </Link>{" "}
          — Subscribe directly for new essays and pattern updates.
        </li>
      </ul>
      <SimulationNotice includeAzoa includeLegalAdvice />
    </NotesShell>
  );
}
