import type { ReactNode } from "react";

import { RelatedLinks } from "~/components/brand/related-links";
import { SimulationNotice } from "~/components/brand/simulation-notice";
import { patternBySlug, type PatternSlug } from "~/lib/writing";

function SourceList({
  items,
}: {
  items: readonly { href: string; label: string }[];
}) {
  return (
    <section className="space-y-2">
      <h2 className="text-xl">Sources</h2>
      <p>Every statement on this page is already published on this site.</p>
      <ul className="space-y-1">
        {items.map((source) => (
          <li key={source.href}>
            <a className="underline underline-offset-4" href={source.href}>
              {source.label}
            </a>
          </li>
        ))}
      </ul>
    </section>
  );
}

const bodies: Record<PatternSlug, () => ReactNode> = {
  "a-record-is-not-a-right": () => (
    <>
      <p>
        ArdaNova keeps a record of work, of agreed value, and of decisions. That
        record is useful when someone later asks what was already decided.
      </p>
      <p>
        It is not, by itself, a promise that anyone will pay you, and it does
        not transfer property. The terms of use, effective 10 November 2026, say
        the same thing.
      </p>
      <p>
        This page does not quote figures from outside organizations. None are
        cited here.
      </p>
      <SimulationNotice includeAzoa />
      <SourceList
        items={[
          { href: "https://ardanova.com/", label: "ArdaNova homepage" },
          {
            href: "https://ardanova.com/terms",
            label: "ArdaNova terms of use",
          },
          {
            href: "https://ardanova.com/notes/a-record-is-not-a-right",
            label: "Note: a record is not a right",
          },
        ]}
      />
    </>
  ),
  "two-records-one-agreement": () => (
    <>
      <p>
        Record A is the work performed, the agreed value, the allocation logic,
        and the reconciliation status, kept visible to the people affected.
      </p>
      <p>
        Record B is proposals, voting or approval rules, delegated roles, and
        recorded outcomes, kept distinct from the work record.
      </p>
      <p>
        The agreement is what binds them. A balance does not stand in for a
        signature.
      </p>
      <p>
        This page does not quote figures from outside organizations. None are
        cited here.
      </p>
      <SimulationNotice includeAzoa />
      <SourceList
        items={[
          { href: "https://ardanova.com/", label: "ArdaNova homepage" },
          {
            href: "https://ardanova.com/terms",
            label: "ArdaNova terms of use",
          },
          {
            href: "https://ardanova.com/notes/two-records-one-agreement",
            label: "Note: two records, one agreement",
          },
        ]}
      />
    </>
  ),
  "decision-logs": () => (
    <>
      <p>
        The workspace can record a proposal, the rule used to decide it, a
        delegated role, and the outcome.
      </p>
      <p>
        A recorded vote does not, by itself, move funds, change who is a member,
        or rewrite a project&apos;s terms. That sentence is already on the terms
        of use.
      </p>
      <p>
        This page does not quote figures from outside organizations. None are
        cited here.
      </p>
      <SimulationNotice includeAzoa />
      <SourceList
        items={[
          { href: "https://ardanova.com/", label: "ArdaNova homepage" },
          {
            href: "https://ardanova.com/terms",
            label: "ArdaNova terms of use",
          },
        ]}
      />
    </>
  ),
  "problem-intake": () => (
    <>
      <p>Discover a problem by starting with the people it affects.</p>
      <p>Define a solution as a plan others can check.</p>
      <p>Build, test, revise. Or stop.</p>
      <p>
        Describing a problem does not create a payment and does not transfer
        property.
      </p>
      <p>
        This page does not quote figures from outside organizations. None are
        cited here.
      </p>
      <SimulationNotice includeAzoa />
      <SourceList
        items={[{ href: "https://ardanova.com/", label: "ArdaNova homepage" }]}
      />
    </>
  ),
};

export function PatternBody({ slug }: { slug: PatternSlug }) {
  const pattern = patternBySlug(slug);
  const Body = bodies[slug];
  if (!pattern) return null;
  return (
    <>
      <Body />
      <RelatedLinks heading="Also in this section" items={pattern.related} />
    </>
  );
}
