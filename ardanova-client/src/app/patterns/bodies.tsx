import type { ReactNode } from "react";
import Link from "next/link";

import { RelatedLinks } from "~/components/brand/related-links";
import { SimulationNotice } from "~/components/brand/simulation-notice";
import { WorkspaceFunnelGate } from "~/components/brand/WorkspaceFunnelGate";
import {
  patternBySlug,
  patternsReviewedAt,
  type PatternSlug,
} from "~/lib/writing";

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
      Author: Escherbridge · Last reviewed: {patternsReviewedAt},
      America/Denver.
    </p>
  );
}

const bodies: Record<PatternSlug, () => ReactNode> = {
  "a-record-is-not-a-right": () => (
    <>
      <p className="text-lg leading-relaxed">
        People building something meaningful together rightly look for stability
        on screen: a stewardship tally updates, a project share column climbs,
        or a member roster adds new neighbors. Left unexamined, it becomes far
        too easy to mistake those interface figures for self-executing property
        title, voting authority, or guaranteed payouts.
      </p>
      <p>
        Honest commons do not run on illusions. Written records prove what
        participants logged and agreed to—not whether an enforceable legal right
        exists independent of the underlying contract. Even in UK corporate
        practice, which subjects registers and minute books to strict statutory
        duties, written registers serve as <em>evidence</em> of entries and
        proceedings rather than automatic legal title on their own.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          What shared stewardship memory establishes—and where it stops
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
            was inserted—rebuttable if reality or external contracts contradict
            the entry.
          </li>
          <li>
            <strong>Minutes capture proceedings, not asset transfers:</strong>{" "}
            Directors must record minutes of all board meetings and preserve
            them:{" "}
            <em>
              &quot;The records must be kept for at least ten years from the
              date of the meeting.&quot;
            </em>{" "}
            Authenticated minutes show a gathering occurred and commitments were
            recorded; they never transfer property across borders by magic. (Ten
            years reflects a UK statutory corporate baseline, cited as
            institutional contrast rather than a software guarantee.)
          </li>
          <li>
            <strong>Clear operational purpose:</strong> HMRC&apos;s 19 May 2026
            Records Policy, quoting ISO 15489-1:2016, describes a record plainly
            as evidence maintained in real activity—built to document{" "}
            <em>
              &quot;what happened, what was decided, and how to do things.&quot;
            </em>
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Common pitfalls when software tallies masquerade as title
        </h2>
        <p>
          <strong>
            Confusing workspace unit tallies with enforceable pay.
          </strong>{" "}
          Unit balances and contribution shares inside a shared project track
          agreed effort inside that community boundary—they never substitute for
          a signed written agreement or external payment settlement system.
        </p>
        <p>
          <strong>
            Assuming a recorded community vote moves funds itself.
          </strong>{" "}
          A democratic vote captured in software states shared intent cleanly;
          on its own, it does not rewrite legal agreements, alter external
          incorporation status, or move property automatically.
        </p>
        <p>
          <strong>
            Conflating outside corporate statutes with a commons tool.
          </strong>{" "}
          UK register duties (including later prospective amendments tied to the
          Economic Crime and Corporate Transparency Act 2023) illustrate honest
          evidential boundaries—without turning US-operated software into
          statutory registries in Idaho or elsewhere.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          How to keep shared ownership honest from day one
        </h2>
        <ul className="list-disc space-y-2 pl-5">
          <li>
            Preserve work contributed, agreed value shares, and collective
            decisions in clear, dated rows that survive seasonal turnover.
          </li>
          <li>
            Point every shared-value screen back to a plain, readable community
            agreement under applicable law rather than letting a badge imply
            title.
          </li>
          <li>
            Label sandbox and simulated bookkeeping plainly beside unit totals
            so no contributor or local sponsor confuses an exercise with live
            legal title.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          Two regional building guilds spend two seasons fabricating solar
          micro-shelter frames under a shared problem charter. Months later, a
          community sponsor asks whether unit totals on screen trigger an
          automatic payout. The stewardship record settles{" "}
          <em>
            what craft hours and materials were contributed and recognized
          </em>
          ; the underlying signed partnership agreement settles{" "}
          <em>what compensation is owed and when</em>.
        </p>
      </section>
      <CitedSources slug="a-record-is-not-a-right" />
    </>
  ),

  "two-records-one-agreement": () => (
    <>
      <p className="text-lg leading-relaxed">
        Every genuine cooperative project balances two utterly different truths:
        who contributed labor, tools, or community funds (and how resulting
        value is shared), and who holds equal democratic voice in steering what
        happens next. Most software mashes both into one numeric leaderboard—and
        quietly hands community control to whoever brings the biggest check.
      </p>
      <p>
        When money contributed doubles as voting weight, wealthy sponsors slowly
        silence daily stewards. Conversely, when a snap assembly vote erases
        previously recognized work, skilled builders walk away. Splitting
        Stewardship &amp; Shared Value (Record A) from Community Voice &amp;
        Governance (Record B) lets both forms of fairness stand visibly side by
        side.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Why cooperatives proved this boundary more than a century ago
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Jointly owned, democratically steered:</strong> The
            International Cooperative Alliance&apos;s 1995 Statement on the
            Cooperative Identity grounds healthy commons around two twin
            pillars:{" "}
            <em>&quot;jointly-owned and democratically-controlled.&quot;</em>
          </li>
          <li>
            <strong>Principle 2 defends one member, one vote:</strong> In
            primary cooperatives, governance follows{" "}
            <em>&quot;one member, one vote&quot;</em> specifically so collective
            direction stays rooted in the community affected rather than ledger
            balance size. (ArdaNova reflects both patterns clearly without
            claiming formal ICA cooperative status.)
          </li>
          <li>
            <strong>
              Principle 3 honors equitable economic participation:
            </strong>{" "}
            Members contribute fairly, steward common wealth responsibly, and
            share agreed proceeds transparently without converting capital into
            political dominance.
          </li>
          <li>
            <strong>
              Principle 4 shields autonomy when outside funds join:
            </strong>{" "}
            Community sponsorship enters only on explicit terms that preserve
            democratic governance rather than hollowing it out.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Record A, Record B, and the agreement holding both together
        </h2>
        <p>
          <strong>Record A (Stewardship, Work &amp; Shared Value):</strong>{" "}
          Tracks craft contributed, material funds offered, agreed value shares,
          and reconciliation state so everyone sees honestly how shared value
          rests.
        </p>
        <p>
          <strong>Record B (Community Voice &amp; Governance):</strong> Tracks
          proposals, delegated stewardship circles, approval rules, and vote
          outcomes completely apart from financial tallies.
        </p>
        <p>
          <strong>The agreement between them:</strong> A clear human covenant
          states how contributions logged in Record A interact with choices
          ratified in Record B—keeping interfaces out of the business of
          guessing community intent.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Where collapsed scoreboards hurt real communities
        </h2>
        <p>
          <strong>Letting capital multiply votes silently.</strong> Turning
          larger financial contributions into louder assembly sway alienates the
          very neighbors living with the ecological or civic outcome.
        </p>
        <p>
          <strong>Using governance votes to erase prior stewardship.</strong> A
          collective choice to pivot belongs in Record B; already recognized
          hours or material stakes in Record A shouldn&apos;t disappear silently
          because direction evolved.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          A civic sponsor joins halfway through a neighborhood food-shed
          project, contributes three times the early cash pool, and expects
          triple voting power in Saturday&apos;s assembly. Splitting Record A
          and Record B makes the compact transparent from day one: agreed value
          shares live cleanly in the economic ledger, while democratic
          governance stays <em>one member, one voice</em> in Record B.
        </p>
      </section>
      <CitedSources slug="two-records-one-agreement" />
    </>
  ),

  "reconciling-two-ledgers-without-a-central-custodian": () => (
    <>
      <p className="text-lg leading-relaxed">
        Social ecology works through federation: autonomous guilds, workshops,
        and regional chapters cooperating around shared watershed or civic goals
        without handing their independence to one central landlord platform. Yet
        the moment Workshop A keeps its stewardship notebook and Coalition B
        tracks shared funds across town, quiet drift begins unless both keep
        lightweight shared anchors.
      </p>
      <p>
        Waiting until year-end reconciliation guarantees suspicion and exhausted
        volunteers. Federated commons stay aligned when small causal anchors sit
        inside commitments from week one rather than frantic repairs months
        later.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Three practices that keep federated commons aligned fairly
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Shared agreement &amp; causal entry IDs:</strong> Every
            recorded contribution on either side names the explicit shared
            agreement identifier plus a clear creator sequence key, preventing
            clashes when offline field work syncs later.
          </li>
          <li>
            <strong>Open periodic checkpoint summaries:</strong> At natural
            season or milestone boundaries, partner guilds exchange a compact
            summary of entries logged, settled unit totals, and checksum
            digests—so differences surface immediately where they began.
          </li>
          <li>
            <strong>Open exception resolution in Record B:</strong> When
            independent checks spot a discrepancy—a split harvest deliverable or
            a revised workshop budget—neither side overwrites the other quietly.
            Both log an open notice settled through shared{" "}
            <TextLink href="/patterns/decision-logs">decision logs</TextLink>.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          How Record A and safe workflow handoffs cooperate
        </h2>
        <p>
          Pairing ArdaNova&apos;s split stewardship model with idempotent
          execution safeguards in{" "}
          <TextLink href="https://azoa.ardanova.com/workflows">
            AZOA workflow specifications
          </TextLink>{" "}
          keeps retried automated syncs from double-counting deliverables across
          partner commons. Every community retains autonomy over its own domain
          while honoring shared receipts.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          A volunteer solar-repair guild logs twelve installation hours the same
          week neighborhood sponsors divide a shed build into two seasonal
          phases. Because both sides tie entries to one explicit agreement ID
          and exchange Friday summaries, the unlinked entry shows up right away
          and resolves cleanly through an open amendment instead of breeding
          resentment next spring.
        </p>
      </section>
      <CitedSources slug="reconciling-two-ledgers-without-a-central-custodian" />
    </>
  ),

  "decision-logs": () => (
    <>
      <p className="text-lg leading-relaxed">
        Six weeks after a lively community gathering, two stewards walk away
        with opposite memories of what the group decided. Chat history holds
        forty overlapping replies and three ambiguous heart reactions, with zero
        clarity on whether a genuine objection actually carried. Without honest
        decision memory, a commons loses democratic accountability to whoever
        speaks loudest afterward.
      </p>
      <p>
        Good decision memory is unglamorous and empowering: capture what was
        decided, why, by whom, and when right as consensus settles—including
        deliberate choices <em>not</em> to act and transparent reversals later.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          What resilient institutional records capture cleanly
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Actionable clarity (HMRC / ISO 15489-1:2016):</strong>{" "}
            HMRC&apos;s 19 May 2026 policy frames shared records as practical
            evidence explaining{" "}
            <em>
              &quot;what happened, what was decided, and how to do things,&quot;
            </em>{" "}
            accessible at the moment another person joins the work.
          </li>
          <li>
            <strong>
              Capture the setting too (Private Office 29 Dec 2023 guidance):
            </strong>{" "}
            Real agreements emerge on porches, community video calls, and text
            threads alike—durable notes log date, time, medium, participants,
            and commitments regardless of where people met.
          </li>
          <li>
            <strong>
              Honor non-actions and course reversals (UK HSE Key Decision Log):
            </strong>{" "}
            HSE operational guidance defines a key decision log cleanly as{" "}
            <em>&quot;not a diary of the actions taken,&quot;</em> underscoring
            why teams must preserve <em>inaction</em> and <em>retractions</em>{" "}
            with reasoning clear enough for any newcomer outside the room.
          </li>
          <li>
            <strong>Long-horizon memory (Companies Act contrast):</strong>{" "}
            Statutory company rules under sections 248 and 355 expect formal
            board minutes and member resolutions to stay inspectable for at
            least ten years—a sharp reminder that disappearing chat apps cannot
            substitute for collective memory. (National Archives FOIA section 46
            Code guidance from 15 July 2021 reinforces keeping shared memory
            intact under clear stewardship.)
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Habits that erode trust in a commons
        </h2>
        <p>
          <strong>Mistaking activity streams for shared decisions.</strong> Task
          feeds show surface motion, rarely why a community rejected an option
          that risked downstream harm.
        </p>
        <p>
          <strong>Silently rewriting old commitments when plans shift.</strong>{" "}
          Secret edits destroy context. Use{" "}
          <TextLink href="/patterns/append-only-amendments-and-retractions">
            open append-only revisions
          </TextLink>{" "}
          so both the earlier covenant and the community&apos;s updated
          reasoning stay visible. Related workflow execution guards live in{" "}
          <TextLink href="https://azoa.ardanova.com/workflows">
            AZOA workflows
          </TextLink>{" "}
          and governance writing on{" "}
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
          During an evening community call, stewards pause a risky neighborhood
          tool-lending expansion pending liability insurance verification. Three
          weeks later, incoming volunteers debate whether the effort was
          permanently dropped or paused on purpose. Two honest dated lines in
          the decision log settle reality instantly—without claiming logging the
          pause alters outside agreements on its own.
        </p>
      </section>
      <CitedSources slug="decision-logs" />
    </>
  ),

  "append-only-amendments-and-retractions": () => (
    <>
      <p className="text-lg leading-relaxed">
        When plans change mid-project—material prices climb, neighbors spot an
        unintended ecological ripple, or roles shift across seasons—it is deeply
        tempting to open last month&apos;s charter document and tidy the words
        in place. The page looks spotless for ten minutes, and six months later
        newcomers have zero trace of what compromises shaped today&apos;s text.
      </p>
      <p>
        Solutionary learning demands humility in public: settled agreements stay
        intact as a record of what everyone understood then, while honest
        revisions move forward as dated amendments anyone can trace.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          How open revision timelines protect trust
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Leave settled commitments readable:</strong> Once the
            community ratifies a proposal or agreement snapshot, freeze its text
            and identifier rather than erasing it quietly.
          </li>
          <li>
            <strong>Link forward via open correction notices:</strong> Publish a
            dated amendment or retraction citing the prior ID (
            <code>supersedes: rec_014</code>), naming who joined the update and
            why—honoring HSE practice to preserve retractions in full view.
          </li>
          <li>
            <strong>Show present clarity above living history:</strong> Surface
            the current effective covenant at the top while keeping the full
            revision series one tap away for any incoming member.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Keeping automated workflows honest alongside community revisions
        </h2>
        <p>
          When companion tools like{" "}
          <TextLink href="https://azoa.ardanova.com/workflows">
            AZOA automated handoffs
          </TextLink>{" "}
          sync milestone approvals across guilds, referencing stable record IDs
          lets anyone verify which agreed version governed an action even after
          the community evolved its rules.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          Halfway through setting up a community seed-exchange library, two
          partner guilds narrow first-season distribution so local soil testing
          finishes cleanly before wider sharing. Instead of erasing last
          month&apos;s charter paragraph, stewards append amendment{" "}
          <code>#19</code> explaining the safety step and linking back to entry{" "}
          <code>#12</code>. Anyone joining next year sees immediately how
          careful iteration worked in practice.
        </p>
      </section>
      <CitedSources slug="append-only-amendments-and-retractions" />
    </>
  ),

  "directors-minutes": () => (
    <>
      <p className="text-lg leading-relaxed">
        When a small circle of elected stewards coordinates day-to-day execution
        for a wider cooperative, calls move quickly: someone authorizes tool
        repairs, updates safety check procedures, and promises to inform members
        later. Whenever those commitments go unrecorded, accountability quietly
        slips out of the community&apos;s hands.
      </p>
      <p>
        Even traditional corporate legislation enforces strict minute-book
        duties precisely to stop executive amnesia. Reading those statutory
        rules alongside community commons practice illuminates clear design
        habits—while keeping clear that ArdaNova notes serve collective memory,
        never legal advice or statutory filings for any specific entity.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Institutional contrast (Companies Act 2006 baseline)
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Mandatory capture &amp; ten-year retention:</strong> Under
            section 248 of the UK Companies Act 2006,{" "}
            <em>
              &quot;Every company must cause minutes of all proceedings at
              meetings of its directors to be recorded.&quot;
            </em>{" "}
            Subsection (2) states:{" "}
            <em>
              &quot;The records must be kept for at least ten years from the
              date of the meeting.&quot;
            </em>
          </li>
          <li>
            <strong>Default enforcement framing:</strong> Omitting or losing
            required minutes exposes corporate officers to summary default
            penalties framed at <em>level 3 on the standard scale</em> plus a
            daily default fine up to <em>one-tenth of level 3</em>. (Those
            statutory scale phrases remain exact without inventing currency
            conversions.)
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Keeping delegated circles accountable without red tape
        </h2>
        <p>
          Voluntary collectives and neighborhood commons don&apos;t need rigid
          bureaucracy to uphold that standard of honesty. Store stewardship
          circle minutes beside{" "}
          <TextLink href="/patterns/records-of-resolutions">
            wider community resolutions
          </TextLink>{" "}
          and capture decisions raised in casual threads via{" "}
          <TextLink href="/patterns/informal-channels">
            informal-channel capture
          </TextLink>{" "}
          so nobody asks a year from now who authorized an agreement. Remember
          too: recording a decision inside software never replaces signed{" "}
          <TextLink href="/patterns/a-record-is-not-a-right">
            written covenants
          </TextLink>{" "}
          under real law.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          Stewardship circle coordinators authorize a shared land-access
          insurance carrier during an afternoon call and forget to log it beyond
          a passing message. Two harvests later, neighbors audit policy choices
          and find zero trace of who approved the terms or which questions were
          raised. One concise dated minute prevents institutional drift.
        </p>
      </section>
      <CitedSources slug="directors-minutes" />
    </>
  ),

  "records-of-resolutions": () => (
    <>
      <p className="text-lg leading-relaxed">
        Genuine participation cannot require every neighbor or guild member to
        sit around a Tuesday night table. People working night shifts, caring
        for elders, or building across regions often participate through
        asynchronous written consent, periodic general assemblies, and solo
        early steward decisions—and all three voices vanish unless archived side
        by side.
      </p>
      <p>
        Examining how formal company law places all three decision streams on
        equal archival footing clarifies how Record B honors busy communities
        equally. (This guide explores organizational patterns, not legal
        advice.)
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Three ways collective consent gets recorded fairly
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Unified visibility across paths:</strong> Under section 355
            of the UK Companies Act 2006, company archives bring together (a){" "}
            <em>
              copies of all resolutions of members passed otherwise than at
              general meetings
            </em>
            , (b) <em>minutes of all proceedings of general meetings</em>, and
            (c) sole-member notices under section 357—held for{" "}
            <em>
              at least ten years from the date of the resolution, meeting or
              decision (as appropriate)
            </em>
            .
          </li>
          <li>
            <strong>Consistent default baseline:</strong> Non-compliance under
            s.355 mirrors s.248 (summary scale framing at level 3 plus a daily
            contravention cap at <em>one-tenth of level 3</em>), expressed
            directly in the Act&apos;s statutory scale.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Honoring async voices equally with live assemblies
        </h2>
        <p>
          Whether neighbors sign a written proposal asynchronously over the
          weekend or vote live in an assembly, treating both equally in Record B
          keeps democratic voice accessible across work and family lives. Pair
          this alongside{" "}
          <TextLink href="/patterns/directors-minutes">
            stewardship circle minutes
          </TextLink>{" "}
          and{" "}
          <TextLink href="/patterns/what-belongs-in-the-minutes">
            simple meeting fields
          </TextLink>{" "}
          so written resolutions never sink inside scattered inboxes.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          Co-op members approve an open-source commons license change via an
          accessible five-day written ballot because shift workers couldn&apos;t
          attend mid-week. Two years later, someone checking live assembly notes
          finds no vote recorded and assumes an unapproved change—because nobody
          shelved written consent beside live meeting history.
        </p>
      </section>
      <CitedSources slug="records-of-resolutions" />
    </>
  ),

  "what-belongs-in-the-minutes": () => (
    <>
      <p className="text-lg leading-relaxed">
        Few documents feel emptier a year later than a community note reading{" "}
        <em>
          &quot;Good conversation about shed budget; aligned on next
          steps.&quot;
        </em>{" "}
        Who actually joined? When was the gathering held? What concrete path did
        people approve—and who committed to carry it?
      </p>
      <p>
        Clear community memory takes two minutes while chairs are still warm if
        the habit puts four plain facts on the page every time. (Shared as a
        practical commons habit, not legal advice.)
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Four simple fields every gathering deserves
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Date, time, setting, people, and literal outcome:</strong>{" "}
            UK Private Office guidance (29 December 2023) captures a crisp
            template usable in any neighborhood hall or video circle:{" "}
            <em>
              &quot;Record the date, time, location or format (in person,
              online, via non-corporate channels), who took part, and decisions
              or undertakings.&quot;
            </em>{" "}
            Adding:{" "}
            <em>
              &quot;Record decisions arising from telephone or online meetings
              or via non-corporate communication channels, such as
              WhatsApp.&quot;
            </em>
          </li>
          <li>
            <strong>Real utility (HMRC / ISO 15489-1:2016):</strong> Write
            plainly enough that anyone checking months later immediately sees{" "}
            <em>
              &quot;what happened, what was decided, and how to do things&quot;
            </em>
            .
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Light structure that protects volunteer time
        </h2>
        <p>
          Whether a workspace runs simulated exercises or live neighborhood
          stewardship, taking two minutes for date, participants, and explicit
          commitments prevents weeks of friction later. Pair this directly with{" "}
          <TextLink href="/patterns/informal-channels">
            bringing informal chat decisions home
          </TextLink>{" "}
          and{" "}
          <TextLink href="/patterns/readable-record">
            writing for future stewards
          </TextLink>{" "}
          inside your{" "}
          <TextLink href="/patterns/decision-logs">decision logs</TextLink>.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          A Saturday planning summary says only{" "}
          <em>&quot;agreed on autumn tool budget split&quot;</em> without names,
          timestamps, or format. When two guilds remember opposite shares next
          month, nobody can tell who attended or whether real consent occurred.
          Four short header lines cure that drift before it begins.
        </p>
      </section>
      <CitedSources slug="what-belongs-in-the-minutes" />
    </>
  ),

  "readable-record": () => (
    <>
      <p className="text-lg leading-relaxed">
        Every tight-knit group falls into cozy shorthand:{" "}
        <em>&quot;Same plan as Thursday; Sam taking care of the rest.&quot;</em>{" "}
        Those ten words make total sense to four friends in a kitchen—and turn a
        shared commons into a locked insiders&apos; club the week a new neighbor
        wants to help.
      </p>
      <p>
        Social ecology requires legible institutions: shared memory written well
        enough that anyone affected can step in six months later, read how a
        choice was made, and contribute as an equal peer. (Shared here as
        commons practice, not inspection guidance or legal advice.)
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          How accessible memory dissolves insider gatekeeping
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Reasons alongside choices, not raw logs:</strong> UK Health
            and Safety Executive (HSE) key decision log practice defines a true
            decision log cleanly as{" "}
            <em>
              &quot;a contemporaneous record of the key decisions that affect
              the course of an investigation and the reasons for those
              decisions&quot;
            </em>
            —explicitly noting it{" "}
            <em>&quot;is not a diary of the actions taken&quot;</em>.
          </li>
          <li>
            <strong>Capture conscious refusals and shifts honestly:</strong>{" "}
            <em>
              &quot;It is equally important to record decisions and reasons for
              not doing something as for doing something and to record any
              changes or retractions of previous decisions.&quot;
            </em>
          </li>
          <li>
            <strong>Write clearly for people not in the room:</strong> Log
            timely, dated reasoning clear enough to{" "}
            <em>
              &quot;enable someone who is not directly involved… to understand
              the thought processes&quot;
            </em>{" "}
            behind the community&apos;s choice.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          The six-month newcomer invitation test
        </h2>
        <p>
          Before saving a shared decision, ask one respectful question: could a
          neighbor joining next season read this note and grasp why we ruled out
          option B for safety or ecological harm? Combine this habit with{" "}
          <TextLink href="/patterns/what-belongs-in-the-minutes">
            four simple meeting fields
          </TextLink>{" "}
          and{" "}
          <TextLink href="/patterns/append-only-amendments-and-retractions">
            open revision histories
          </TextLink>
          .
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          An archive note states tersely{" "}
          <em>&quot;kept existing creek buffer; Morgan to follow up.&quot;</em>{" "}
          Six months later, volunteers propose mowing closer to the bank,
          unaware that biologists ruled it out to protect frog habitat. Two
          extra sentences capturing <em>why</em> keep hard-won community wisdom
          alive.
        </p>
      </section>
      <CitedSources slug="readable-record" />
    </>
  ),

  "informal-channels": () => (
    <>
      <p className="text-lg leading-relaxed">
        Communities don&apos;t wait for monthly town halls to solve urgent
        practical problems. Neighbors figure out tool deliveries on front
        porches, voice calls, and WhatsApp group chats. The trap isn&apos;t fast
        conversation—it is letting an informal chat thread stay the sole keeper
        of an agreement the wider group relies on.
      </p>
      <p>
        Honest stewardship welcomes casual coordination while fiercely refusing
        workplace spying: this pattern is solely a voluntary habit for copying
        agreed commitments back into shared space—
        <strong>not legal advice</strong>,{" "}
        <strong>not a surveillance recommendation</strong>, and{" "}
        <strong>not a recommendation to search private devices</strong>.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Honor conversation privacy while preserving community agreements
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Mirror confirmed choices across settings:</strong> Public
            records guidance (Private Office 29 December 2023) instructs simply:{" "}
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
            <strong>
              Good stewardship never hides behind informal venues:
            </strong>{" "}
            Seen beside{" "}
            <TextLink href="/patterns/directors-minutes">
              stewardship circle minutes
            </TextLink>{" "}
            (Companies Act s.248) and{" "}
            <TextLink href="/patterns/records-of-resolutions">
              resolutions
            </TextLink>{" "}
            (s.355), accountability doesn&apos;t create a &quot;decided over
            text message&quot; loophole—without ever turning chat apps into
            monitored spaces. Neither section is stretched into a chat-app
            offence.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A thirty-second voluntary bridge
        </h2>
        <p>
          Treat casual group threads as human living rooms and{" "}
          <TextLink href="/patterns/decision-logs">decision logs</TextLink> as
          shared commons memory—
          <strong>not as a reason to install a monitor</strong> or scrape
          private talk. Whenever people confirm a shared commitment, one
          participant posts date, actors, medium, and decision clearly via{" "}
          <TextLink href="/patterns/what-belongs-in-the-minutes">
            essential note fields
          </TextLink>{" "}
          and{" "}
          <TextLink href="/patterns/readable-record">
            accessible writing
          </TextLink>
          .
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          Two tool-library stewards adjust weekend checkout hours over Saturday
          text messages so shift workers can pick up ladders, then forget to
          update the shared bulletin. Neighbors show up to locked doors Monday
          evening. Thirty seconds copying the agreed schedule into the open log
          respects everyone&apos;s privacy and keeps the commons reliable.
        </p>
      </section>
      <CitedSources slug="informal-channels" />
    </>
  ),

  "what-happened-what-was-decided": () => (
    <>
      <p className="text-lg leading-relaxed">
        Dumping thousands of raw chat messages into a shared folder looks
        transparent on the surface while drowning volunteers in noise. Six
        months later, nobody looking for why a neighborhood project changed
        course wants to scroll past 4,000 lines of greetings and scheduling
        chatter.
      </p>
      <p>
        Public stewardship standards distill real shared memory down to three
        human questions—offered here as clear organizing design rather than
        legal advice or compliance theater.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Three plain questions living memory answers
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Evidence grounded in real activity:</strong> HMRC&apos;s
            Records and Information Collection Policy (19 May 2026), quoting ISO
            15489-1:2016, describes records as{" "}
            <em>
              &quot;information created, received and maintained as evidence and
              information by an organisation or person, in pursuance of legal
              obligations or in the transaction of business.&quot;
            </em>
          </li>
          <li>
            <strong>The three-part heart of transparency:</strong> Those records
            exist{" "}
            <em>
              &quot;to provide information about what happened, what was
              decided, and how to do things,&quot;
            </em>{" "}
            regardless of whether a group uses paper notebooks or digital tools.
          </li>
          <li>
            <strong>Intentional care over chaotic hoarding:</strong> Following
            the{" "}
            <em>
              &quot;efficient and systemic control of the creation, receipt,
              maintenance, use and disposition of records&quot;
            </em>{" "}
            means intentionally organizing shared facts instead of hoarding
            every stray utterance forever.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Keeping events, choices, and playbooks clear
        </h2>
        <p>
          Inside a community workspace, don&apos;t flatten field notes, votes,
          and practical guides into one endless wall. Separate field events (
          <TextLink href="/patterns/what-belongs-in-the-minutes">
            what happened
          </TextLink>
          ), shared commitments (
          <TextLink href="/patterns/decision-logs">what was decided</TextLink>
          ), and how-to craft guides alongside thoughtful retention principles
          in{" "}
          <TextLink href="/patterns/tna-section-46">
            keeping what holds value
          </TextLink>
          .
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          A shared folder labeled <em>&quot;Watershed Archive&quot;</em> holds
          fifty unnamed exports and raw message dumps. When incoming neighbors
          ask <em>what happened</em> on soil testing, <em>what was decided</em>{" "}
          about pesticide-free boundaries, or <em>how to test water samples</em>
          , they are stuck tracking down three busy founders unless memory sits
          in clear layers.
        </p>
      </section>
      <CitedSources slug="what-happened-what-was-decided" />
    </>
  ),

  "tna-section-46": () => (
    <>
      <p className="text-lg leading-relaxed">
        Healthy collectives avoid two opposite traps: keeping zero written
        memory so tribal cliques hold power, or obsessively hoarding every stray
        message forever in a surveillance archive nobody can bear reading.
        Respecting human privacy means letting clutter retire while protecting
        what holds lasting value.
      </p>
      <p>
        The National Archives&apos; July 2021 Code of Practice (under section 46
        of the UK Freedom of Information Act 2000) outlines three humane
        pillars—
        <em>value, integrity, and accountability</em>—worth borrowing in
        cooperative design. (Cited strictly as institutional design inspiration,
        not legal advice and not a claim that ArdaNova is a relevant authority.)
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Three principles from the 15 July 2021 Code
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Not every message deserves permanence:</strong> Published 15
            July 2021 (ISBN 978-1-5286-2517-3, 32-page PDF covering England,
            Northern Ireland, and Wales), the Code opens simply:{" "}
            <em>&quot;not all information is a record&quot;</em> across paper or
            digital space—protecting commons from noise hoarding.
          </li>
          <li>
            <strong>Value, integrity, and accountability:</strong> Keep shared
            material only{" "}
            <em>
              &quot;as long as they can show it has value and dispose of it when
              it no longer has value,&quot;
            </em>{" "}
            safeguard integrity so members{" "}
            <em>&quot;can rely upon and trust the information,&quot;</em> and
            honor real accountability toward neighbors affected.
          </li>
          <li>
            <strong>Scope honesty:</strong> While its editorial glossary notes
            BS ISO 15489-1:2016 around material kept{" "}
            <em>
              &quot;in pursuit of legal obligations or in the transaction of
              business&quot;
            </em>
            , the Code clarifies explicitly that glossary context{" "}
            <em>does not form part of the Code itself</em>.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Stewardship memory without hoarding personal chatter
        </h2>
        <p>
          Cooperative workshops can keep casual conversation light and temporary
          while preserving settled{" "}
          <TextLink href="/patterns/decision-logs">decision memory</TextLink>,{" "}
          <TextLink href="/patterns/what-happened-what-was-decided">
            shared outcomes
          </TextLink>
          , and open disposal notes when old seasonal drafts are retired.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          A group boasts about &quot;total archival permanence&quot; while
          keeping years of messy personal chat exports mixed together with
          active bylaws. Deliberately retiring noisy chatter while anchoring
          true agreements builds real accountability without surveillance bloat.
        </p>
      </section>
      <CitedSources slug="tna-section-46" />
    </>
  ),

  "articles-of-incorporation": () => (
    <>
      <p className="text-lg leading-relaxed">
        Communities starting a new initiative often toss everything—core
        democratic principles, Tuesday&apos;s meeting recap, and who volunteered
        this summer—into one shared &quot;charter&quot; folder. Months later, a
        new member asks whether a quorum norm is bedrock collective policy or
        just one casual afternoon idea, and nobody can tell without digging
        through dozens of files.
      </p>
      <p>
        Separating standing collective rules (&quot;articles&quot;) from daily
        decision logs and active member rosters preserves democratic clarity for
        everyone. Note: people often search &quot;articles of
        incorporation,&quot; while UK law calls them &quot;articles of
        association&quot;; we keep the familiar slug while sharing structural
        clarity—this is organizing contrast, not legal formation advice.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Separating constitutional compacts from everyday proceedings
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Standing rules prescribe how power works (s.18):</strong>{" "}
            <em>
              &quot;A company must have articles of association prescribing
              regulations for the company.&quot;
            </em>{" "}
            Unless default rules apply under s.20, standing regulations stay
            readable inside one ordered document rather than scattered comments.
          </li>
          <li>
            <strong>Constitution vs. daily meetings (s.17):</strong> Standing
            articles and formal community resolutions stand cleanly apart from
            day-to-day coordination proceedings.
          </li>
          <li>
            <strong>Founding snapshots aren&apos;t living bylaws (s.8):</strong>{" "}
            An initial incorporation memorandum merely documents founders&apos;
            original intent (such as{" "}
            <em>&quot;to take at least one share each&quot;</em>)—reminding
            commons not to confuse day-one origin notes with evolving member
            rules.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Three plain drawers every commons can maintain
        </h2>
        <p>
          Keep your cooperative charter cleanly separate from{" "}
          <TextLink href="/patterns/directors-minutes">
            stewardship circle minutes
          </TextLink>
          ,{" "}
          <TextLink href="/patterns/records-of-resolutions">
            community resolutions
          </TextLink>
          , and{" "}
          <TextLink href="/patterns/company-registers">member rosters</TextLink>{" "}
          across{" "}
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
          A neighborhood land-stewardship folder titled{" "}
          <em>&quot;Charter&quot;</em> mixes twelve conversational recaps with
          zero legible conflict-resolution or quorum rules. Giving standing
          agreements their own uncluttered home lets every new neighbor
          understand collective rules immediately.
        </p>
      </section>
      <CitedSources slug="articles-of-incorporation" />
    </>
  ),

  "company-registers": () => (
    <>
      <p className="text-lg leading-relaxed">
        The moment a cooperative grows past five close friends, someone asks:{" "}
        <em>
          &quot;Wait—is Elena still an active voting steward, or did she rotate
          out after the autumn harvest?&quot;
        </em>{" "}
        Forcing newcomers to read fifty chronological threads to discover who
        participates right now makes community governance unfair and opaque.
      </p>
      <p>
        Maintaining an open, explicit participant roster apart from meeting
        minutes and charters solves that tension cleanly—shared here as living
        commons organization, contrasted against statutory corporate rules
        rather than offered as legal advice.
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          What a dedicated member register clarifies cleanly
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Explicit active and stepped-back dates (s.113):</strong>{" "}
            <em>
              &quot;Every company must keep a register of its members.&quot;
            </em>{" "}
            Under the current in-force text as verified 10 November 2026,
            subsection (2) captures names, addresses, entry dates, and cessation
            dates (and subsection (3) covers holding particulars when relevant).
          </li>
          <li>
            <strong>Honest versioning on pending legal rules:</strong> A later
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
            <strong>Distinct from minute books (s.1134):</strong> Even statutory
            company-record definitions list registers, accounting records,
            agreements, and minutes as separate tools—never dumped into one
            tangled thread.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Keeping rosters visible without misreading what a list proves
        </h2>
        <p>
          Keep a clean roster tracking active voice and transition dates beside{" "}
          <TextLink href="/patterns/directors-minutes">
            stewardship minutes
          </TextLink>
          ,{" "}
          <TextLink href="/patterns/records-of-resolutions">
            resolutions
          </TextLink>
          , and{" "}
          <TextLink href="/patterns/articles-of-incorporation">
            charters
          </TextLink>
          —and never mistake a list entry alone for{" "}
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
          A mutual-aid collective relies on one chronological notebook to double
          as both meeting diary and active member list. Every quorum check turns
          into forty minutes of page-flipping archaeology. Keeping a clean
          active roster fixes fairness immediately.
        </p>
      </section>
      <CitedSources slug="company-registers" />
    </>
  ),

  "sole-member-decisions": () => (
    <>
      <p className="text-lg leading-relaxed">
        Every big civic venture begins small—often with one person drafting a
        proposal, testing soil samples, or launching a neighborhood tool bank
        alone. It feels silly writing minutes for a meeting of one:{" "}
        <em>&quot;Why note decisions when nobody else was there yet?&quot;</em>{" "}
        The problem hits months later when five neighbors join and find zero
        explanation for founding commitments.
      </p>
      <p>
        Writing early solo choices down makes an eventual transition from solo
        spark to collective stewardship honest from day one. (Explored through
        institutional contrast, not legal advice.)
      </p>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Why even a single initiator writes assumptions down
        </h2>
        <ul className="list-disc space-y-3 pl-5">
          <li>
            <strong>Explicit written record (Companies Act s.357):</strong> Even
            when a private company has only a single member taking decisions
            equivalent to a general meeting, UK s.357 requires delivering
            written details of that choice (unless passed via written
            resolution). Subsection (5) clarifies practically: omitting the
            notice <em>does not affect the validity</em> of the decision, though
            default triggers summary enforcement framed at{" "}
            <em>level 2 on the standard scale</em>.
          </li>
          <li>
            <strong>How s.288 written resolutions connect:</strong> Written
            resolutions preserve procedural clarity across private-company
            votes, and s.357 ensures solo founders don&apos;t drop written
            memory simply because nobody else sat across the table yet.
          </li>
        </ul>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          Leaving an open doorway for incoming co-stewards
        </h2>
        <p>
          When an initiator documents early foundational choices beside{" "}
          <TextLink href="/patterns/records-of-resolutions">
            resolutions
          </TextLink>{" "}
          and{" "}
          <TextLink href="/patterns/company-registers">member rosters</TextLink>
          , incoming community stewards step into transparent partnership rather
          than guessing founder intentions.
        </p>
      </section>
      <section className="space-y-3">
        <h2 className="text-xl font-bold tracking-[-0.015em]">
          A situation like this
        </h2>
        <p>
          An organizer launches a regional repair workshop solo and sets safety
          tool-lending rules in month two without leaving a note explaining why.
          When six mechanics join in month seven, the rules feel arbitrary until
          explained. Two clear lines logged on day one open the work cleanly to
          everyone.
        </p>
      </section>
      <CitedSources slug="sole-member-decisions" />
    </>
  ),

  "problem-intake": () => (
    <>
      <p className="text-lg leading-relaxed">
        Solutionary work begins inside the actual problem and the people living
        through it—before anyone invents point tables, milestone charts, or
        premature tooling.
      </p>
      <ul className="list-disc space-y-2 pl-5">
        <li>
          <strong>Discover:</strong> Listen closely to lived social and
          ecological reality and name who faces downstream harm directly.
        </li>
        <li>
          <strong>Define:</strong> Shape a root-cause proposal with open
          assumptions and shared stewardship terms anyone affected can inspect.
        </li>
        <li>
          <strong>Iterate—or pause honestly:</strong> Build, observe real
          results together, revise openly, and honor stopping or changing course
          when evidence tells you to.
        </li>
      </ul>
      <p>
        Starting with an honest problem brief builds shared purpose cleanly
        without implying automatic payment promises or transferring legal title.
      </p>
      <SourceList
        intro="Core solutionary inquiry loop across ArdaNova."
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
