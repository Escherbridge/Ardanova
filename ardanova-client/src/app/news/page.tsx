import Link from "next/link";

import { NotesShell } from "~/components/brand/notes-shell";
import { SimulationNotice } from "~/components/brand/simulation-notice";
import { publicPageMetadata } from "~/lib/page-metadata";

const description =
  "No public announcement is listed yet. The economy in this workspace is simulated.";

export const metadata = publicPageMetadata({
  path: "/news",
  title: "News",
  description,
  noindex: true,
});

export default function NewsPage() {
  return (
    <NotesShell
      title="News"
      trail={[
        { href: "/", label: "Home" },
        { href: "/news", label: "News" },
      ]}
    >
      <p>Nothing has been announced yet.</p>
      <SimulationNotice />
      <p>
        <Link className="underline underline-offset-4" href="/notes">
          Notes
        </Link>
        {" · "}
        <Link className="underline underline-offset-4" href="/patterns">
          Patterns
        </Link>
      </p>
    </NotesShell>
  );
}
