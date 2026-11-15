import type { ReactNode } from "react";
import Link from "next/link";

import { type NoteSlug } from "~/lib/writing";

export { noteBySlug, noteCanonical, notes } from "~/lib/writing";

function FollowLink({ href, children }: { href: string; children: string }) {
  if (href.startsWith("/")) {
    return (
      <Link className="underline underline-offset-4" href={href}>
        {children}
      </Link>
    );
  }
  return (
    <a className="underline underline-offset-4" href={href}>
      {children}
    </a>
  );
}

function Sources({
  items,
}: {
  items: readonly { label: string; href: string }[];
}) {
  return (
    <section className="space-y-2 border-t border-black/30 pt-5">
      <h2 className="text-xl font-bold tracking-[-0.015em]">Sources</h2>
      <ul className="text-muted-foreground space-y-1 text-sm">
        {items.map((item) => (
          <li key={item.href}>
            <FollowLink href={item.href}>{item.label}</FollowLink>
          </li>
        ))}
      </ul>
    </section>
  );
}

const sharedBreakSources = [
  { label: "ArdaNova pattern library", href: "https://ardanova.com/patterns" },
  {
    label: "Reconciling two ledgers without a central custodian",
    href: "https://ardanova.com/patterns/reconciling-two-ledgers-without-a-central-custodian",
  },
  {
    label: "AZOA workflow specifications",
    href: "https://azoa.ardanova.com/workflows",
  },
  { label: "ArdaNova terms of use", href: "https://ardanova.com/terms" },
] as const;

const governanceSplitSources = [
  { label: "ArdaNova homepage", href: "https://ardanova.com" },
  {
    label: "Pattern: two records, one agreement",
    href: "https://ardanova.com/patterns/two-records-one-agreement",
  },
  {
    label: "ICA Statement on the Cooperative Identity (1995)",
    href: "https://ica.coop/en/cooperatives/cooperative-identity",
  },
  { label: "ArdaNova terms of use", href: "https://ardanova.com/terms" },
] as const;

const recordSources = [
  { label: "ArdaNova homepage", href: "https://ardanova.com" },
  { label: "ArdaNova terms of use", href: "https://ardanova.com/terms" },
  { label: "PrimusNeo", href: "https://primusneo.com" },
  { label: "AZOA", href: "https://azoa.ardanova.com" },
  { label: "AZOA Workflows", href: "https://azoa.ardanova.com/workflows" },
] as const;

const agreementSources = [
  { label: "ArdaNova homepage", href: "https://ardanova.com" },
  { label: "ArdaNova terms of use", href: "https://ardanova.com/terms" },
  { label: "PrimusNeo", href: "https://primusneo.com" },
] as const;

const bodies: Record<NoteSlug, () => ReactNode> = {
  "why-shared-work-breaks-between-orgs": () => (
    <>
      <p className="text-lg leading-relaxed">
        When two organizations team up on a joint build, nobody starts expecting
        a dispute over spreadsheets. Week one opens cleanly with shared repos, a
        Slack channel, and a friendly milestone estimate. Yet by week six, half
        of every sync disappears into reconciling different copies of who
        approved a change, why a deliverable split in two, and which doc
        actually holds the agreed baseline.
      </p>
      <p>
        Multi-org projects break less often from malice than from a structural
        missing piece: nobody owns a single place where work logs, decisions,
        and governing terms refer to the same immutable identifiers.
      </p>
      <h2 className="pt-2 text-xl font-bold tracking-[-0.015em]">
        Three silent failure modes across organizational boundaries
      </h2>
      <p>
        First, <strong>decisions get stranded inside chat channels</strong>. A
        team lead agrees to shift acceptance tests in a Friday DM, updates a
        ticket label, and forgets that the partner&apos;s project manager
        wasn&apos;t tagged. Three weeks later, each organization operates from
        opposite definitions of done.
      </p>
      <p>
        Second, <strong>milestones get edited in place</strong>. Trying to keep
        a spec clean by overwriting old bullet points destroys why a rule
        changed mid-flight. Newcomers see today&apos;s wording with zero trace
        of the compromises underneath it.
      </p>
      <p>
        Third, <strong>work accounting bleeds into decision power</strong>.
        Without separating effort tracked (Record A) from formal sign-offs
        (Record B), whoever holds the loudest status spreadsheet begins acting
        as though their tally decides governance outcomes too.
      </p>
      <h2 className="pt-2 text-xl font-bold tracking-[-0.015em]">
        Designing clean handoffs between partner ledgers
      </h2>
      <p>
        Keeping independent teams aligned starts with small habits baked into
        shared workspace records: attach every deliverable log to an explicit
        agreement ID, treat amendments as appended superseding entries rather
        than quiet overwrites, and pair agreement records with idempotent
        handoff steps like those explored in{" "}
        <FollowLink href="https://azoa.ardanova.com/workflows">
          AZOA workflows
        </FollowLink>
        . See our detailed walkthrough in{" "}
        <FollowLink href="/patterns/reconciling-two-ledgers-without-a-central-custodian">
          reconciling two ledgers without a central custodian
        </FollowLink>{" "}
        and{" "}
        <FollowLink href="/patterns/append-only-amendments-and-retractions">
          append-only amendments
        </FollowLink>
        .
      </p>
      <Sources items={sharedBreakSources} />
    </>
  ),
  "separating-contribution-ledgers-from-governance-votes": () => (
    <>
      <p className="text-lg leading-relaxed">
        Every time a community tool tries inventing a single universal score for
        participation, the same quiet drift sets in. A team starts counting pull
        requests, support hours, or contributed funds into one weighted balance,
        and before long uses that single score for everything from quarterly
        allocations to voting weight.
      </p>
      <p>
        Merging economic tracking and governance voice feels sleek in a product
        demo, but breaks trust under real pressure. If logging extra hours
        multiplies governance say, big time-banks squeeze out cautious
        reviewers. Conversely, if a quick simple majority vote directly rewrites
        historical effort ledgers, contributors stop logging work openly.
      </p>
      <h2 className="pt-2 text-xl font-bold tracking-[-0.015em]">
        Two orthogonal planes, one readable contract
      </h2>
      <p>
        ArdaNova models collaboration as two independent ledgers tied cleanly to
        shared problem agreements:
      </p>
      <ul className="list-disc space-y-2 pl-5">
        <li>
          <strong>Record A (Work &amp; value):</strong> Keeps task outcomes,
          agreed unit allocations, and reconciliation checkpoints visible across
          participants.
        </li>
        <li>
          <strong>Record B (Voice &amp; permissions):</strong> Keeps proposals,
          quorum rules, delegated roles, and recorded approvals distinct from
          unit balances.
        </li>
      </ul>
      <p>
        Cooperative governance principles nailed this distinction long before
        distributed software existed: primary cooperatives protect equal member
        voice independently of capital contributions so external funding never
        silently captures governance. Our{" "}
        <FollowLink href="/patterns/two-records-one-agreement">
          two records, one agreement
        </FollowLink>{" "}
        and{" "}
        <FollowLink href="/patterns/decision-logs">decision logs</FollowLink>{" "}
        patterns break down how to structure both data models cleanly while
        keeping legal rights anchored strictly in signed agreements.
      </p>
      <Sources items={governanceSplitSources} />
    </>
  ),
  "a-record-is-not-a-right": () => (
    <>
      <p className="text-lg leading-relaxed">
        Teams working together across org boundaries usually want one simple
        promise at the end of a sprint: that logged work actually counts, and
        the agreement behind it will still hold six months after details fade
        from memory. Software makes that desire dangerously easy to fake—put a
        balance on a screen, assign a unit tally, display a percentage badge,
        and everyone feels covered until a real dispute lands.
      </p>
      <p>
        Yet a database row is never a right on its own unless an external
        enforceable contract or applicable law backs it up. ArdaNova separates
        coordination from legal overreach right at the architecture layer: it
        gives builders a clean problem space for recording deliverables, agreed
        allocations, and decision trails while stating explicitly where records
        stop and formal agreements begin.
      </p>
      <p>
        Even inside the{" "}
        <FollowLink href="https://primusneo.com">PrimusNeo</FollowLink> stack
        alongside companion workflow engine{" "}
        <FollowLink href="https://azoa.ardanova.com/workflows">AZOA</FollowLink>
        , hosted platform tallies stay strictly inside workspace
        bookkeeping—they never claim to move bank funds automatically or
        substitute for executed agreements governed by Idaho law and reviewed
        directly by the participants involved.
      </p>
      <p>
        Honest boundaries make software records more valuable, not less. When
        Record A cleanly logs agreed work and reconciliation state without
        pretending to be a legal title certificate, participants stay
        protected—and newcomers reading{" "}
        <FollowLink href="/patterns/a-record-is-not-a-right">
          the full record-vs-right pattern
        </FollowLink>{" "}
        can verify how every claim holds up under scrutiny.
      </p>
      <Sources items={recordSources} />
    </>
  ),
  "two-records-one-agreement": () => (
    <>
      <p className="text-lg leading-relaxed">
        Every multi-party build generates two completely different kinds of
        facts: what work got shipped (and at what agreed value), and who held
        authority to steer decisions along the way. Mash both into a single
        score and you quickly wake up unable to tell whether a number on screen
        represents money, permission, or a participation trophy.
      </p>
      <p>
        ArdaNova, built within the{" "}
        <FollowLink href="https://primusneo.com">PrimusNeo</FollowLink> tooling
        stack, refuses that shortcut deliberately. <strong>Record A</strong>{" "}
        tracks work performed, agreed valuations, allocation logic, and
        reconciliation status. <strong>Record B</strong> captures proposals,
        voting or approval rules, delegated roles, and recorded outcomes—kept
        wholly apart from unit balances.
      </p>
      <p>
        Neither ledger tries replacing readable agreements under real law (in
        our hosted workspace, under terms published by Escherbridge, LLC in
        Boise, Idaho). Instead, putting work evidence and governance voice into
        cleanly separated structures makes the shared agreement unambiguous when
        reviewers, auditors, or future maintainers inspect what happened—and
        lets teams record a deliberate <em>stop</em> when an experiment should
        halt rather than inventing numbers to keep going. Explore the
        architecture in{" "}
        <FollowLink href="/patterns/two-records-one-agreement">
          Two records, one agreement
        </FollowLink>
        .
      </p>
      <Sources items={agreementSources} />
    </>
  ),
};

export function NoteBody({ slug }: { slug: NoteSlug }) {
  const Body = bodies[slug];
  return <Body />;
}
