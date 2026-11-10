import Link from "next/link";

import { JsonLd } from "~/components/brand/json-ld";
import { NotesShell } from "~/components/brand/notes-shell";
import { SimulationNotice } from "~/components/brand/simulation-notice";
import { itemList, breadcrumbList } from "~/lib/schema";
import { publicPageMetadata } from "~/lib/page-metadata";
import { patternCanonical, patterns } from "~/lib/writing";

const description =
  "How this workspace keeps a record of shared work. The economy is simulated. These pages quote this site, not outside figures.";

export const metadata = publicPageMetadata({
  path: "/patterns",
  title: "Patterns",
  description,
});

export default function PatternsPage() {
  return (
    <>
      <JsonLd
        data={breadcrumbList([
          { name: "Home", path: "/" },
          { name: "Patterns", path: "/patterns" },
        ])}
      />
      <JsonLd
        data={itemList(
          "Patterns",
          patterns.map((pattern) => patternCanonical(pattern.slug)),
        )}
      />
      <NotesShell
        kicker="Four patterns · one simulated workspace"
        title="The shape of a shared record."
        trail={[
          { href: "/", label: "Home" },
          { href: "/patterns", label: "Patterns" },
        ]}
      >
        <p>
          These pages say what this workspace records, and what a record does
          not do. No outside figure is quoted, because none is cited here.
        </p>
        <p>
          <a
            className="underline underline-offset-4"
            href="https://primusneo.com"
          >
            PrimusNeo, the lab whose stack this workspace belongs to
          </a>
          .
        </p>
        <SimulationNotice includeAzoa />
        <ul className="grid gap-6">
          {patterns.map((pattern) => (
            <li className="border border-black/70 p-6" key={pattern.slug}>
              <h2 className="text-2xl">
                <Link
                  className="underline-offset-4 hover:underline"
                  href={`/patterns/${pattern.slug}`}
                >
                  {pattern.title}
                </Link>
              </h2>
              <p className="text-muted-foreground mt-3">
                {pattern.description}
              </p>
              <p className="mt-4">
                <Link
                  className="inline-flex min-h-11 items-center font-semibold underline-offset-4 hover:underline"
                  href={`/patterns/${pattern.slug}`}
                >
                  View
                </Link>
              </p>
            </li>
          ))}
        </ul>
        <p>
          <Link className="underline underline-offset-4" href="/notes">
            Notes
          </Link>
          {" · "}
          <Link className="underline underline-offset-4" href="/news">
            News
          </Link>
          {" · "}
          <Link className="underline underline-offset-4" href="/feed.xml">
            Feed
          </Link>
        </p>
      </NotesShell>
    </>
  );
}
