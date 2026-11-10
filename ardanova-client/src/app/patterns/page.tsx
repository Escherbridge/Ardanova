import Link from "next/link";

import { JsonLd } from "~/components/brand/json-ld";
import { NotesShell } from "~/components/brand/notes-shell";
import { itemList, breadcrumbList } from "~/lib/schema";
import { publicPageMetadata } from "~/lib/page-metadata";
import { patternCanonical, researchedPatterns } from "~/lib/writing";

const description =
  "Patterns of shared work: a record is not a right; two records, one agreement; decision logs; and the records a meeting has to keep. The economy is simulated.";

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
          researchedPatterns.map((pattern) => patternCanonical(pattern.slug)),
        )}
      />
      <NotesShell
        kicker="Eight patterns · the economy is simulated"
        title="The shape of a shared record."
        trail={[
          { href: "/", label: "Home" },
          { href: "/patterns", label: "Patterns" },
        ]}
      >
        <p>
          These pages are about recurring shapes of shared work: a register as
          evidence rather than title, two ledgers bound by an agreement, and a
          decision log that a later person can actually find. Five field notes
          sit with them, on the record itself: directors&apos; minutes, records
          of resolutions and general meetings, what belongs in the minutes, a
          record someone else can read, and a decision taken on an informal
          channel. The figures and legal sentences are other people&apos;s, with
          the links left on, and they are used as public contrast, not as a
          claim that ArdaNova is a UK company, a cooperative, or a cap-table
          product. The field notes are not legal advice.
        </p>
        <p>
          <strong className="font-mono">ECONOMY: SIMULATED.</strong> Platform
          records do not automatically create legal ownership, equity, or
          payment rights. That sentence belongs on this index and on every
          child, not only in the footer.
        </p>
        <section className="space-y-6">
          <h2 className="text-xl">The patterns</h2>
          <ul className="grid gap-6">
            {researchedPatterns.map((pattern) => (
              <li className="border border-black/70 p-6" key={pattern.slug}>
                <h3 className="text-2xl">
                  <Link
                    className="underline-offset-4 hover:underline"
                    href={`/patterns/${pattern.slug}`}
                  >
                    {pattern.title}
                  </Link>
                </h3>
                {"indexBlurb" in pattern && pattern.indexBlurb ? (
                  <p className="mt-3">{pattern.indexBlurb}</p>
                ) : null}
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
        </section>
        <section className="space-y-3">
          <h2 className="text-xl">What ArdaNova is doing here</h2>
          <p>
            ArdaNova is a project-and-work workspace with a simulated economy.
            Related house notes are{" "}
            <Link
              className="underline underline-offset-4"
              href="/notes/a-record-is-not-a-right"
            >
              A record is not a right
            </Link>{" "}
            and{" "}
            <Link
              className="underline underline-offset-4"
              href="/notes/two-records-one-agreement"
            >
              Two records. One agreement.
            </Link>
            .{" "}
            <a
              className="underline underline-offset-4"
              href="https://azoa.ardanova.com"
            >
              AZOA
            </a>
            , the planned workflow engine, is also still a simulation. This
            workspace belongs to the stack at{" "}
            <a
              className="underline underline-offset-4"
              href="https://primusneo.com"
            >
              PrimusNeo
            </a>
            . Start from the{" "}
            <a
              className="underline underline-offset-4"
              href="https://ardanova.com/"
            >
              homepage
            </a>{" "}
            and the{" "}
            <Link className="underline underline-offset-4" href="/terms">
              Terms
            </Link>
            . Site notes, when there is something true to announce, live in{" "}
            <Link className="underline underline-offset-4" href="/news">
              News
            </Link>
            .
          </p>
        </section>
        <p>Author: Escherbridge. Last reviewed: 2026-11-10, America/Denver.</p>
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
