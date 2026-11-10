import type { ReactNode } from "react";

import { publicCanonical } from "~/lib/site";

function FollowLink({ href, children }: { href: string; children: string }) {
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
    <section className="space-y-2">
      <h2 className="text-xl">Sources</h2>
      <ul className="space-y-1">
        {items.map((item) => (
          <li key={item.href}>
            <FollowLink href={item.href}>{item.label}</FollowLink>
          </li>
        ))}
      </ul>
    </section>
  );
}

const recordSources = [
  { label: "ArdaNova homepage", href: "https://ardanova.com" },
  { label: "ArdaNova terms of use", href: "https://ardanova.com/terms" },
  {
    label: "ArdaNova BFF health (economy: simulated)",
    href: "https://ardanova-bff-production.up.railway.app/health",
  },
  { label: "PrimusNeo", href: "https://primusneo.com" },
  { label: "AZOA", href: "https://azoa.ardanova.com" },
] as const;

const agreementSources = [
  { label: "ArdaNova homepage", href: "https://ardanova.com" },
  { label: "ArdaNova terms of use", href: "https://ardanova.com/terms" },
  { label: "PrimusNeo", href: "https://primusneo.com" },
] as const;

export const notes = [
  {
    slug: "a-record-is-not-a-right",
    title: "A record is not a right",
    date: "9 November 2026",
    summary:
      "ArdaNova will keep a record of work, of agreed value, and of decisions. That record is useful. It is not, by itself, ownership, equity, or a promise that anyone will pay you. The economy in the workspace is simulated, and the terms already say so.",
  },
  {
    slug: "two-records-one-agreement",
    title: "Two records. One agreement.",
    date: "9 November 2026",
    summary:
      "Work and voice are easy to mash into a single score. ArdaNova keeps them as two records on purpose: what was done and how value was agreed, on one side; who was allowed to decide, and what they decided, on the other. The agreement is what binds them, not a balance.",
  },
] as const;

export type NoteSlug = (typeof notes)[number]["slug"];

export function noteBySlug(slug: string) {
  return notes.find((note) => note.slug === slug);
}

export function noteCanonical(slug: string): string {
  return publicCanonical(`/notes/${slug}`);
}

const bodies: Record<NoteSlug, () => ReactNode> = {
  "a-record-is-not-a-right": () => (
    <>
      <p>
        People who do work together often want the same sentence at the end of
        the week: that the work will count, and that what it counted for will
        still be there when the week is forgotten. Software is very good at
        offering a costume of that sentence. A balance appears. A unit is
        allocated. A profile shows a share. The costume is comforting, and it is
        the beginning of a lot of confusion, because a row in a database is not
        a right unless something outside the database says it is.
      </p>
      <p>
        ArdaNova is a workspace for describing a problem, coordinating work on
        it, and keeping records of decisions and activity. It belongs to the{" "}
        <FollowLink href="https://primusneo.com">PrimusNeo</FollowLink> stack,
        and it is offered here as its own product. The marketing line is already
        on the homepage: find a problem, fund it, build it, share in what it
        becomes, on agreed terms. That line is doing a lot of work, and it is
        only safe if the next line travels with it. Platform records do not
        automatically create legal ownership, equity, or payment rights. Project
        agreements, participant review, and applicable law determine their
        effect. The terms of use, published by Escherbridge, LLC and effective
        10 November 2026, repeat the same idea in a slightly drier voice: a
        record here is not a promise that anyone will pay you, and it does not
        transfer property.
      </p>
      <p>
        The deployed service is also, today, a simulation. So is{" "}
        <FollowLink href="https://azoa.ardanova.com">AZOA</FollowLink>, the
        planned workflow engine in the same stack; this workspace does not treat
        it as live settlement. The API health check reports{" "}
        <code>economy: simulated</code>. The terms are explicit about what that
        means. Balances, unit counts, and funding records are bookkeeping inside
        the workspace. Selling units and sending money to a bank stay
        unavailable unless a deployment offers those steps. Ending a holding
        period on a record does not pay anyone. A recorded vote does not, by
        itself, move funds, change who is a member, or rewrite a project&apos;s
        terms. Those sentences should not live only on the terms page. They
        should sit next to &quot;Fund it,&quot; because &quot;Fund it&quot; is
        the phrase a hurried reader will remember.
      </p>
      <p>
        This is not a refusal to take records seriously. It is the condition
        under which they are worth keeping. A record of work performed, of
        agreed value, of allocation logic, of a proposal and its outcome, is
        useful when a group later disagrees, or when someone new arrives and
        asks what was already decided. It is more useful if it has not been
        asked to masquerade as a share certificate. Rights stay explicit:
        ownership and decision authority come from documented terms, never from
        activity, membership, or a balance alone. That line is already in the
        Trust section. It should remain harder to delete than a slogan about
        owning what you build.
      </p>
      <p>
        &quot;Own what you build&quot; is a desire, and it is a fine desire. It
        is not a transactional claim this workspace can honour by writing a
        number on a profile. Using it as a headline, a button, or a share image
        would be a shortcut, and the shortcut would be at the expense of the
        people the workspace is supposed to protect — the same people who are
        asked to start with the problem, make a plan others can check, and, if
        the plan fails, stop.
      </p>
      <p>
        ArdaNova is operated by Escherbridge, LLC in Boise, Idaho. The terms are
        governed by Idaho law, with disputes in Ada County courts. That is the
        legal frame. The product frame is smaller and, for the moment, more
        important: a simulated economy, two kinds of record, and a refusal to
        let the first pretend to be a right. When the economy is no longer
        simulated, the sentence about records and rights does not get to retire.
        It gets harder, and it still has to be true.
      </p>
      <Sources items={recordSources} />
    </>
  ),
  "two-records-one-agreement": () => (
    <>
      <p>
        A group that is trying to solve a problem usually has two kinds of fact
        to keep, and it usually tries to keep them in one place. Someone did the
        work. Someone was supposed to have a say. Mash those together and you
        get a score: a balance that looks like influence, or a vote that looks
        like a payment, or a membership that looks like a share. The mash is
        convenient. It is also how a project wakes up one morning unable to say
        whether a number on a screen is money, permission, or a souvenir.
      </p>
      <p>
        ArdaNova, part of the{" "}
        <FollowLink href="https://primusneo.com">PrimusNeo</FollowLink> stack,
        splits the facts on purpose. The homepage already names the split as a
        jacket line: two records, one agreement. Record A is work and value —
        the work performed, the agreed value, the allocation logic, the
        reconciliation status, kept visible to the people affected. Record B is
        voice and permissions — proposals, voting or approval rules, delegated
        roles, and recorded outcomes, kept distinct from economic interests. The
        agreement is what binds them. The software does not get to bind them by
        accident, and a balance does not get to stand in for a signature.
      </p>
      <p>
        This sounds like bookkeeping, which is the point. Bookkeeping is what
        you want when a later disagreement arrives, or when a person who was not
        in the room asks what was already decided. A single score cannot answer
        that question. It can only offer a feeling of fairness, which is a
        feeling that tends to evaporate under review.
      </p>
      <p>
        The method above the records is deliberately short. Start with the
        people the problem affects. Make a plan others can check. Build, test,
        revise — or stop. &quot;Or stop&quot; is not a tone. It is the only way
        the loop is a method rather than a slogan. A workspace that cannot
        record a stop will eventually invent a reason to keep going, and the
        records will be asked to support the invention.
      </p>
      <p>
        What the records cannot do is still the most important sentence on the
        site. They do not automatically create legal ownership, equity, or
        payment rights. In the current deployment they are also, in the economic
        column, simulated: bookkeeping inside the workspace, not a payment and
        not a transfer of property. A recorded vote does not, by itself, move
        funds, change who is a member, or rewrite a project&apos;s terms. If a
        particular project wants those effects, it will have to get them from an
        agreement people can actually read, under law that actually applies — in
        this operator&apos;s case, from terms published by Escherbridge, LLC,
        governed by the laws of Idaho, with disputes in Ada County courts, plus
        whatever project-level terms the participants review.
      </p>
      <p>
        There is a temptation to close the split later, once the product feels
        friendlier, by introducing a single &quot;contribution score&quot; that
        drives both pay and voice. That would be a different product, and a
        worse one for the problem this one set out to hold. Work can be visible
        without being a vote. A vote can be recorded without being a payout. The
        agreement is the place those two facts are allowed to meet. Everything
        else is a mash with a nicer name.
      </p>
      <Sources items={agreementSources} />
    </>
  ),
};

export function NoteBody({ slug }: { slug: NoteSlug }) {
  const Body = bodies[slug];
  return <Body />;
}
