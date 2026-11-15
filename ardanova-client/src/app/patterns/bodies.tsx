import type { ReactNode } from "react";
import Link from "next/link";

import { RelatedLinks } from "~/components/brand/related-links";
import { SimulationNotice } from "~/components/brand/simulation-notice";
import { WorkspaceFunnelGate } from "~/components/brand/WorkspaceFunnelGate";
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

function SourceList({
  items,
  intro,
}: {
  items: readonly { href: string; label: string }[];
  intro?: string;
}) {
  return (
    <section className="space-y-2 border-t border-black/30 pt-5">
      <h2 className="text-xl font-bold tracking-[-0.015em]">Sources</h2>
      {intro ? <p className="text-muted-foreground text-sm">{intro}</p> : null}
      <ol className="text-muted-foreground list-decimal space-y-1 pl-5 text-sm">
        {items.map((source) => (
          <li key={source.label}>
            <a
              className="hover:text-foreground underline underline-offset-4"
              href={source.href}
            >
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
  return <SourceList items={pattern.citations} />;
}

function ReviewedLine() {
  return (
    <p className="text-muted-foreground text-sm">
      Author: Escherbridge · Last reviewed: 2026-11-14, America/Denver.
    </p>
  );
}

const bodies: Record<PatternSlug, () => ReactNode> = {
  "a-record-is-not-a-right": () => (
    <>
      <p className="text-lg leading-relaxed">
        Teams building shared tools love the reassurance of a ledger row: an
        allocation column updates, a contributor balance climbs, or a member
        list grows a new line item. Before long, collaborators start treating
        those database rows as though they automatically confer property rights,
        voting authority, or guaranteed payouts.
      </p>
      <p>
        In practice, software records only prove what was entered—not whether an
        underlying right actually exists outside the database. Even in UK
        corporate practice, which turns record-keeping into strict statutory
        obligations, a members&apos; register and a board minute book operate as{" "}
        <em>evidence</em> of entries and proceedings rather than self-executing
        legal title.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          What durable record-keeping establishes—and where it stops
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Evidence of membership vs. grant of title:</strong> Under{" "}
            the UK Companies Act register framework,{" "}
            <em>
              &quot;Every company must keep a register of its members&quot;
            </em>{" "}
            recording names, addresses, entry dates, and exit dates (and, for
            companies with share capital, counts and amounts paid). Even there,
            the register stands only as <em>prima facie</em> evidence of what
            was inserted—rebuttable if reality or outside contracts contradict
            the row.
          </li>
          <li>
            <strong>Board minutes track proceedings, not transfers:</strong>{" "}
            Directors must record minutes of all board meetings and preserve
            them:{" "}
            <em>
              &quot;The records must be kept for at least ten years from the
              date of the meeting.&quot;
            </em>{" "}
            Properly authenticated minutes create rebuttable proof that a
            meeting occurred and decisions were recorded, not a transfer of
            assets on their own. (Note: ten years reflects UK statutory
            corporate timing, cited here as an institutional baseline rather
            than a software retention guarantee.)
          </li>
          <li>
            <strong>Operational definitions match common sense:</strong>{" "}
            HMRC&apos;s 19 May 2026 Records Policy, quoting ISO 15489-1:2016,
            frames a record simply as evidence maintained in the transaction of
            business—built to document{" "}
            <em>
              &quot;what happened, what was decided, and how to do things.&quot;
            </em>
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Common mistakes when ledgers masquerade as title
        </h2>
        <p>
          <strong>
            Confusing bookkeeping counts with enforceable payouts.
          </strong>{" "}
          Unit balances and contribution allocations inside a shared workspace
          track agreed effort inside the project boundary—they never replace a
          signed commercial contract or payment settlement system.
        </p>
        <p>
          <strong>Assuming a recorded poll moves assets automatically.</strong>{" "}
          A vote logged inside governance software captures group intent; by
          itself, it does not rewrite legal agreements, alter corporate
          membership, or transfer property across jurisdictions.
        </p>
        <p>
          <strong>Applying foreign statutes blindly to software tools.</strong>{" "}
          UK register sections (including later prospective amendments tied to
          the Economic Crime and Corporate Transparency Act 2023) clarify good
          evidential separation for builders—without turning US-operated tools
          into statutory corporate registries in Idaho or elsewhere.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          How to structure the pattern cleanly
        </h2>
        <ul className="list-disc space-y-2 pl-5">
          <li>
            Store work performed, agreed valuations, and decisions in clear,
            timestamped rows that survive team turnover.
          </li>
          <li>
            Bind every ledger view back to an explicit, human-readable project
            agreement under applicable law rather than letting UI balances imply
            title.
          </li>
          <li>
            Display workspace limits clearly beside financial or unit figures so
            no collaborator confuses simulated tracking with live legal title.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          A project ledger shows a unit tally after two sprints of joint
          engineering across two partner teams. Months later, a finance lead
          asks whether an invoice is owed automatically. The ledger settles{" "}
          <em>what work was logged and agreed</em>; the underlying partner
          agreement settles <em>what is owed and when</em>.
        </p>
      </section>
      <CitedSources slug="a-record-is-not-a-right" />
    </>
  ),

  "two-records-one-agreement": () => (
    <>
      <p className="text-lg leading-relaxed">
        Every multi-party project maintains two fundamentally different sets of
        facts: who contributed work or funding (and how value gets divided), and
        who holds authority to steer decisions. Most collaboration apps merge
        both into a single numeric score—and that shortcut quietly breaks
        fairness on both sides.
      </p>
      <p>
        Once a contribution balance begins behaving like voting weight, larger
        budgets buy out governance. Conversely, once a quick majority vote acts
        like a unilateral payroll adjustment, contributors stop trusting the
        ledger. Splitting work accounting (Record A) from governance authority
        (Record B) keeps each concern verifiable on its own terms.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Why cooperatives learned this separation a century ago
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Jointly owned, democratically steered:</strong> The
            International Cooperative Alliance&apos;s 1995 Statement on the
            Cooperative Identity defines cooperatives around two distinct
            pillars right in one phrase:{" "}
            <em>&quot;jointly-owned and democratically-controlled.&quot;</em>
          </li>
          <li>
            <strong>Principle 2 separates voice from balance size:</strong> In
            primary cooperatives, voting follows{" "}
            <em>&quot;one member, one vote&quot;</em> precisely so governance
            authority stays anchored in membership rather than ledger balance.
            (ArdaNova tracks both patterns without claiming ICA cooperative
            status.)
          </li>
          <li>
            <strong>
              Principle 3 treats economic participation independently:
            </strong>{" "}
            Members contribute equitably, hold common property cleanly, and
            receive strictly agreed compensation—keeping capital rules auditable
            without collapsing them into governance permissions.
          </li>
          <li>
            <strong>Principle 4 protects decision autonomy:</strong> External
            funding enters only on terms preserving member governance rather
            than silently overriding who decides.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Record A, Record B, and the agreement between them
        </h2>
        <p>
          <strong>Record A (Work + value):</strong> Logs tasks delivered, agreed
          pricing or unit allocations, and reconciliation status so every
          participant sees exactly how effort was weighted.
        </p>
        <p>
          <strong>Record B (Voice + permissions):</strong> Logs proposals,
          delegated authorities, approval rules, and vote outcomes without
          reading or modifying unit balances.
        </p>
        <p>
          <strong>What binds them:</strong> An explicit written project
          agreement specifies how and when deliverables in Record A trigger
          outcomes approved in Record B—keeping software bookkeeping out of the
          business of guessing legal intent.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Where merged systems break down
        </h2>
        <p>
          <strong>
            Using contribution tallies as automatic vote multipliers.
          </strong>{" "}
          Without an explicit charter rule authorizing weighted voting, turning
          hours or units into louder governance signals alienates active
          builders mid-project.
        </p>
        <p>
          <strong>
            Letting governance approvals rewrite compensation retrospectively.
          </strong>{" "}
          A vote to pivot scope belongs in Record B; settled work already logged
          in Record A shouldn&apos;t vanish silently because direction changed.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          A partner joining halfway through a build invests more capital than a
          founding engineer and asks why their vote count isn&apos;t three times
          larger on architecture reviews. Splitting Record A and Record B makes
          the boundary plain: capital share lives in the economic ledger, while
          technical and project sign-off authority follows the explicit rules in
          Record B.
        </p>
      </section>
      <CitedSources slug="two-records-one-agreement" />
    </>
  ),

  "reconciling-two-ledgers-without-a-central-custodian": () => (
    <>
      <p className="text-lg leading-relaxed">
        Two organizations agree to co-build a shared release. Organization A
        logs sprint deliveries inside its issue tracker; Organization B tracks
        milestone budgets inside its own finance workbook. By week two, rows
        have been edited locally on both sides and exported CSVs no longer
        match—yet neither team wants to hand over sole control of its records to
        a single vendor database.
      </p>
      <p>
        Waiting until quarter-end reconciliation guarantees friction. Keeping
        two independent records aligned requires small structural anchors baked
        into entries from day one rather than heroic spreadsheet merges later.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Three primitives that keep independent books honest
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Shared agreement &amp; causal entry IDs:</strong> Every
            event emitted in either partner&apos;s record carries the explicit
            agreement identifier and a deterministic client-side sequence key
            (such as a monotonic counter plus creator scope), avoiding
            collisions from unsynchronized wall clocks.
          </li>
          <li>
            <strong>Append-only checkpoint digests:</strong> At agreed sprint
            boundaries, both sides exchange a lightweight period summary listing
            entry count, settled unit totals, and a deterministic content
            checksum over canonicalized entries. Divergence surfaces at the
            exact window where drift started rather than three months later.
          </li>
          <li>
            <strong>Explicit exception routing into Record B:</strong> When
            automated diffing catches a mismatch—a retracted deliverable, a
            split milestone, or an unlogged scope swap—neither system overwrites
            the other. The diff generates a discrepancy notice resolved openly
            via{" "}
            <TextLink href="/patterns/decision-logs">decision logs</TextLink>.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          How Record A and workflow engines cooperate cleanly
        </h2>
        <p>
          Pairing ArdaNova&apos;s split record model with idempotent handoffs in{" "}
          <TextLink href="https://azoa.ardanova.com/workflows">
            AZOA workflow specifications
          </TextLink>{" "}
          prevents retried webhook deliveries from double-counting deliverables
          across two partner databases. Each side stays authoritative for its
          own state while sharing verifiable checkpoint receipts.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          A partner engineering guild finishes twelve hours of integration fixes
          the same afternoon a sponsor team splits a milestone into two phases.
          Because both ledgers anchor entries by agreement ID and run Friday
          checkpoint digests, Friday&apos;s reconciliation flags one unlinked
          entry immediately—resolved in five minutes with an appended amendment
          instead of stalling invoicing a month later.
        </p>
      </section>
      <CitedSources slug="reconciling-two-ledgers-without-a-central-custodian" />
    </>
  ),

  "decision-logs": () => (
    <>
      <p className="text-lg leading-relaxed">
        Six weeks after a pivotal architecture review, two leads hold completely
        opposite memories of what got approved. Chat search surfaces forty-two
        messages with partial context, three emoji reactions, and zero clarity
        on whether an objection carried. Without a dedicated decision log, the
        team doesn&apos;t have a governance outcome—it has an argument about
        memory.
      </p>
      <p>
        Good decision logging is deliberately boring: write down what was
        decided, why, by whom, and when as soon as the choice lands—including
        explicit decisions <em>not</em> to proceed and later reversals.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          What seasoned operational standards capture in a log
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Actionable clarity (HMRC / ISO 15489-1:2016):</strong>{" "}
            HMRC&apos;s 19 May 2026 policy frames operational records as
            evidence explaining{" "}
            <em>
              &quot;what happened, what was decided, and how to do things,&quot;
            </em>{" "}
            stressing that records stay most useful when accurate and reachable
            at the moment another person needs them.
          </li>
          <li>
            <strong>
              Capture the medium too (Private Office 29 Dec 2023 guidance):
            </strong>{" "}
            Substantive decisions happen in person, on video calls, and in
            messaging threads alike—effective logs capture date, time, format,
            participants, and commitments regardless of where discussion
            started.
          </li>
          <li>
            <strong>
              Log reasoning and non-actions (UK HSE Key Decision Log):
            </strong>{" "}
            HSE operational guidance defines a key decision log cleanly as{" "}
            <em>&quot;not a diary of the actions taken,&quot;</em> emphasizing
            that teams must capture <em>inaction</em> and <em>retractions</em>{" "}
            right beside approvals, dated, timed, and written clearly enough for
            someone outside the room to follow.
          </li>
          <li>
            <strong>Retention discipline (Companies Act contrast):</strong>{" "}
            Statutory company rules under sections 248 and 355 expect formal
            board minutes and member resolutions to remain inspectable for at
            least ten years—underscoring why informal chat retention
            shouldn&apos;t serve as long-term institutional memory. (National
            Archives FOIA section 46 Code guidance from 15 July 2021 reinforces
            keeping authoritative records intact under clear stewardship.)
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Anti-patterns that rot team logs
        </h2>
        <p>
          <strong>Treating activity streams as decision logs.</strong> Commit
          feeds and ticket transitions tell you what code changed, rarely why an
          alternative design was rejected or postponed.
        </p>
        <p>
          <strong>Rewriting old notes when plans shift.</strong> Silent edits
          erase why earlier work was scoped the way it was. Use{" "}
          <TextLink href="/patterns/append-only-amendments-and-retractions">
            append-only amendments
          </TextLink>{" "}
          so both the prior baseline and the change notice stay readable.
          Related execution safety guides live in{" "}
          <TextLink href="https://azoa.ardanova.com/workflows">
            AZOA workflows
          </TextLink>{" "}
          (including fail-closed guards and settle-once semantics) and
          governance practice on{" "}
          <TextLink href="https://neos.primusneo.com/blog/act-is-not-a-vote">
            NEOS
          </TextLink>
          .
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          A distributed build group pauses a risky payment integration during a
          live voice call. Three weeks later, contributors disagree on whether
          they shelved the module permanently or postponed it pending security
          review. One dated two-line entry in the decision log settles the facts
          instantly—without implying that logging the pause automatically alters
          contract rights.
        </p>
      </section>
      <CitedSources slug="decision-logs" />
    </>
  ),

  "append-only-amendments-and-retractions": () => (
    <>
      <p className="text-lg leading-relaxed">
        Every team runs into the temptation to tidy history in place: a delivery
        date slips, a budget splits in half, or a resolution needs clearer
        wording—so someone opens the wiki page and edits last Tuesday&apos;s
        paragraph. Five minutes later it looks clean, and three months later
        nobody can verify what teams originally agreed before work started.
      </p>
      <p>
        Shared records work best when settled entries stay immutable and
        corrections move forward via explicit superseding rows.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          How append-only revision chains stay readable
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Preserve the base record untouched:</strong> Once a
            decision, agreement snapshot, or milestone sign-off is committed,
            lock its text and identifier rather than overwriting fields
            silently.
          </li>
          <li>
            <strong>Link forward via explicit correction events:</strong>{" "}
            Publish a new amendment or retraction entry pointing directly at the
            prior ID (`supersedes: rec_014`), recording author, timestamp, and
            concrete reason for the change—matching HSE guidance to log
            retractions and changes openly.
          </li>
          <li>
            <strong>Separate projection from history:</strong> Render the latest
            effective state cleanly at the top of the workspace UI while keeping
            a one-click history timeline showing every superseded state in
            sequence.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Why this prevents silent drift across tools
        </h2>
        <p>
          When external workflow engines or partner ledgers sync records over{" "}
          <TextLink href="https://azoa.ardanova.com/workflows">
            AZOA automated handoffs
          </TextLink>
          , referencing immutable record IDs ensures an automated step can
          verify exactly which version of terms triggered execution, even if
          terms evolved later.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          Mid-sprint, two partner teams agree to reduce QA scope on a demo feed
          so core shipping stabilizes first. Instead of editing last
          Monday&apos;s charter paragraph in place, the steward appends
          amendment `#19` noting the reduced scope and linking back to entry
          `#12`. Weeks later, a reviewer immediately sees both what was
          originally promised and who approved the change.
        </p>
      </section>
      <CitedSources slug="append-only-amendments-and-retractions" />
    </>
  ),

  "directors-minutes": () => (
    <>
      <p className="text-lg leading-relaxed">
        Leadership calls tend to move fast: three tactical choices get approved
        on screen share, someone promises to update a ticket, and everyone moves
        on to implementation. When no formal minutes are recorded afterward,
        those decisions vanish from institutional memory the moment people leave
        the room.
      </p>
      <p>
        Institutional corporate frameworks treat board record-keeping seriously
        for exactly that reason. Looking at statutory directors&apos; minute
        rules gives builders a sharp baseline for designing durable governance
        logs—while keeping clear that ArdaNova records are workspace tools, not
        legal advice or statutory filings for any specific company.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          The statutory baseline (Companies Act 2006 contrast)
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Mandatory capture &amp; ten-year retention:</strong> Under
            section 248 of the UK Companies Act 2006,{" "}
            <em>
              &quot;Every company must cause minutes of all proceedings at
              meetings of its directors to be recorded.&quot;
            </em>{" "}
            Subsection (2) adds plainly:{" "}
            <em>
              &quot;The records must be kept for at least ten years from the
              date of the meeting.&quot;
            </em>
          </li>
          <li>
            <strong>Default enforcement framing:</strong> Failure to record or
            retain those minutes is a statutory default offence by officers,
            framed in the Act as a summary conviction fine not exceeding{" "}
            <em>level 3 on the standard scale</em> plus a daily default fine not
            exceeding <em>one-tenth of level 3</em> for continued failure.
            (Those statutory scale phrases stand on their own—without converting
            standard-scale levels into currency estimates—so readers inspect the
            exact text cleanly.)
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Applying minute discipline without bureaucratic drag
        </h2>
        <p>
          Software teams aren&apos;t automatically governed by UK board law—yet
          the discipline transfers directly. Keep leadership decisions alongside{" "}
          <TextLink href="/patterns/records-of-resolutions">
            member resolution archives
          </TextLink>{" "}
          and capture decisions raised over messaging via{" "}
          <TextLink href="/patterns/informal-channels">
            informal-channel capture
          </TextLink>{" "}
          so ten years out—or ten months out—nobody guesses what happened.
          Remember also: recording a board vote inside a shared workspace does
          not, on its own, transfer funds or rewrite{" "}
          <TextLink href="/patterns/a-record-is-not-a-right">
            external contracts
          </TextLink>
          .
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          Project directors authorize a new vendor handoff during a busy Friday
          call, leaving only a passing note in chat. Years later, due diligence
          asks when that authorization cleared and who joined the call. Without
          dated minutes tied to the decision log, the group has nothing
          inspectable to point to.
        </p>
      </section>
      <CitedSources slug="directors-minutes" />
    </>
  ),

  "records-of-resolutions": () => (
    <>
      <p className="text-lg leading-relaxed">
        Not every member vote happens around a physical table. Teams approve
        changes via asynchronous written sign-offs, full general meetings, and
        single-steward determinations—and unless all three land in a structured
        archive, email approvals silently age out while meeting notes sit in
        private drives.
      </p>
      <p>
        Looking at how formal corporate legislation groups member resolutions
        highlights a clean architectural structure for Record B. Note that this
        guide explains structural record-keeping concepts and is not legal
        advice.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Three resolution streams that belong side by side
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>All three paths in one durable store:</strong> Under section
            355 of the UK Companies Act 2006, companies maintain records
            comprising (a){" "}
            <em>
              copies of all resolutions of members passed otherwise than at
              general meetings
            </em>
            , (b) <em>minutes of all proceedings of general meetings</em>, and
            (c) details of sole-member decisions supplied under section 357—kept
            for{" "}
            <em>
              at least ten years from the date of the resolution, meeting or
              decision (as appropriate)
            </em>
            .
          </li>
          <li>
            <strong>Consistent default framing:</strong> Non-compliance under
            s.355 mirrors s.248 (summary scale framing at level 3 plus a daily
            contravention cap at <em>one-tenth of level 3</em>); this page does
            not invent a pound figure beyond the statute&apos;s text.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Why builders model async + synchronous approvals identically
        </h2>
        <p>
          Whether a team signs off asynchronously in a proposal thread or votes
          live in a sprint review, both carry equal governance weight when
          stored with clear metadata in Record B. Pair this with{" "}
          <TextLink href="/patterns/directors-minutes">
            directors&apos; minutes
          </TextLink>{" "}
          and{" "}
          <TextLink href="/patterns/what-belongs-in-the-minutes">
            minimal meeting-note fields
          </TextLink>{" "}
          rather than letting written resolutions disappear into inbox archives.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          Contributors ratify a licensing update via an email poll rather than a
          live assembly. Two years later, a new maintainer looks at general
          meeting notes, sees no mention of the license change, and assumes the
          change was informal—because nobody archived async written resolutions
          beside live meeting records.
        </p>
      </section>
      <CitedSources slug="records-of-resolutions" />
    </>
  ),

  "what-belongs-in-the-minutes": () => (
    <>
      <p className="text-lg leading-relaxed">
        Few documents feel more useless six months later than a bullet list
        reading{" "}
        <em>&quot;Discussed vendor rollout; agreed on path forward.&quot;</em>{" "}
        Who attended? When did the call happen? What exact path was chosen—and
        who holds responsibility if something breaks?
      </p>
      <p>
        Writing useful minutes takes two minutes while context is warm, provided
        the structure forces four concrete fields onto the page every time. This
        engineering guide shares operational best practices, not legal advice.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          The non-negotiable minimum fields
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Date, time, medium, actors, and literal outcome:</strong> UK
            Private Office records guidance (29 December 2023) gives teams a
            remarkably clean operational template for substantive meetings
            across any medium:{" "}
            <em>
              &quot;Record the date, time, location or format (in person,
              online, via non-corporate channels), who took part, and decisions
              or undertakings.&quot;
            </em>{" "}
            Crucially, it adds:{" "}
            <em>
              &quot;Record decisions arising from telephone or online meetings
              or via non-corporate communication channels, such as
              WhatsApp.&quot;
            </em>
          </li>
          <li>
            <strong>Operational purpose (HMRC / ISO 15489-1:2016):</strong> Keep
            the note crisp enough that anyone can pull up{" "}
            <em>
              &quot;what happened, what was decided, and how to do things&quot;
            </em>{" "}
            accurately when needed later.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Keep format consistent, separate from payment chatter
        </h2>
        <p>
          Whether a workspace economy runs simulated bookkeeping or production
          milestones, capturing clear date/time/participants/outcome metadata
          never wastes effort. Pair this template directly with{" "}
          <TextLink href="/patterns/informal-channels">
            capturing decisions in chat
          </TextLink>{" "}
          and{" "}
          <TextLink href="/patterns/readable-record">
            writing for future readers
          </TextLink>{" "}
          across your{" "}
          <TextLink href="/patterns/decision-logs">decision logs</TextLink>.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          A team note states simply{" "}
          <em>&quot;aligned on Q3 budget split&quot;</em> without names,
          timestamps, or channel context. When two members argue over
          percentages next sprint, nobody can establish who attended or whether
          consensus even existed. Adding five concise header lines solves the
          ambiguity permanently.
        </p>
      </section>
      <CitedSources slug="what-belongs-in-the-minutes" />
    </>
  ),

  "readable-record": () => (
    <>
      <p className="text-lg leading-relaxed">
        Every team builds private shorthand:{" "}
        <em>&quot;Same setup as Tuesday; Maya handling fallout.&quot;</em> That
        shorthand works great for the three people sitting in the call—and turns
        completely opaque the week a new engineer onboards or someone steps back
        to audit an old choice.
      </p>
      <p>
        Writing a record that stays understandable six months later is a craft.
        Operational safety investigation logs already nail the exact design bar
        needed here (shared here as a practical pattern, not an inspection claim
        or legal advice).
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          What makes a decision log readable without tribal memory
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Decisions + reasons, not raw activity diaries:</strong> UK
            Health and Safety Executive (HSE) key decision log practice defines
            an effective log cleanly as{" "}
            <em>
              &quot;a contemporaneous record of the key decisions that affect
              the course of an investigation and the reasons for those
              decisions&quot;
            </em>
            —explicitly distinguishing it from a raw action diary (
            <em>&quot;is not a diary of the actions taken&quot;</em>).
          </li>
          <li>
            <strong>Log decisions not to act and reversals explicitly:</strong>{" "}
            <em>
              &quot;It is equally important to record decisions and reasons for
              not doing something as for doing something and to record any
              changes or retractions of previous decisions.&quot;
            </em>
          </li>
          <li>
            <strong>Timestamp immediately and write for outsiders:</strong>{" "}
            Entries must be logged promptly, dated and timed, and detailed
            enough to{" "}
            <em>
              &quot;enable someone who is not directly involved… to understand
              the thought processes&quot;
            </em>{" "}
            behind why the group took that road.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Putting the newcomer test into habit
        </h2>
        <p>
          Before closing a log entry, run one sanity check: could a skilled
          contributor joining next quarter read these four lines and understand
          why we said no to option B? Combine this with{" "}
          <TextLink href="/patterns/what-belongs-in-the-minutes">
            required meeting fields
          </TextLink>{" "}
          and{" "}
          <TextLink href="/patterns/append-only-amendments-and-retractions">
            append-only revisions
          </TextLink>{" "}
          so context stays durable.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          An entry reads simply{" "}
          <em>&quot;stick with old retry policy; DK to confirm.&quot;</em>{" "}
          Months later an outage hits that endpoint, and nobody knows whether an
          idempotency rollout was considered and rejected for safety reasons or
          simply forgotten. Writing two extra sentences explaining <em>why</em>{" "}
          preserves institutional engineering judgment.
        </p>
      </section>
      <CitedSources slug="readable-record" />
    </>
  ),

  "informal-channels": () => (
    <>
      <p className="text-lg leading-relaxed">
        Nobody waits for next Tuesday&apos;s scheduled committee call to resolve
        an urgent production blocker. Builders settle details over voice
        huddles, direct messages, and WhatsApp chats. The failure mode
        isn&apos;t using fast messaging—it is letting an informal chat app
        become the only place where an agreement lives.
      </p>
      <p>
        Good record-keeping treats informal channels realistically without
        invading privacy: this is a workflow habit about copying confirmed
        outcomes back into the workspace log—<strong>not legal advice</strong>,{" "}
        <strong>not a surveillance recommendation</strong>, and{" "}
        <strong>not a recommendation to search private devices</strong>.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Write the outcome down wherever the conversation happened
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>
              Capture outcomes across all channels (Private Office 29 Dec 2023):
            </strong>{" "}
            Public records guidance states clearly:{" "}
            <em>
              &quot;Record the date, time, location or format (in person,
              online, via non-corporate channels), who took part, and decisions
              or undertakings.&quot;
            </em>{" "}
            And specifically:{" "}
            <em>
              &quot;Record decisions arising from telephone or online meetings
              or via non-corporate communication channels, such as
              WhatsApp.&quot;
            </em>
          </li>
          <li>
            <strong>Formal duties never exempt side-channels:</strong> As seen
            alongside{" "}
            <TextLink href="/patterns/directors-minutes">
              directors&apos; minutes
            </TextLink>{" "}
            (Companies Act s.248) and{" "}
            <TextLink href="/patterns/records-of-resolutions">
              records of resolutions
            </TextLink>{" "}
            (s.355), governance obligations never include a &quot;decided over
            chat&quot; loophole. Neither section is stretched into a chat-app
            offence. Both simply confirm that real decisions belong in durable
            records.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A zero-friction handoff rule for chat threads
        </h2>
        <p>
          Treat WhatsApp, Discord, or Slack as scratchpad rooms and{" "}
          <TextLink href="/patterns/decision-logs">decision logs</TextLink> as
          the commitment layer—
          <strong>not as a reason to install a monitor</strong> or archive
          private banter. Once four people say &quot;ship plan B,&quot; one
          steward spends thirty seconds logging date, attendees, channel, and
          outcome using the checklist in{" "}
          <TextLink href="/patterns/what-belongs-in-the-minutes">
            what belongs in the minutes
          </TextLink>{" "}
          and{" "}
          <TextLink href="/patterns/readable-record">readable records</TextLink>
          .
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          Two co-leads agree on a pricing floor adjustment inside a group text
          on Saturday morning, forget to mirror it into Record B on Monday, and
          a month later discover everyone quoted conflicting baselines. Spending
          sixty seconds porting the confirmation into the shared log eliminates
          the drift immediately.
        </p>
      </section>
      <CitedSources slug="informal-channels" />
    </>
  ),

  "what-happened-what-was-decided": () => (
    <>
      <p className="text-lg leading-relaxed">
        Archiving gigabytes of unindexed message exports creates the illusion of
        thorough documentation without any actual usability. When something goes
        wrong six months later, wading through 4,000 lines of chat doesn&apos;t
        answer basic operational questions.
      </p>
      <p>
        What transforms noise into a working record comes down to three plain
        tests summarized neatly in public records standards—shared here as an
        engineering framework rather than legal advice or certification claims.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Three questions every real record answers
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Evidence in business transactions:</strong> HMRC&apos;s
            Records and Information Collection Policy (published 19 May 2026),
            quoting ISO 15489-1:2016, defines records as{" "}
            <em>
              &quot;information created, received and maintained as evidence and
              information by an organisation or person, in pursuance of legal
              obligations or in the transaction of business.&quot;
            </em>
          </li>
          <li>
            <strong>The practical three-part summary:</strong> HMRC adds that it
            keeps records explicitly{" "}
            <em>
              &quot;to provide information about what happened, what was
              decided, and how to do things,&quot;
            </em>{" "}
            noting this stays format-neutral across paper, digital, and hybrid
            repositories.
          </li>
          <li>
            <strong>Systemic control over ad-hoc hoarding:</strong> Quoting the
            same baseline, HMRC frames records management around the{" "}
            <em>
              &quot;efficient and systemic control of the creation, receipt,
              maintenance, use and disposition of records.&quot;
            </em>{" "}
            Notice the deliberate wording—<em>systemic</em> control beats
            frantic ad-hoc searching after an incident.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Designing clean schema separation
        </h2>
        <p>
          Inside a collaborative workspace, don&apos;t dump events, policies,
          and votes into a single text blob. Separate factual logs (
          <TextLink href="/patterns/what-belongs-in-the-minutes">
            what happened
          </TextLink>
          ), governance choices (
          <TextLink href="/patterns/decision-logs">what was decided</TextLink>
          ), and operating runbooks—paired cleanly with retention principles in{" "}
          <TextLink href="/patterns/tna-section-46">
            public-authority records management
          </TextLink>
          .
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          A shared folder named <em>&quot;Q4 Archive&quot;</em> contains fifty
          renamed PDFs and exported message dumps. When an auditor or new
          partner asks why a sprint milestone shifted, nobody can extract{" "}
          <em>what happened</em>, <em>what was decided</em>, or{" "}
          <em>how to repeat the handoff</em> without asking three founders on a
          call.
        </p>
      </section>
      <CitedSources slug="what-happened-what-was-decided" />
    </>
  ),

  "tna-section-46": () => (
    <>
      <p className="text-lg leading-relaxed">
        Engineering teams often swing between extremes: keeping zero written
        rationale, or hoarding every raw webhook log forever in hopes nothing is
        missed. Neither approach helps an operator locate trustworthy facts
        quickly.
      </p>
      <p>
        The National Archives&apos; July 2021 Code of Practice (issued under
        section 46 of the UK Freedom of Information Act 2000) lays out a clean
        three-principle model—<em>value, integrity, and accountability</em>—for
        deciding what deserves lasting record stewardship and when obsolete
        clutter can retire. Cited here strictly as institutional systems
        thinking (this is not legal advice and not a claim that ArdaNova is a
        relevant authority).
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Three engineering-friendly principles from the 15 July 2021 Code
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Not all information is a record:</strong> Published 15 July
            2021 (ISBN 978-1-5286-2517-3, 32-page PDF covering England, Northern
            Ireland, and Wales), the Code is built on the reminder that{" "}
            <em>&quot;not all information is a record&quot;</em> across any
            physical or digital format, separating ephemeral noise from durable
            evidence.
          </li>
          <li>
            <strong>Value, integrity, and accountability:</strong> Teams should{" "}
            <em>
              &quot;keep information as long as they can show it has value and
              dispose of it when it no longer has value,&quot;
            </em>{" "}
            protect integrity so stakeholders{" "}
            <em>&quot;can rely upon and trust the information,&quot;</em> and
            preserve accountability around regulatory needs, rights protection,
            and historical continuity.
          </li>
          <li>
            <strong>Operational scope &amp; glossary distinction:</strong> The
            Code guides statutory &quot;relevant authorities&quot; (with an
            editorial glossary citing BS ISO 15489-1:2016 around records kept{" "}
            <em>
              &quot;in pursuit of legal obligations or in the transaction of
              business&quot;
            </em>
            —explicitly noted as reference context that{" "}
            <em>does not form part of the Code itself</em>).
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Applying retention hygiene to software workspaces
        </h2>
        <p>
          Teams don&apos;t need FOIA statutory obligations to borrow its
          clarity: distinguish ephemeral chat logs from settled{" "}
          <TextLink href="/patterns/decision-logs">decision records</TextLink>,
          document{" "}
          <TextLink href="/patterns/what-happened-what-was-decided">
            what happened and why
          </TextLink>
          , and explicitly record why superseded drafts were archived.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          A team pitches its workspace as &quot;compliant with public-sector
          records standards&quot; while keeping chaotic shared drives and zero
          deletion or integrity trail. Calling out clear retention scope
          prevents hollow compliance theater and sets verifiable engineering
          habits.
        </p>
      </section>
      <CitedSources slug="tna-section-46" />
    </>
  ),

  "articles-of-incorporation": () => (
    <>
      <p className="text-lg leading-relaxed">
        Builders routinely bundle every organizational document into one generic
        &quot;governance&quot; folder—mixing baseline operating rules with daily
        stand-up notes and current contributor lists. When someone asks whether
        a team rule is permanent policy or just last Tuesday&apos;s quick
        consensus, nobody can tell without reading forty files.
      </p>
      <p>
        Separating standing governance rules (&quot;articles&quot;) from
        decision logs and membership registers solves that confusion from the
        start. Note: people often search &quot;articles of incorporation,&quot;
        whereas UK corporate law uses &quot;articles of association&quot;; we
        keep the common search slug while discussing both clearly—and this page
        is architectural contrast, not formation tooling or legal advice.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          How formal company architecture separates rules from events
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Prescribed operating rules (Companies Act s.18):</strong>{" "}
            <em>
              &quot;A company must have articles of association prescribing
              regulations for the company.&quot;
            </em>{" "}
            Unless default model articles apply cleanly under s.20, registered
            articles live inside a single document divided into consecutively
            numbered paragraphs.
          </li>
          <li>
            <strong>Constitution vs. minutes (s.17):</strong> A company&apos;s
            constitution includes both standing articles and specific formal
            resolutions/agreements—explicitly distinct from daily meeting
            proceedings.
          </li>
          <li>
            <strong>Contrast with the founding memorandum (s.8):</strong> A
            historical formation memorandum merely captures subscribers&apos;
            initial intent to incorporate (and, where relevant,{" "}
            <em>&quot;to take at least one share each&quot;</em>)—confirming why
            founding snapshots shouldn&apos;t be confused with evolving
            operational bylaws. (For completeness: a former Companies House
            model-articles landing link returned HTTP 404 on 2026-11-10 and is
            not linked here.)
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Keep bylaws, minutes, and rosters in three clean drawers
        </h2>
        <p>
          Inside a multi-party project, keep the governing agreement / charter
          separate from{" "}
          <TextLink href="/patterns/directors-minutes">board minutes</TextLink>,{" "}
          <TextLink href="/patterns/records-of-resolutions">
            written resolutions
          </TextLink>
          , and{" "}
          <TextLink href="/patterns/company-registers">
            member registers
          </TextLink>
          —preserving clean boundaries across{" "}
          <TextLink href="/patterns/two-records-one-agreement">
            two records and one agreement
          </TextLink>
          .
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          A repository folder labeled <em>&quot;charter&quot;</em> contains
          twelve unstructured call recaps and zero clear quorum rules or role
          definitions. Keeping standing regulations cleanly separated from
          chronological minutes guarantees anyone joining later finds the live
          rules immediately.
        </p>
      </section>
      <CitedSources slug="articles-of-incorporation" />
    </>
  ),

  "company-registers": () => (
    <>
      <p className="text-lg leading-relaxed">
        Whenever a collaborative group expands past five contributors, someone
        inevitably asks:{" "}
        <em>
          &quot;Wait—is Alex still an active voting member, or did they rotate
          off last spring?&quot;
        </em>{" "}
        Digging through old meeting notes to deduce active membership is
        brittle, slow, and prone to mistakes.
      </p>
      <p>
        That is why formal corporate frameworks split membership rosters into an
        explicit register separate from minutes, resolutions, and bylaws—offered
        here as clear system-design contrast rather than legal advice.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          What a dedicated register tracks cleanly (Companies Act baseline)
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Explicit active/inactive state (s.113):</strong>{" "}
            <em>
              &quot;Every company must keep a register of its members.&quot;
            </em>{" "}
            Under the current in-force text as verified 10 November 2026,
            subsection (2) records member names and addresses alongside entry
            dates and cessation dates (and subsection (3) adds holding details
            when a company has share capital).
          </li>
          <li>
            <strong>Clean versioning discipline on pending law:</strong> A later
            statutory annotation notes prospective wording for sections 113A and
            113B substituted on{" "}
            <em>
              &quot;26.10.2023 but only so far as it confers a power to make
              regulations or relates to the exercise of the power, otherwise
              prosp.&quot;
            </em>{" "}
            Honest documentation notes what applies today and does not write as
            if 113A and 113B are already the everyday duty.
          </li>
          <li>
            <strong>Distinct from minute books (s.1134):</strong> Under the
            Act&apos;s definition of &quot;company records,&quot; registers,
            accounting books, agreements, and minutes sit side by side as
            separate record types—never collapsed into one messy log.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          State projection vs. audit log in software design
        </h2>
        <p>
          Keep a normalized roster showing who holds active decision rights and
          when roles began or ended, alongside{" "}
          <TextLink href="/patterns/directors-minutes">board minutes</TextLink>,{" "}
          <TextLink href="/patterns/records-of-resolutions">
            resolutions
          </TextLink>
          , and{" "}
          <TextLink href="/patterns/articles-of-incorporation">
            articles
          </TextLink>
          —and never mistake a roster row on its own for{" "}
          <TextLink href="/patterns/a-record-is-not-a-right">
            enforceable title
          </TextLink>
          .
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          A team tries treating one chronological &quot;minute book&quot; as
          both its attendance diary and its current member directory. Every
          onboarding or departure forces someone to scan a hundred historical
          notes just to compute who currently holds a voice. Maintaining an
          explicit member register fixes the bug at the data model layer.
        </p>
      </section>
      <CitedSources slug="company-registers" />
    </>
  ),

  "sole-member-decisions": () => (
    <>
      <p className="text-lg leading-relaxed">
        Solo maintainers and single-member entities naturally skip writing
        notes:{" "}
        <em>
          &quot;Why write minutes if nobody else was in the meeting?&quot;
        </em>{" "}
        The trouble hits six months later when a second partner joins, an
        auditor reviews historical terms, or even the original builder forgets
        why an architecture lock was enacted.
      </p>
      <p>
        Even where only one decision-maker exists, durable workspaces log those
        calls explicitly. Looking at sole-member statutory rules makes the logic
        crystal clear (shared as structural context, not legal advice).
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Why single-steward decisions still belong in the written record
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Written notification duty (Companies Act s.357):</strong>{" "}
            Where a private company limited by shares or by guarantee has a sole
            member who takes a decision that could have been made in a general
            meeting, they must supply the company with written details of that
            decision (unless passed as a written resolution under the Act).
            Notably, subsection (5) keeps practical clarity: omitting the notice{" "}
            <em>does not affect the validity</em> of the decision itself, though
            default exposes officers to a summary scale fine (framed cleanly in
            the Act as <em>level 2 on the standard scale</em>, left exact
            without currency-conversion speculation).
          </li>
          <li>
            <strong>Why s.288 written resolutions appear nearby:</strong>{" "}
            Written resolutions follow a defined private-company procedure under
            Chapter 2 (with exceptions such as removing an auditor or director
            early), so s.357 prevents single stewards from bypassing
            record-keeping simply because no multi-person room convened.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Small habit, huge downstream payoff
        </h2>
        <p>
          When a project begins with one creator or steward, logging sole
          decisions alongside{" "}
          <TextLink href="/patterns/records-of-resolutions">
            resolution records
          </TextLink>{" "}
          and{" "}
          <TextLink href="/patterns/company-registers">
            member registers
          </TextLink>{" "}
          ensures clean handoffs the day a collaborator joins.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          A solo maintainer pivots a shared codebase&apos;s licensing terms and
          funding caps in month two—leaving zero note because there wasn&apos;t
          anyone else to meet with. When partner #2 joins in month eight, the
          early change looks arbitrary and undocumented. One concise entry in
          the decision log solves that continuity gap from day one.
        </p>
      </section>
      <CitedSources slug="sole-member-decisions" />
    </>
  ),

  "problem-intake": () => (
    <>
      <p className="text-lg leading-relaxed">
        Good engineering collaboration starts with the actual problem and the
        people living with it—before anyone spins up point tables, scorecards,
        or premature feature roadmaps.
      </p>
      <ul className="list-disc space-y-2 pl-5">
        <li>
          <strong>Discover:</strong> Name the concrete operational bottleneck
          and who encounters it directly.
        </li>
        <li>
          <strong>Define:</strong> Draft a verifiable plan and explicit
          agreement terms others can inspect before committing time.
        </li>
        <li>
          <strong>Iterate—or stop:</strong> Build, verify, revise, and allow a
          clean recorded stop when a path fails rather than pushing sunk costs
          forward silently.
        </li>
      </ul>
      <p>
        Logging a problem intake note starts alignment cleanly without creating
        payment obligations or transferring property.
      </p>
      <SourceList
        intro="Core operational loop documented across ArdaNova."
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
      <WorkspaceFunnelGate surface="pattern-detail" />
      <SimulationNotice includeAzoa includeLegalAdvice />
      <ReviewedLine />
    </>
  );
}
