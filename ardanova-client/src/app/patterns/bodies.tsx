import type { ReactNode } from "react";
import Link from "next/link";

import { RelatedLinks } from "~/components/brand/related-links";
import { SimulationNotice } from "~/components/brand/simulation-notice";
import { patternBySlug, type PatternSlug } from "~/lib/writing";

function TextLink({ href, children }: { href: string; children: ReactNode }) {
  const className = "underline underline-offset-4";
  if (href.startsWith("/")) {
    return (
      <Link className={className} href={href}>
        {children}
      </Link>
    );
  }
  return (
    <a className={className} href={href}>
      {children}
    </a>
  );
}

function EconomyParagraph({ children }: { children?: ReactNode }) {
  return (
    <p>
      <strong className="font-mono">ECONOMY: SIMULATED.</strong> Platform
      records do not automatically create legal ownership, equity, or payment
      rights.
      {children ? <> {children}</> : null}
    </p>
  );
}

function SourceList({
  items,
  intro,
}: {
  items: readonly { href: string; label: string }[];
  intro: string;
}) {
  return (
    <section className="space-y-2">
      <h2 className="text-xl">Sources</h2>
      <p>{intro}</p>
      <ol className="list-decimal space-y-1 pl-5">
        {items.map((source) => (
          <li key={source.label}>
            <a className="underline underline-offset-4" href={source.href}>
              {source.label}
            </a>
          </li>
        ))}
      </ol>
    </section>
  );
}

function CitedSources({ slug }: { slug: PatternSlug }) {
  const pattern = patternBySlug(slug);
  if (!pattern) return null;
  return (
    <SourceList
      intro="Every stat on this page is cited."
      items={pattern.citations}
    />
  );
}

function ReviewedLine() {
  return (
    <p>Author: Escherbridge. Last reviewed: 2026-11-10, America/Denver.</p>
  );
}

const bodies: Record<PatternSlug, () => ReactNode> = {
  "a-record-is-not-a-right": () => (
    <>
      <p>
        A minute book or a members&apos; register is a record of what was
        written down, and even a statutory register is only evidence of those
        entries. It does not, by itself, create ownership, equity, or a payment
        right. That is already the live ArdaNova disclaimer, and it matters more
        because the workspace economy is simulated. Public company law already
        treats a members&apos; register as evidence of what was entered, and
        minutes as evidence of proceedings. ArdaNova&apos;s terms go one step
        further in the present tense: a record here is not a promise of payment.
      </p>
      <p>
        Software is very good at costumes of ownership. This page will not raise
        the reader&apos;s pulse, and it will not soften the simulation sentence
        to make the costume look warmer.
      </p>
      <EconomyParagraph>
        Balances, unit counts, and funding records are bookkeeping inside the
        workspace, not a payment and not a transfer of property.
      </EconomyParagraph>
      <p>
        The legal citations below are a public contrast. ArdaNova is not a UK
        company, not a cap-table product, and not a statutory filing in Idaho or
        anywhere else.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl">Hero points</h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Companies Act 2006, s.113:</strong> &quot;Every company must
            keep a register of its members.&quot; The current subsection (2)
            requires, in respect of each member, the required information and
            the date the person was registered as a member; (2A) requires the
            date membership ceased. For a company with share capital, subsection
            (3) also requires a statement of the shares held by each member
            (number and, where relevant, class) and the amount paid or agreed to
            be considered as paid. This is a duty to keep a record of membership
            and, where relevant, of shareholdings. It is not a grant of title by
            software.
          </li>
          <li>
            <strong>Companies Act 2006, s.127:</strong> &quot;The register of
            members is prima facie evidence of any matters which are by this Act
            directed or authorised to be inserted in it.&quot; Prima facie means
            evidence in the first instance, not an unchallengeable right. The
            transferable point is the one the live essay already makes in house
            language: a row is not a right unless something outside the row says
            it is.
          </li>
          <li>
            <strong>Companies Act 2006, s.248–249:</strong> every company
            &quot;must cause minutes of all proceedings at meetings of its
            directors to be recorded,&quot; and &quot;The records must be kept
            for at least ten years from the date of the meeting.&quot; Minutes
            recorded under s.248, if purporting to be authenticated by the
            chairman of the meeting or of the next directors&apos; meeting,
            &quot;are evidence (in Scotland, sufficient evidence) of the
            proceedings at the meeting.&quot; Until the contrary is proved, the
            meeting is deemed duly held, the proceedings deemed to have duly
            taken place, and appointments deemed valid. Minutes are a required
            record of proceedings, with a rebuttable evidential effect — not a
            transfer of property and not a cap table. The ten-year figure is the
            UK companies figure, not a house retention policy.
          </li>
          <li>
            <strong>
              ISO 15489-1:2016, as quoted on HMRC&apos;s Records and Information
              Collection Policy (published 19 May 2026):
            </strong>{" "}
            records are &quot;information created, received and maintained as
            evidence and information by an organisation or person, in pursuance
            of legal obligations or in the transaction of business.&quot; HMRC
            adds that it creates records &quot;to provide information about what
            happened, what was decided, and how to do things.&quot; A useful
            record answers those questions. It does not, by being stored, settle
            who owns what.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">The bill for a split system</h2>
        <p>
          <strong>
            Treating a balance or unit count as equity or a payout.
          </strong>{" "}
          The live homepage and Terms (effective 10 November 2026) already
          refuse this: &quot;A record here is not a promise that anyone will pay
          you, and it does not transfer property.&quot;
        </p>
        <p>
          <strong>
            Treating a recorded vote as a change of membership or of terms.
          </strong>{" "}
          The same live pages say a recorded vote &quot;does not, by itself,
          move funds, change who is a member, or rewrite a project&apos;s
          terms.&quot;
        </p>
        <p>
          <strong>
            Importing the Companies Act as if ArdaNova, or Escherbridge, were a
            UK company,
          </strong>{" "}
          or inventing a US cap-table statute that was not opened. The later
          exception in s.127 for matters of which the central register is prima
          facie evidence, after changes associated with the Economic Crime and
          Corporate Transparency Act 2023, is not over-explained here, and no
          equivalent US or Idaho wording is invented.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">A shared record, applied here</h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>
              Keep work, value, and decisions as records that can be read later.
            </strong>
          </li>
          <li>
            <strong>
              Point every such record at an agreement and at applicable law, not
              at the row itself.
            </strong>
          </li>
          <li>
            <strong>
              Leave the simulation sentence on the same screen as any mention of
              balances or funding.
            </strong>
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">What ArdaNova is, and is not, on this page</h2>
        <p>
          This is a workspace for problems, work, and records, and the economy
          is simulated.{" "}
          <TextLink href="https://azoa.ardanova.com">AZOA</TextLink>, the
          planned workflow engine, is also still a simulation. This workspace
          belongs to the stack at{" "}
          <TextLink href="https://primusneo.com">PrimusNeo</TextLink>.
        </p>
      </section>
      <CitedSources slug="a-record-is-not-a-right" />
      <section className="space-y-3">
        <h2 className="text-xl">An illustrative scenario</h2>
        <p>
          This is a worked example, not a customer. A project ledger shows a
          unit count after a week of work. A later reader asks whether anyone is
          owed money. The record can show the work. It cannot, by itself, show
          the right.
        </p>
      </section>
    </>
  ),
  "two-records-one-agreement": () => (
    <>
      <p>
        When work and voice share a score, a balance starts to look like
        influence and a vote starts to look like a payment. Public cooperative
        identity already refuses that mash: in a primary cooperative, members
        have equal voting rights — one member, one vote — while capital is
        contributed and compensated on different terms. ArdaNova&apos;s
        &quot;two records, one agreement&quot; is the same kind of split, in a
        simulated workspace. A balance still must not be mistaken for a vote or
        a share.
      </p>
      <p>
        ArdaNova is not a cooperative, is not ICA-registered, and is not
        adopting &quot;one member, one vote&quot; as a live product rule on this
        page. The Statement on the Cooperative Identity is a public primer, used
        as contrast.
      </p>
      <EconomyParagraph>
        The economy on this workspace is simulated. A recorded vote does not, by
        itself, move funds, change who is a member, or rewrite a project&apos;s
        terms.
      </EconomyParagraph>
      <section className="space-y-3">
        <h2 className="text-xl">Hero points</h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>
              The International Cooperative Alliance&apos;s Statement on the
              Cooperative Identity, adopted in 1995,
            </strong>{" "}
            defines a cooperative as &quot;an autonomous association of persons
            united voluntarily to meet their common economic, social and
            cultural needs and aspirations through a jointly-owned and
            democratically-controlled enterprise.&quot;
            &quot;Jointly-owned&quot; and &quot;democratically-controlled&quot;
            are already two ideas in one sentence.
          </li>
          <li>
            <strong>Principle 2, Democratic Member Control:</strong> &quot;In
            primary cooperatives members have equal voting rights (one member,
            one vote) and cooperatives at other levels are also organised in a
            democratic manner.&quot; Decision rights, in that model, are not
            bought by a larger contribution. This is not a promise about
            ArdaNova membership or voting power.
          </li>
          <li>
            <strong>Principle 3, Member Economic Participation:</strong>{" "}
            &quot;Members contribute equitably to, and democratically control,
            the capital of their cooperative.&quot; &quot;At least part of that
            capital is usually the common property of the cooperative.&quot;{" "}
            &quot;Members usually receive limited compensation, if any, on
            capital subscribed as a condition of membership.&quot; Surpluses are
            allocated for developing the cooperative (including reserves, part
            of which at least would be indivisible), benefiting members in
            proportion to their transactions, and supporting other activities
            approved by the membership. Contribution, compensation, and voice
            are described as related but not identical. That is the public
            warrant for keeping Record A (work and value) distinct from Record B
            (voice and permissions).
          </li>
          <li>
            <strong>Principle 4, Autonomy and Independence:</strong> if
            cooperatives &quot;raise capital from external sources, they do so
            on terms that ensure democratic control by their members and
            maintain their cooperative autonomy.&quot; Even in that official
            primer, outside capital is not allowed to silently rewrite who
            decides. This page does not narrate a raise or an ArdaNova
            financing.
          </li>
        </ul>
      </section>
      <p>
        The live homepage already names the split, and this page does not
        improve the jacket lines: Record A — &quot;Work + value / Contribution
        and economic interests&quot; (work performed, agreed value, allocation
        logic, reconciliation status). Record B — &quot;Voice + permissions /
        Governance and decision rights&quot; (proposals, voting or approval
        rules, delegated roles, recorded outcomes), &quot;kept distinct from
        economic interests.&quot;
      </p>
      <section className="space-y-3">
        <h2 className="text-xl">The bill for a split system</h2>
        <p>
          <strong>A contribution balance used as a vote weight.</strong> The
          records can show the work and the rule separately, and they should not
          be mashed into a score.
        </p>
        <p>
          <strong>A vote treated as a payment or a membership change,</strong>{" "}
          which the homepage already refuses.
        </p>
        <p>
          <strong>Calling ArdaNova a cooperative.</strong> &quot;Share in what
          it becomes&quot; appears on the live site only on agreed terms, next
          to the simulation and no-automatic-rights sentences. This page will
          not upgrade it.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">A shared record, applied here</h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Write work and agreed value in Record A,</strong> visible to
            the people affected.
          </li>
          <li>
            <strong>
              Write proposals, rules, roles, and outcomes in Record B,
            </strong>{" "}
            apart from economic interests.
          </li>
          <li>
            <strong>
              Put the binding in a readable agreement, under law that actually
              applies — and say the present deployment does not transfer
              property.
            </strong>
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">What ArdaNova is, and is not, on this page</h2>
        <p>
          The pattern is two records and one agreement, in a simulated economy.{" "}
          <TextLink href="https://azoa.ardanova.com">AZOA</TextLink>, the
          planned workflow engine, is also still a simulation. This workspace
          belongs to the stack at{" "}
          <TextLink href="https://primusneo.com">PrimusNeo</TextLink>. The ICA
          Guidance Notes mentioned on the official identity page were not opened
          and are not quoted.
        </p>
      </section>
      <CitedSources slug="two-records-one-agreement" />
      <section className="space-y-3">
        <h2 className="text-xl">An illustrative scenario</h2>
        <p>
          This is a worked example, not a customer. A contributor asks whether a
          higher unit count should mean a louder vote. The records can show the
          work and the rule, separately. The agreement is what would bind them.
          The present economy still does not transfer property.
        </p>
      </section>
    </>
  ),
  "decision-logs": () => (
    <>
      <p>
        A usable decision log is a contemporaneous record of what was decided,
        why, by whom, and when — including changes and decisions not to act.
        That is already how official records-management and investigation-log
        guidance talks about decisions. A chat thread or a later recap is not
        the same thing. On ArdaNova a recorded decision still does not, by
        itself, move funds or rewrite terms.
      </p>
      <p>
        If a later person cannot tell what was decided, why, by whom, and when,
        the group does not have a decision. It has a memory argument. Official
        guidance already names the fields, and ArdaNova can keep that kind of
        record without pretending the record executes the economy.
      </p>
      <EconomyParagraph>
        A recorded vote does not, by itself, move funds, change who is a member,
        or rewrite a project&apos;s terms. ArdaNova users are not under FOIA,
        HSE, or Companies Act duties; those sources are public models of a log,
        not a product registration.
      </EconomyParagraph>
      <section className="space-y-3">
        <h2 className="text-xl">Hero points</h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>
              ISO 15489-1:2016, as quoted by HMRC (policy published 19 May
              2026):
            </strong>{" "}
            a record is &quot;information created, received and maintained as
            evidence and information… in pursuance of legal obligations or in
            the transaction of business.&quot; HMRC creates records &quot;to
            provide information about what happened, what was decided, and how
            to do things,&quot; and says information has most value when it is
            &quot;accurate, up-to-date and accessible when it is needed.&quot;
            That is the test for a shared-work log: can a person who was not in
            the room find what was decided, while it is still true?
          </li>
          <li>
            <strong>
              UK government guidance for ministers&apos; private offices
              (published 29 December 2023)
            </strong>{" "}
            tells officials to &quot;Keep a record of ministers&apos; meetings
            on substantive issues whatever the medium,&quot; and to &quot;Record
            the date, time, location or format (in person, online, via
            non-corporate channels), who took part, and decisions or
            undertakings.&quot; It also says: &quot;Record decisions arising
            from telephone or online meetings or via non-corporate communication
            channels, such as WhatsApp.&quot; This is a public model of what
            belongs in a decision record, not a public-records duty on users.
          </li>
          <li>
            <strong>
              The UK Health and Safety Executive&apos;s key decision log (KDL)
            </strong>{" "}
            is defined as &quot;a contemporaneous record of the key decisions
            that affect the course of an investigation and the reasons for those
            decisions.&quot; It &quot;is not a diary of the actions taken.&quot;{" "}
            &quot;It is equally important to record decisions and reasons for
            not doing something as for doing something and to record any changes
            or retractions of previous decisions.&quot; Entries &quot;should be
            made as soon as possible after making the decision&quot;; &quot;all
            recorded decisions must be dated and timed&quot;; and &quot;The
            record in the KDL should enable someone who is not directly
            involved… to understand the thought processes of the investigator
            and the reasons for the course of the investigation.&quot; ArdaNova
            is not an investigation product or an HSE tool.
          </li>
          <li>
            <strong>Companies Act 2006, s.355:</strong> a company must keep
            records comprising copies of members&apos; resolutions passed
            otherwise than at general meetings, minutes of all proceedings of
            general meetings, and specified sole-member decisions, &quot;kept
            for at least ten years from the date of the resolution, meeting or
            decision.&quot; Section 248 requires directors&apos; minutes to be
            kept for at least ten years. Again: UK company law as a public
            minute-keeping contrast, not a house retention policy.
          </li>
        </ul>
      </section>
      <p>
        The National Archives&apos; Code of Practice on the management of
        records, issued under section 46 of the Freedom of Information Act 2000
        and published 15 July 2021, is official guidance to public authorities
        on keeping, managing, and destroying records. It is cited here as the
        public-authority framework that sits behind the private-office meeting
        rule, not as a checklist ArdaNova has passed, and not as a set of
        metrics — the landing page was opened; the PDF body was not relied on
        for figures.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl">The bill for a split system</h2>
        <p>
          <strong>
            A chat recap written a month later, sold as the decision.
          </strong>
        </p>
        <p>
          <strong>
            Logging every action and missing the actual decision, or omitting
            the decision not to act.
          </strong>{" "}
          The HSE log is not a diary. Inaction and retractions belong in the
          same record.
        </p>
        <p>
          <strong>
            Treating the log as a transfer of funds, membership, or terms.
          </strong>{" "}
          Already refused on the homepage.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">A shared record, applied here</h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Write the decision as soon as it is made:</strong> date,
            time, who, the decision, the reason.
          </li>
          <li>
            <strong>Record changes and retractions in the same log,</strong>{" "}
            rather than silently editing the past.
          </li>
          <li>
            <strong>
              Keep the simulation and &quot;not a right&quot; sentences next to
              any recorded vote or outcome.
            </strong>
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">What ArdaNova is, and is not, on this page</h2>
        <p>
          Decisions stay recorded; the economy is simulated. This is the first
          research child for the topic; there is no separate house essay yet,
          only the two live notes on records and agreements.{" "}
          <TextLink href="https://azoa.ardanova.com">AZOA</TextLink> is also
          still a simulation; its live notes on{" "}
          <TextLink href="https://azoa.ardanova.com/notes/fail-closed-or-it-isnt-a-control">
            fail-closed controls
          </TextLink>{" "}
          and{" "}
          <TextLink href="https://azoa.ardanova.com/notes/settle-once-or-not-at-all">
            settle-once habit
          </TextLink>{" "}
          are related writing, not a live-value claim. Related practice in the
          stack:{" "}
          <TextLink href="https://neos.primusneo.com/blog/act-is-not-a-vote">
            NEOS on ACT
          </TextLink>
          .
        </p>
      </section>
      <CitedSources slug="decision-logs" />
      <section className="space-y-3">
        <h2 className="text-xl">An illustrative scenario</h2>
        <p>
          This is a worked example, not a customer. A group pauses a build in a
          voice call and cannot later agree whether they stopped or only
          hesitated. A dated note would have settled the fact, not the right.
          The economy is still simulated either way.
        </p>
      </section>
    </>
  ),
  "problem-intake": () => (
    <>
      <p>
        <strong className="font-mono">ECONOMY: SIMULATED.</strong>
      </p>
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
        intro="Every statement on this page is already published on this site."
        items={[{ href: "https://ardanova.com/", label: "ArdaNova homepage" }]}
      />
    </>
  ),
};

const closings: Partial<Record<PatternSlug, () => ReactNode>> = {
  "a-record-is-not-a-right": () => (
    <>
      <p>
        Related:{" "}
        <TextLink href="https://ardanova.com/notes/a-record-is-not-a-right">
          A record is not a right
        </TextLink>{" "}
        (the house essay),{" "}
        <TextLink href="https://ardanova.com/notes/two-records-one-agreement">
          Two records. One agreement.
        </TextLink>
        , <TextLink href="https://ardanova.com/terms">Terms</TextLink>,{" "}
        <TextLink href="https://primusneo.com">PrimusNeo</TextLink>.
      </p>
      <p>
        The useful next step is to read the{" "}
        <TextLink href="https://ardanova.com/">homepage</TextLink> and the{" "}
        <TextLink href="https://ardanova.com/terms">Terms</TextLink>, including
        the simulation sentence, before treating any row as a right.
      </p>
      <ReviewedLine />
    </>
  ),
  "two-records-one-agreement": () => (
    <>
      <p>
        Related:{" "}
        <TextLink href="https://ardanova.com/notes/two-records-one-agreement">
          Two records. One agreement.
        </TextLink>
        ,{" "}
        <TextLink href="https://ardanova.com/notes/a-record-is-not-a-right">
          A record is not a right
        </TextLink>
        , <TextLink href="https://ardanova.com/terms">Terms</TextLink>,{" "}
        <TextLink href="https://primusneo.com">PrimusNeo</TextLink>.
      </p>
      <p>
        The useful next step is to read the{" "}
        <TextLink href="https://ardanova.com/">homepage</TextLink> and the{" "}
        <TextLink href="https://ardanova.com/terms">Terms</TextLink>, and to
        leave the simulation sentence in view while you do.
      </p>
      <ReviewedLine />
    </>
  ),
  "decision-logs": () => (
    <>
      <p>
        Related:{" "}
        <TextLink href="https://ardanova.com/notes/a-record-is-not-a-right">
          A record is not a right
        </TextLink>
        ,{" "}
        <TextLink href="https://ardanova.com/notes/two-records-one-agreement">
          Two records. One agreement.
        </TextLink>
        , <TextLink href="https://ardanova.com/terms">Terms</TextLink>,{" "}
        <TextLink href="https://primusneo.com">PrimusNeo</TextLink>.
      </p>
      <p>
        The useful next step is to read the{" "}
        <TextLink href="https://ardanova.com/">homepage</TextLink> and the{" "}
        <TextLink href="https://ardanova.com/terms">Terms</TextLink>. A decision
        that cannot be found later is hard to hand to the next person.
      </p>
      <ReviewedLine />
    </>
  ),
};

export function PatternBody({ slug }: { slug: PatternSlug }) {
  const pattern = patternBySlug(slug);
  const Body = bodies[slug];
  const Closing = closings[slug];
  if (!pattern) return null;
  return (
    <>
      <Body />
      <RelatedLinks heading="Also in this section" items={pattern.related} />
      {Closing ? <Closing /> : null}
    </>
  );
}
