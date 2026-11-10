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

/** End limit for a field note. The label is not the opening theme. */
function FieldEconomy({
  balances = false,
  trailing,
}: {
  balances?: boolean;
  trailing?: ReactNode;
}) {
  return (
    <p>
      The economy on this workspace is simulated.
      {balances ? " Balances are bookkeeping, not payment." : null}{" "}
      <strong className="font-mono">ECONOMY: SIMULATED.</strong> Platform
      records do not automatically create legal ownership, equity, or payment
      rights.
      {trailing ? <> {trailing}</> : null}
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
  "directors-minutes": () => (
    <>
      <p>
        The Companies Act 2006 requires every company to cause minutes of all
        proceedings at meetings of its directors to be recorded, and to keep
        those records for at least ten years from the date of the meeting. If
        the directors met and no minute was recorded, the official record of
        that meeting does not yet exist.
      </p>
      <p>
        This is a field note about recording directors&apos; proceedings, not a
        product theme and not legal advice. It is not advice about any
        particular company, and it does not say how a particular entity should
        format minutes. It does not treat section 248 as applying, without more,
        to every informal chat or to non-company bodies. The practice siblings —{" "}
        <TextLink href="/patterns/what-belongs-in-the-minutes">
          what belongs in the minutes
        </TextLink>{" "}
        and{" "}
        <TextLink href="/patterns/readable-record">
          a record someone else can read
        </TextLink>{" "}
        — take up the habit. The already-live{" "}
        <TextLink href="/patterns/decision-logs">decision logs</TextLink> page
        is the pattern sibling.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl">Hero figures</h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>
              Companies Act 2006, s.248, &quot;Minutes of directors&apos;
              meetings,&quot;
            </strong>{" "}
            latest available revised text on legislation.gov.uk. &quot;(1) Every
            company must cause minutes of all proceedings at meetings of its
            directors to be recorded. (2) The records must be kept for at least
            ten years from the date of the meeting.&quot;
          </li>
          <li>
            <strong>On default:</strong> failure to comply is an offence by
            every officer of the company who is in default, with a fine on
            summary conviction not exceeding level 3 on the standard scale and a
            daily default fine for continued contravention not exceeding
            one-tenth of level 3. Those are the Act&apos;s words. This page does
            not convert &quot;level 3&quot; into a pound figure that is not on
            the opened page, and it does not turn the penalty subsection into a
            scare lede.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">What s.248 actually says</h2>
        <p>
          Every company; minutes of all proceedings at directors&apos; meetings;
          keep them at least ten years from the date of the meeting. That is the
          official duty, in the Act&apos;s own words. This page does not invent
          a house minutes template as if it satisfied s.248, and it does not
          give advice on offences or filings.
        </p>
        <p>
          The members&apos; and general-meeting side of the same record-keeping
          habit is{" "}
          <TextLink href="/patterns/records-of-resolutions">
            records of resolutions
          </TextLink>{" "}
          (s.355).{" "}
          <TextLink href="/patterns/informal-channels">
            Decisions on informal channels
          </TextLink>{" "}
          is the companion for a decision that happened in chat.
        </p>
        <p>
          A recorded decision on ArdaNova still needs a record. A recorded vote
          does not, by itself, move funds or rewrite terms.{" "}
          <TextLink href="/patterns/a-record-is-not-a-right">
            A record is not a right
          </TextLink>{" "}
          is the already-live pattern for that limit.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">An illustrative scenario</h2>
        <p>
          This is a worked example, not a customer and not a company filing.
          Directors met, a decision was taken, and the only trace is a later
          chat message. Ten years from the date of the meeting, no minute can be
          found. Section 248 already names the missing record.
        </p>
      </section>
      <CitedSources slug="directors-minutes" />
    </>
  ),
  "records-of-resolutions": () => (
    <>
      <p>
        The Companies Act 2006 also requires a company to keep copies of
        members&apos; resolutions passed otherwise than at general meetings,
        minutes of all proceedings of general meetings, and the details of
        sole-member decisions — and to keep those records for at least ten
        years. A &quot;minute book&quot; in this sense is a set of records:
        written resolutions, general-meeting minutes, and sole-member decisions,
        kept for at least ten years from the relevant date.
      </p>
      <p>
        This is a field note about the members&apos; and general-meeting record,
        sibling to{" "}
        <TextLink href="/patterns/directors-minutes">
          directors&apos; minutes
        </TextLink>
        , not a second copy of that page, and not legal advice. It does not
        invent a constitution, a share register, or a filing duty from this
        section. It does not quote the body of section 357; that section is
        named only as s.355 already names it. There is no ownership framing of
        &quot;resolutions.&quot;
      </p>
      <section className="space-y-3">
        <h2 className="text-xl">Hero figures</h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>
              Companies Act 2006, s.355, &quot;Records of resolutions and
              meetings etc,&quot;
            </strong>{" "}
            latest available revised text on legislation.gov.uk. &quot;(1) Every
            company must keep records comprising— (a) copies of all resolutions
            of members passed otherwise than at general meetings, (b) minutes of
            all proceedings of general meetings, and (c) details provided to the
            company in accordance with section 357 (decisions of sole member).
            (2) The records must be kept for at least ten years from the date of
            the resolution, meeting or decision (as appropriate).&quot;
          </li>
          <li>
            <strong>On default:</strong> failure to comply is an offence by
            every officer in default, with the same style of summary fine as
            s.248 (level 3, and a daily default fine not exceeding one-tenth of
            level 3). This page does not invent a pound figure.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">What s.355 actually lists</h2>
        <p>
          Three kinds of record: copies of members&apos; resolutions passed
          otherwise than at general meetings; minutes of all proceedings of
          general meetings; and details of sole-member decisions provided in
          accordance with section 357. The ten-year rule is attached to
          &quot;the date of the resolution, meeting or decision (as
          appropriate).&quot;
        </p>
        <p>
          The directors&apos; side of the habit is{" "}
          <TextLink href="/patterns/directors-minutes">s.248</TextLink>. The
          already-live pattern siblings are{" "}
          <TextLink href="/patterns/decision-logs">decision logs</TextLink> and{" "}
          <TextLink href="/patterns/two-records-one-agreement">
            two records, one agreement
          </TextLink>
          .{" "}
          <TextLink href="/patterns/what-belongs-in-the-minutes">
            What belongs in the minutes
          </TextLink>{" "}
          is the practice list for the note itself.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">An illustrative scenario</h2>
        <p>
          This is a worked example, not a customer and not a filing. A written
          resolution of the members was passed outside a general meeting, and
          the only copy is in someone&apos;s inbox. Ten years from the date of
          the resolution, the company cannot produce it. Section 355 already
          names the missing record.
        </p>
      </section>
      <CitedSources slug="records-of-resolutions" />
    </>
  ),
  "what-belongs-in-the-minutes": () => (
    <>
      <p>
        Official UK Private Office records guidance says a meeting record should
        capture the date, the time, who was there, and the decisions — including
        when the exchange happened on a non-corporate channel. If the note does
        not say when it was, who was there, and what was decided, it is not yet
        doing the job a later reader needs.
      </p>
      <p>
        This is a practice page about what to write down, sibling to the statute
        pages and to the already-live{" "}
        <TextLink href="/patterns/decision-logs">decision logs</TextLink>{" "}
        pattern. It is not legal advice, not a required template, and not a
        claim that ArdaNova, or a reader&apos;s company, is bound by Private
        Office rules. The opened guidance is official UK government practice for
        Private Office information and records, published 29 December 2023. It
        is guidance to a public-authority setting, not a statute for this
        workspace, and not a certification.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl">Hero figures</h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>
              &quot;Guidance for the management of Private Office information
              and records (HTML),&quot; published 29 December 2023,
            </strong>{" "}
            tells officials to &quot;Keep a record of ministers&apos; meetings
            on substantive issues whatever the medium,&quot; and to &quot;Record
            the date, time, location or format (in person, online, via
            non-corporate channels), who took part, and decisions or
            undertakings.&quot; It also says: &quot;Record decisions arising
            from telephone or online meetings or via non-corporate communication
            channels, such as WhatsApp.&quot; This is a public model of what
            belongs in a decision record, not a public-records duty on users,
            and not a surveillance recommendation.
          </li>
          <li>
            <strong>
              HMRC Records and Information Collection Policy, 19 May 2026,
              quoting ISO 15489-1:2016:
            </strong>{" "}
            a record is &quot;information created, received and maintained as
            evidence and information… in pursuance of legal obligations or in
            the transaction of business.&quot; HMRC creates records &quot;to
            provide information about what happened, what was decided, and how
            to do things,&quot; and says information has most value when it is
            &quot;accurate, up-to-date and accessible when it is needed.&quot;
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">The official list, as guidance</h2>
        <p>
          Date, time, who, decisions — and the format, including a non-corporate
          channel if that is where it happened. What happened and what was
          decided, in HMRC&apos;s quotation of ISO 15489. Non-corporate channels
          do not get a free pass. The longer treatment of informal channels is{" "}
          <TextLink href="/patterns/informal-channels">
            If the decision happened in the chat
          </TextLink>
          .{" "}
          <TextLink href="/patterns/readable-record">
            A record someone else can read
          </TextLink>{" "}
          is the later-reader companion.{" "}
          <TextLink href="/patterns/directors-minutes">
            Directors&apos; minutes
          </TextLink>{" "}
          and{" "}
          <TextLink href="/patterns/records-of-resolutions">
            records of resolutions
          </TextLink>{" "}
          are the statute siblings.
        </p>
        <p>
          The habit is the same whether or not the economy is simulated. This
          page does not give legal advice about personal messaging apps beyond
          what the opened guidance actually says.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">An illustrative scenario</h2>
        <p>
          This is a worked example, not a customer. A note says &quot;agreed in
          the meeting,&quot; with no date, no names, and no decision written as
          a decision. A later person cannot tell what was decided, or who was
          there. The Private Office list already names the missing fields.
        </p>
      </section>
      <CitedSources slug="what-belongs-in-the-minutes" />
    </>
  ),
  "readable-record": () => (
    <>
      <p>
        The UK Health and Safety Executive&apos;s key-decision-log practice is
        that the log should be contemporaneous, give reasons, be dated and
        timed, include inaction and retractions, and be readable by someone who
        was not in the room. If only the people who were there can decode the
        note, the record is not yet doing its job.
      </p>
      <p>
        This is a field note about a later reader, not a safety-inspection
        claim, not a rewrite of{" "}
        <TextLink href="/patterns/decision-logs">decision logs</TextLink>, and
        not legal advice. The opened page is official HSE internal operational
        guidance on a key decision log. It is HSE investigation practice, not a
        general corporate statute, and not a certification ArdaNova holds. HSE
        rules are not claimed to apply to this workspace or to a private
        meeting. No &quot;key decision log&quot; product is invented here.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl">Hero figures</h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>
              UK Health and Safety Executive, &quot;Investigation — Stage 3:
              Prepare the investigation,&quot; Key decision log.
            </strong>{" "}
            The KDL is defined as &quot;a contemporaneous record of the key
            decisions that affect the course of an investigation and the reasons
            for those decisions.&quot; It &quot;is not a diary of the actions
            taken.&quot;
          </li>
          <li>
            <strong>On inaction and retractions:</strong> &quot;It is equally
            important to record decisions and reasons for not doing something as
            for doing something and to record any changes or retractions of
            previous decisions.&quot;
          </li>
          <li>
            <strong>On timing and a later reader:</strong> Entries &quot;should
            be made as soon as possible after making the decision&quot;;
            &quot;all recorded decisions must be dated and timed&quot;; and
            &quot;The record in the KDL should enable someone who is not
            directly involved… to understand the thought processes of the
            investigator and the reasons for the course of the
            investigation.&quot;
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">What HSE&apos;s practice actually asks for</h2>
        <p>
          Contemporaneous; reasons; dated and timed; inaction and retractions
          included; readable by someone not in the room. Useful as a habit. Not
          an ArdaNova obligation.{" "}
          <TextLink href="/patterns/what-belongs-in-the-minutes">
            What belongs in the minutes
          </TextLink>{" "}
          is the short official list for date, time, who, and decisions.{" "}
          <TextLink href="/patterns/directors-minutes">
            Directors&apos; minutes
          </TextLink>{" "}
          and{" "}
          <TextLink href="/patterns/records-of-resolutions">
            records of resolutions
          </TextLink>{" "}
          are the statute siblings.{" "}
          <TextLink href="/patterns/decision-logs">Decision logs</TextLink> is
          the pattern page this one complements.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">An illustrative scenario</h2>
        <p>
          This is a worked example, not a customer and not an investigation. A
          log entry reads &quot;same as last time, DK to handle,&quot; with no
          date, no reason, and no account of a decision not to act. A person who
          was not in the room cannot reconstruct the thought. HSE&apos;s
          later-reader sentence is already the test that entry fails.
        </p>
      </section>
      <CitedSources slug="readable-record" />
    </>
  ),
  "informal-channels": () => (
    <>
      <p>
        Official Private Office records guidance does not treat a non-corporate
        channel as a place where official business can happen without a record.
        If the decision was taken there, the record still has to capture it. The
        channel does not retire the duty to have a record.
      </p>
      <p>
        This is a field note about where a decision actually happened, not a
        policy on monitoring anyone&apos;s messages, not a recommendation to
        search private devices, and not legal advice about messaging apps. It is
        public-authority guidance, not a private-company statute, and not a
        surveillance recommendation. Stay with the records habit: if the
        decision happened there, write it down in the official record.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl">Hero figures</h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>
              &quot;Guidance for the management of Private Office information
              and records (HTML),&quot; published 29 December 2023,
            </strong>{" "}
            already opened for the decision-logs and what-belongs pages, tells
            officials to &quot;Keep a record of ministers&apos; meetings on
            substantive issues whatever the medium,&quot; and to &quot;Record
            the date, time, location or format (in person, online, via
            non-corporate channels), who took part, and decisions or
            undertakings.&quot; It also says: &quot;Record decisions arising
            from telephone or online meetings or via non-corporate communication
            channels, such as WhatsApp.&quot;
          </li>
          <li>
            <strong>Companies Act 2006, s.248 and s.355,</strong> opened 10
            November 2026, still require minutes and resolution records to be
            kept, without creating an exception for informal tools. They are
            pointed at here only as the{" "}
            <TextLink href="/patterns/directors-minutes">
              directors&apos; minutes
            </TextLink>{" "}
            and{" "}
            <TextLink href="/patterns/records-of-resolutions">
              records of resolutions
            </TextLink>{" "}
            siblings. Neither section is stretched into a chat-app offence.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">What the opened guidance is for</h2>
        <p>
          Private Office practice; non-corporate channels included; date, time,
          who, and decisions still belong in the record. WhatsApp is named in
          the guidance as an example of a non-corporate communication channel,
          not as a product recommendation, and not as a reason to install a
          monitor. This page does not recommend a chat product, a retention
          vendor, or a surveillance setup.
        </p>
        <p>
          <TextLink href="/patterns/what-belongs-in-the-minutes">
            What belongs in the minutes
          </TextLink>{" "}
          is the short list of fields.{" "}
          <TextLink href="/patterns/readable-record">
            A record someone else can read
          </TextLink>{" "}
          is the later-reader test.{" "}
          <TextLink href="/patterns/decision-logs">Decision logs</TextLink> is
          the pattern sibling.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">An illustrative scenario</h2>
        <p>
          This is a worked example, not a customer and not a monitoring
          proposal. A decision was taken in a personal-device chat, everyone who
          was there remembers it differently a month later, and the official
          record is silent. The guidance already says the channel does not
          excuse the missing note.
        </p>
      </section>
      <CitedSources slug="informal-channels" />
    </>
  ),
  "what-happened-what-was-decided": () => (
    <>
      <p>
        HMRC, quoting ISO 15489-1:2016, defines records as information created,
        received and maintained as evidence and information in the transaction
        of business, and says it creates records to provide information about
        what happened, what was decided, and how to do things. If the note does
        not tell a later reader those three things, it is not doing the work
        HMRC names. This is a short field note about what a record is for, not a
        product page, and not legal advice.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl">Hero figures</h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>
              HMRC Records and Information Collection Policy, published 19 May
              2026.
            </strong>{" "}
            &quot;Records are defined as information created, received and
            maintained as evidence and information by an organisation or person,
            in pursuance of legal obligations or in the transaction of business
            (ISO 15489-1:2016 Information and documentation — Records management
            — Part 1: Concepts and principles).&quot; The definition is
            attributed to HMRC quoting the standard, not to a clause number that
            was not opened.
          </li>
          <li>
            <strong>HMRC&apos;s own sentence:</strong> &quot;HMRC creates
            records in the day-to-day management of its services to provide
            information about what happened, what was decided, and how to do
            things. These records are a valuable resource because of the
            information they contain.&quot; &quot;This policy is format neutral,
            and applies to paper, digital, hybrid and any other record
            formats.&quot;
          </li>
          <li>
            <strong>HMRC also quotes ISO 15489-1:2016</strong> on records
            management as the field of management &quot;responsible for the
            efficient and systemic control of the creation, receipt,
            maintenance, use and disposition of records.&quot; The word
            &quot;systemic&quot; is quoted as it appears on the opened page; it
            is not silently corrected to another standard&apos;s wording. The
            benefits list on the same page is HMRC describing its own practice,
            not a claim about ArdaNova.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">What this page is not</h2>
        <p>
          This is a public-authority quotation of what a record is for. It is
          not a private-company filing rule, and it is not a claim that ArdaNova
          is ISO-certified or that HMRC has reviewed the product.{" "}
          <TextLink href="/patterns/what-belongs-in-the-minutes">
            What belongs in the minutes
          </TextLink>{" "}
          and <TextLink href="/patterns/decision-logs">Decision logs</TextLink>{" "}
          are the siblings for what to put in the official note.{" "}
          <TextLink href="/patterns/tna-section-46">
            The National Archives Code of Practice on managing records
          </TextLink>{" "}
          is the companion Code, addressed to relevant authorities.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">An illustrative scenario</h2>
        <p>
          This is a worked example, not a filing and not a customer. A folder of
          messages is labelled &quot;the record,&quot; and a later reader cannot
          tell what happened, what was decided, or how to do the thing.
          HMRC&apos;s sentence is already the test that folder fails.
        </p>
      </section>
      <CitedSources slug="what-happened-what-was-decided" />
    </>
  ),
  "tna-section-46": () => (
    <>
      <p>
        The National Archives&apos; July 2021 Code, issued under section 46 of
        the Freedom of Information Act 2000, is guidance to relevant authorities
        on keeping, managing and destroying their records, built on the
        principles of value, integrity and accountability — and &quot;not all
        information is a record.&quot; The Code is about a framework so an
        authority can account for its activities. It is official guidance to
        public authorities, not a product badge, and not a claim that ArdaNova
        is a relevant authority or that it has been assessed against the Code.
        This page is not legal advice.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl">Hero figures</h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>
              GOV.UK landing, &quot;Code of Practice on the Management of
              Records issued under section 46 the Freedom of Information Act
              2000,&quot; published 15 July 2021, The National Archives.
            </strong>{" "}
            &quot;Code of Practice issued under section 46 of the Freedom of
            Information Act providing guidance to public authorities on the
            keeping, management and destruction of records.&quot; Applies to
            England, Northern Ireland and Wales. ISBN 978-1-5286-2517-3; 32-page
            PDF.
          </li>
          <li>
            <strong>
              The Code PDF itself, laid before Parliament on 15 July 2021.
            </strong>{" "}
            &quot;This is a statutory code of practice issued under section 46
            of the Freedom of Information Act 2000.&quot; &quot;This Code of
            Practice (the ‘Code’) provides guidance to authorities which helps
            them to create a framework for keeping, managing and destroying
            their information, and therefore records. Complying with this Code
            will help authorities to account for their activities.&quot;
            &quot;The Code takes a principles-based approach. The three
            principles of value, integrity and accountability provide a
            high-level framework for authorities to manage information and
            maintain a record of their activities.&quot; &quot;The principles
            apply to information in any format or medium that the authority
            holds or that another body holds on its behalf.&quot; &quot;For the
            purposes of the Code, the term information is used as shorthand for
            both information and records. Not all information is a record.&quot;
          </li>
          <li>
            <strong>
              On value, integrity, and accountability, from the opened table in
              Part Two:
            </strong>{" "}
            the authority must manage information so it can assess current and
            future value, &quot;keep information as long as they can show it has
            value and dispose of it when it no longer has value,&quot; and
            &quot;be able to explain why they no longer hold information&quot;;
            &quot;The authority and all its stakeholders must be able to rely
            upon and trust the information that it holds&quot;; &quot;The
            authority’s information management must enable it to provide a clear
            and accurate account of its activity in accordance with its legal
            and other obligations.&quot; Reasons to keep information include
            &quot;for accountability and audit,&quot; &quot;to comply with
            regulatory requirements,&quot; &quot;to protect legal and other
            rights and interests,&quot; and &quot;as a historical record.&quot;
          </li>
          <li>
            <strong>Who it is for.</strong> Annex A: the Code applies to
            &quot;relevant authorities&quot; as defined in FOIA s.46(7) — public
            authorities and certain bodies whose records are public records —
            not to bodies subject only to the Scottish FOI Act. Annex B: it
            fulfils the Secretary of State&apos;s duty to provide guidance on
            practice which it would &quot;be desirable for them to follow in
            connection with the keeping, management and destruction of their
            records.&quot; The Information Commissioner may issue a practice
            recommendation under FOIA s.48 to public authorities whose practice
            does not conform. The glossary (which &quot;does not form part of
            the Code itself&quot;) defines records, quoting BS ISO 15489-1:2016,
            as &quot;recorded information created, received, and maintained as
            evidence and as an asset by an organisation or person, in pursuit of
            legal obligations or in the transaction of business.&quot; If that
            wording sits next to HMRC&apos;s, each is kept intact; they are not
            identical.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">What this page is not</h2>
        <p>
          This is a source, not a claim that ArdaNova is a relevant authority or
          that it has passed a TNA or ICO assessment. The glossary is not
          treated as operative Code text; the PDF says it is not part of the
          Code itself.{" "}
          <TextLink href="/patterns/what-happened-what-was-decided">
            What a record is for
          </TextLink>{" "}
          is the HMRC-definition sibling.{" "}
          <TextLink href="/patterns/decision-logs">Decision logs</TextLink> and{" "}
          <TextLink href="/patterns/readable-record">
            Write a record someone else can read
          </TextLink>{" "}
          are the practice siblings.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">An illustrative scenario</h2>
        <p>
          This is a worked example, not an assessment and not a customer. A
          workspace is described as &quot;compliant with the section 46
          Code,&quot; with no authority, no assessment, and no account of what
          was kept or destroyed. The Code is already addressed to relevant
          authorities, and this site is not one.
        </p>
      </section>
      <CitedSources slug="tna-section-46" />
    </>
  ),
  "articles-of-incorporation": () => (
    <>
      <p>
        People often search &quot;articles of incorporation.&quot; Under the
        Companies Act 2006, a company must have articles of association
        prescribing regulations for the company. Those articles are part of the
        company&apos;s constitution; they are not the minutes of a meeting, and
        they are not a register of members. The statutory name is used in the
        body; the address of this page keeps the query people type. This is a
        field note about the document that prescribes how the company is run,
        not a formation-product page, and not legal advice about a specific
        entity.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl">Hero figures</h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Companies Act 2006, s.18 (Articles of association).</strong>{" "}
            &quot;(1) A company must have articles of association prescribing
            regulations for the company.&quot; &quot;(2) Unless it is a company
            to which model articles apply by virtue of section 20 (default
            application of model articles in case of limited company), it must
            register articles of association.&quot; &quot;(3) Articles of
            association registered by a company must— (a) be contained in a
            single document, and (b) be divided into paragraphs numbered
            consecutively.&quot; &quot;(4) References in the Companies Acts to a
            company&apos;s ‘articles’ are to its articles of association.&quot;
          </li>
          <li>
            <strong>
              Companies Act 2006, s.20 (Default application of model articles).
            </strong>{" "}
            On the formation of a limited company, if articles are not
            registered, or if they are registered but do not exclude or modify
            the relevant model articles, &quot;the relevant model articles (so
            far as applicable) form part of the company&apos;s articles in the
            same manner and to the same extent as if articles in the form of
            those articles had been duly registered.&quot; The &quot;relevant
            model articles&quot; are &quot;the model articles prescribed for a
            company of that description as in force at the date on which the
            company is registered.&quot; This page does not invent a 2026
            model-articles text that was not opened.
          </li>
          <li>
            <strong>
              Companies Act 2006, s.17 (A company&apos;s constitution).
            </strong>{" "}
            Unless the context otherwise requires, references to a
            company&apos;s constitution include &quot;(a) the company&apos;s
            articles, and (b) any resolutions and agreements to which Chapter 3
            applies (see section 29).&quot;
          </li>
          <li>
            <strong>
              Companies Act 2006, s.8 (Memorandum of association), for the
              contrast only.
            </strong>{" "}
            A memorandum states that the subscribers &quot;wish to form a
            company under this Act, and agree to become members of the
            company&quot; (and, where the company is to have a share capital,
            &quot;to take at least one share each&quot;). That sentence is
            quoted only to distinguish the memorandum from the articles. The
            share-capital limb is left as that contrast.
          </li>
        </ul>
      </section>
      <p>
        The dedicated Companies House &quot;model articles&quot; guidance URL
        returned HTTP 404 on 2026-11-10 and is not linked here. The
        formation-guide page that lists articles of association was opened and
        is not used for liability passages. This page stays with the Act.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl">What they are not</h2>
        <p>
          Not minutes.{" "}
          <TextLink href="/patterns/directors-minutes">
            Directors&apos; meeting minutes
          </TextLink>{" "}
          and{" "}
          <TextLink href="/patterns/records-of-resolutions">
            Records of resolutions and general meetings
          </TextLink>{" "}
          are the siblings for those duties. Not the register of members;{" "}
          <TextLink href="/patterns/company-registers">
            What is a company register of members
          </TextLink>{" "}
          is the next brief in this batch.{" "}
          <TextLink href="/patterns/two-records-one-agreement">
            Two records, one agreement
          </TextLink>{" "}
          is the already-briefed companion when two records have to stay
          aligned.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">An illustrative scenario</h2>
        <p>
          This is a worked example, not a filing and not a customer. A folder
          labelled &quot;articles of incorporation&quot; holds only a set of
          meeting notes, with no regulations for the company and no register of
          members. The Act already names the articles, the minutes, and the
          register as distinct records.
        </p>
      </section>
      <CitedSources slug="articles-of-incorporation" />
    </>
  ),
  "company-registers": () => (
    <>
      <p>
        Every company must keep a register of its members. That register is one
        of the company&apos;s required records; it is not the minutes of
        directors&apos; meetings, and it is not the record of resolutions. Those
        duties sit side by side; one file does not discharge the other. This is
        a field distinction between two required records, and not legal advice
        about a specific entity.
      </p>
      <p>
        The opened legislation page banner read: &quot;Section 113 is up to date
        with all changes known to be in force on or before 10 November 2026.
        There are changes that may be brought into force at a future date.&quot;
      </p>
      <section className="space-y-3">
        <h2 className="text-xl">Hero figures</h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Companies Act 2006, s.113 (Register of members).</strong>{" "}
            &quot;(1) Every company must keep a register of its members.&quot;
            The in-force subsection (2), as it still stands on the opened page,
            requires entry of &quot;(a) the names and addresses of the members,
            (b) the date on which each person was registered as a member, and
            (c) the date at which any person ceased to be a member.&quot;
          </li>
          <li>
            <strong>A later substitution</strong> of subsection (2) and a new
            (2A), pointing at &quot;required information&quot; in sections 113A
            and 113B, appears on the same page with the annotation that it was
            substituted &quot;26.10.2023 but only so far as it confers a power
            to make regulations or relates to the exercise of the power,
            otherwise prosp.&quot; This page does not write as if 113A and 113B
            are already the everyday duty.
          </li>
          <li>
            <strong>Subsection (3)</strong> adds, for a company having a share
            capital, a statement of the shares held by each member (with the
            numbering and class detail the subsection gives). That statutory
            wording is quoted only as what the register must contain in that
            case. Default in complying with the section is an offence by the
            company and every officer in default (subsection (7)). No penalty
            amount is invented beyond what was quoted from the opened page, and
            no advice is given about a specific entity.
          </li>
          <li>
            <strong>
              Companies Act 2006, s.1134 (Meaning of &quot;company
              records&quot;), for the contrast.
            </strong>{" "}
            In that Part, &quot;company records&quot; means &quot;any register,
            index, accounting records, agreement, memorandum, minutes or other
            document required by the Companies Acts to be kept by a
            company,&quot; and any register of debenture holders. Registers and
            minutes are both company records; they are not the same kind of
            record.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">What this page is not</h2>
        <p>
          ArdaNova is not described here as a live register of legal title.{" "}
          <TextLink href="/patterns/directors-minutes">
            Directors&apos; meeting minutes
          </TextLink>{" "}
          and{" "}
          <TextLink href="/patterns/records-of-resolutions">
            Records of resolutions and general meetings
          </TextLink>{" "}
          are the minutes and resolutions siblings.{" "}
          <TextLink href="/patterns/articles-of-incorporation">
            What are articles of association
          </TextLink>{" "}
          is the constitution sibling.{" "}
          <TextLink href="/patterns/a-record-is-not-a-right">
            A record is not a right
          </TextLink>{" "}
          is the already-briefed limit.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">An illustrative scenario</h2>
        <p>
          This is a worked example, not a filing and not a customer. A single
          &quot;minute book&quot; holds names, votes, and notes, and is treated
          as the register of members. The Act already requires a register, and
          it already requires minutes. One pile does not discharge both.
        </p>
      </section>
      <CitedSources slug="company-registers" />
    </>
  ),
  "sole-member-decisions": () => (
    <>
      <p>
        Where a company has only one member, a decision that may be taken by the
        company in general meeting, and that has effect as if so agreed, must
        still be given to the company in writing — unless it was taken as a
        written resolution. Having no meeting does not mean there is nothing to
        record. This is a short companion to{" "}
        <TextLink href="/patterns/records-of-resolutions">
          Records of resolutions and general meetings
        </TextLink>
        , not a formation-product page, and not legal advice about a specific
        entity.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl">Hero figures</h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>
              Companies Act 2006, s.357 (Records of decisions by sole member).
            </strong>{" "}
            &quot;(1) This section applies to a company limited by shares or by
            guarantee that has only one member.&quot; &quot;(2) Where the member
            takes any decision that— (a) may be taken by the company in general
            meeting, and (b) has effect as if agreed by the company in general
            meeting, he must (unless that decision is taken by way of a written
            resolution) provide the company with details of that decision.&quot;
            &quot;(3) If a person fails to comply with this section he commits
            an offence.&quot; &quot;(5) Failure to comply with this section does
            not affect the validity of any decision referred to in subsection
            (2).&quot; The penalty in subsection (4), as quoted from the opened
            page, is &quot;a fine not exceeding level 2 on the standard
            scale.&quot; That figure is not turned into advice about a specific
            person. &quot;Limited by shares&quot; and &quot;limited by
            guarantee&quot; are the Act&apos;s words.
          </li>
          <li>
            <strong>
              Companies Act 2006, s.288, only as the cross-reference s.357
              itself makes.
            </strong>{" "}
            In the Companies Acts a &quot;written resolution&quot; means a
            resolution of a private company proposed and passed in accordance
            with that Chapter. A written resolution of a private company
            &quot;has effect as if passed… by the company in general
            meeting&quot; (or by a class meeting, as the case may be). Certain
            removals of a director or an auditor may not be passed as a written
            resolution. Those sentences are used only to explain the
            &quot;unless… written resolution&quot; exception in s.357. This page
            is not a second essay on the written-resolution procedure.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">What s.357 actually says</h2>
        <p>
          Scope: a company limited by shares or by guarantee that has only one
          member. The duty: provide the company with details of the decision,
          unless it was taken as a written resolution. Failure is an offence;
          failure does not, by itself, undo the decision. This page does not
          invent filing forms or deadlines that are not in the opened text.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl">An illustrative scenario</h2>
        <p>
          This is a worked example, not a filing and not a customer. A
          one-member company takes a decision that has effect as if agreed in
          general meeting, and nothing is given to the company because
          &quot;there was no meeting.&quot; Section 357 already requires the
          details, unless the decision was taken as a written resolution.
        </p>
      </section>
      <CitedSources slug="sole-member-decisions" />
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
  "directors-minutes": () => (
    <>
      <p>
        <TextLink href="https://azoa.ardanova.com">AZOA</TextLink> is the
        planned workflow engine; its public site, like this workspace, is still
        a simulation.{" "}
        <TextLink href="https://primusneo.com">PrimusNeo</TextLink> is the lab
        whose stack this workspace belongs to.
      </p>
      <FieldEconomy balances />
      <p>
        The useful next step is to record the proceedings of the meeting, or to
        read <TextLink href="/patterns/decision-logs">decision logs</TextLink>.
        If something on this page is wrong,{" "}
        <TextLink href="/">write to us</TextLink>.
      </p>
      <ReviewedLine />
    </>
  ),
  "records-of-resolutions": () => (
    <>
      <FieldEconomy
        balances
        trailing={
          <>
            <TextLink href="https://azoa.ardanova.com">AZOA</TextLink> is the
            planned workflow engine, also still a simulation.{" "}
            <TextLink href="https://primusneo.com">PrimusNeo</TextLink> is the
            lab whose stack this workspace belongs to.
          </>
        }
      />
      <p>
        The useful next step is to keep the three kinds of record s.355 actually
        lists, or to read{" "}
        <TextLink href="/patterns/directors-minutes">
          directors&apos; minutes
        </TextLink>
        . If something on this page is wrong,{" "}
        <TextLink href="/">write to us</TextLink>.
      </p>
      <ReviewedLine />
    </>
  ),
  "what-belongs-in-the-minutes": () => (
    <>
      <FieldEconomy />
      <p>
        The useful next step is to write the date, the time, who was there, and
        what was decided. If something on this page is wrong,{" "}
        <TextLink href="/">write to us</TextLink>.
      </p>
      <ReviewedLine />
    </>
  ),
  "readable-record": () => (
    <>
      <FieldEconomy />
      <p>
        The useful next step is to write the note so a person who was not in the
        room can read it. If something on this page is wrong,{" "}
        <TextLink href="/">write to us</TextLink>.
      </p>
      <ReviewedLine />
    </>
  ),
  "informal-channels": () => (
    <>
      <FieldEconomy />
      <p>
        The useful next step is to write the decision into the official record,
        including the date, the time, who took part, and that it happened on an
        informal channel. If something on this page is wrong,{" "}
        <TextLink href="/">write to us</TextLink>.
      </p>
      <ReviewedLine />
    </>
  ),
  "what-happened-what-was-decided": () => (
    <>
      <FieldEconomy />
      <p>
        The useful next step is to write the note so it says what happened, what
        was decided, and how to do the thing. If something on this page is
        wrong, <TextLink href="/">write to us</TextLink>.
      </p>
      <ReviewedLine />
    </>
  ),
  "tna-section-46": () => (
    <>
      <FieldEconomy />
      <p>
        The useful next step is to read the Code as guidance to public
        authorities, not as a badge this site holds. If something on this page
        is wrong, <TextLink href="/">write to us</TextLink>.
      </p>
      <ReviewedLine />
    </>
  ),
  "articles-of-incorporation": () => (
    <>
      <FieldEconomy
        trailing={<>A recorded vote still does not, by itself, move funds.</>}
      />
      <p>
        The useful next step is to say whether you are reading the articles, the
        minutes, or the register. If something on this page is wrong,{" "}
        <TextLink href="/">write to us</TextLink>.
      </p>
      <ReviewedLine />
    </>
  ),
  "company-registers": () => (
    <>
      <FieldEconomy
        trailing={<>A recorded vote still does not, by itself, move funds.</>}
      />
      <p>
        The useful next step is to keep the register, the minutes, and the
        record of resolutions as distinct records. If something on this page is
        wrong, <TextLink href="/">write to us</TextLink>.
      </p>
      <ReviewedLine />
    </>
  ),
  "sole-member-decisions": () => (
    <>
      <FieldEconomy
        trailing={<>A recorded vote still does not, by itself, move funds.</>}
      />
      <p>
        The useful next step is to give the company details of the decision, or
        to record it as a written resolution if that route applies. If something
        on this page is wrong, <TextLink href="/">write to us</TextLink>.
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
