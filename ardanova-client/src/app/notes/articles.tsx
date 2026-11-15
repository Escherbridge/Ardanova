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

const solutionarySources = [
  { label: "ArdaNova homepage", href: "https://ardanova.com/" },
  {
    label: "Pattern: two records, one agreement",
    href: "https://ardanova.com/patterns/two-records-one-agreement",
  },
  {
    label: "AZOA workflow safeguards",
    href: "https://azoa.ardanova.com/workflows",
  },
  { label: "ArdaNova terms of use", href: "https://ardanova.com/terms" },
] as const;

const ecologySources = [
  { label: "ArdaNova homepage", href: "https://ardanova.com/" },
  {
    label: "ICA Statement on the Cooperative Identity (1995)",
    href: "https://ica.coop/en/cooperatives/cooperative-identity",
  },
  {
    label: "Pattern: keeping partner commons aligned without a gatekeeper",
    href: "https://ardanova.com/patterns/reconciling-two-ledgers-without-a-central-custodian",
  },
  { label: "ArdaNova terms of use", href: "https://ardanova.com/terms" },
] as const;

const ownershipSources = [
  { label: "ArdaNova homepage", href: "https://ardanova.com/" },
  { label: "PrimusNeo", href: "https://primusneo.com" },
  {
    label: "Pattern: decisions on informal channels without monitoring",
    href: "https://ardanova.com/patterns/informal-channels",
  },
  { label: "ArdaNova terms of use", href: "https://ardanova.com/terms" },
] as const;

const coalitionDriftSources = [
  { label: "ArdaNova pattern library", href: "https://ardanova.com/patterns" },
  {
    label: "Keeping partner commons aligned without a central gatekeeper",
    href: "https://ardanova.com/patterns/reconciling-two-ledgers-without-a-central-custodian",
  },
  {
    label: "AZOA workflow specifications",
    href: "https://azoa.ardanova.com/workflows",
  },
  { label: "ArdaNova terms of use", href: "https://ardanova.com/terms" },
] as const;

const governanceSplitSources = [
  { label: "ArdaNova homepage", href: "https://ardanova.com/" },
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
  { label: "ArdaNova homepage", href: "https://ardanova.com/" },
  { label: "ArdaNova terms of use", href: "https://ardanova.com/terms" },
  { label: "PrimusNeo", href: "https://primusneo.com" },
  { label: "AZOA", href: "https://azoa.ardanova.com" },
  { label: "AZOA Workflows", href: "https://azoa.ardanova.com/workflows" },
] as const;

const agreementSources = [
  { label: "ArdaNova homepage", href: "https://ardanova.com/" },
  { label: "ArdaNova terms of use", href: "https://ardanova.com/terms" },
  { label: "PrimusNeo", href: "https://primusneo.com" },
] as const;

const bodies: Record<NoteSlug, () => ReactNode> = {
  "solutionary-thinking-in-practice": () => (
    <>
      <p className="text-lg leading-relaxed">
        Most online feeds are astonishingly good at making real-world harm feel
        both urgent and untouchable. You read six alarming threads before
        breakfast—soil erosion in a farm valley, clinic closures two towns over,
        housing cooperatives priced out by speculative landlords—feel a burst of
        grief or anger, share one commentary clip, and end the evening slightly
        more exhausted than before.
      </p>
      <p>
        Notice what the software rewarded: reaction speed, moral posture, and
        the performance of concern. Notice what it never asked for: who lives
        closest to the problem, what root structures hold it in place, what
        small intervention might improve life without pushing fresh harm
        elsewhere, and who is willing to build and steward a testable answer
        over the next six months.
      </p>
      <h2 className="pt-2 text-xl font-bold tracking-[-0.015em]">
        Why symptom-chasing exhausts good communities
      </h2>
      <p>
        When groups jump straight from outrage to improvisation, three traps
        catch them quickly. First, <strong>heroic bandages</strong> treat the
        loudest visible symptom while leaving the upstream cause untouched—food
        drives run every November while municipal zoning keeps neighborhoods
        food-bare. Second, <strong>unaccounted side effects</strong> solve one
        group&apos;s crunch by offloading stress onto neighboring workers or
        ecosystems. Third, <strong>unshared memory</strong> leaves volunteers
        reinventing last year&apos;s failed attempt because nobody wrote down
        what was tested and learned.
      </p>
      <p>
        Solutionary thinking takes a quieter, steadier route. Rather than
        treating awareness as an end in itself or pitching a miracle app,
        solutionary practice keeps asking:{" "}
        <em>how do we do the most good and the least downstream harm</em> with
        the people directly affected in the room from day one?
      </p>
      <h2 className="pt-2 text-xl font-bold tracking-[-0.015em]">
        Living inside the loop: Discover, Define, Iterate
      </h2>
      <p>
        Across ArdaNova, shared projects don&apos;t move down a conveyor belt
        toward predetermined hype milestones. They turn around a three-beat
        inquiry loop:
      </p>
      <ul className="list-disc space-y-2 pl-5">
        <li>
          <strong>Discover a problem:</strong> Gather lived field notes, open
          questions, and testimony from the neighbors, practitioners, or guilds
          navigating the fracture right now. Resist rushing to software before
          the human reality is legible.
        </li>
        <li>
          <strong>Define a solution:</strong> Shape a hypothesis others can
          examine without guesswork—what concrete shift is proposed, which
          skills and community funds it takes, who steers decisions, and under
          what documented terms contributors share in outcomes.
        </li>
        <li>
          <strong>Iterate—or stop honestly:</strong> Put the proposal to the
          test in public, log what actually happened beside what was decided,
          and treat changing course (or pausing a path that fails its people) as
          real progress rather than defeat.
        </li>
      </ul>
      <p>
        Read alongside{" "}
        <FollowLink href="/notes/social-ecology-and-shared-stewardship">
          social ecology for builders
        </FollowLink>{" "}
        and our{" "}
        <FollowLink href="/patterns/decision-logs">
          decision memory patterns
        </FollowLink>
        , solutionary work stops resembling a doom-scrolling timeline and begins
        feeling like a workshop where neighbors actually finish what they start.
      </p>
      <Sources items={solutionarySources} />
    </>
  ),

  "social-ecology-and-shared-stewardship": () => (
    <>
      <p className="text-lg leading-relaxed">
        Software culture talks endlessly about scale—bigger audiences, faster
        growth, frictionless onboarding—and almost never talks about ecology.
        Yet human communities fail for the exact same reason monoculture farms
        fail: when you strip away diversity, local feedback loops, and mutual
        stewardship in favor of a single central bottleneck, the whole system
        grows brittle overnight.
      </p>
      <p>
        Murray Bookchin and decades of social-ecology thinkers made an
        observation that still hits builders between the eyes: the way human
        beings treat the living world mirrors the way we organize power among
        ourselves. Extract from workers and neighbors, concentrate decisions in
        an unaccountable tower, and every tool built on top reproduces that
        imbalance. Organize around mutual aid, skilled guilds, and federated
        commons—where small autonomous groups cooperate openly without handing
        authority to one landlord—and resilience stops needing constant heroics.
      </p>
      <h2 className="pt-2 text-xl font-bold tracking-[-0.015em]">
        How living commons stay balanced without a single boss
      </h2>
      <p>
        Healthy ecosystems rely on interlocking niches rather than rigid ladders
        of command. A watershed alliance needs hydrologists, municipal policy
        readers, soil growers, storytellers, and tool librarians working
        together across different rhythms. None of those guilds wants to flatten
        its craft into someone else&apos;s company org chart, yet every guild
        depends on honoring shared agreements across boundaries.
      </p>
      <p>
        That is why ArdaNova organizes work around shared problems, skilled
        guilds, and transparent covenants rather than top-down feeds. Local
        sponsors can back a specific watershed remediation, tool library, or
        neighborhood micro-grid under clear terms. Craftsfolk and researchers
        can contribute effort without surrendering autonomy to an opaque
        intermediary.
      </p>
      <h2 className="pt-2 text-xl font-bold tracking-[-0.015em]">
        Protecting democracy from capital capture inside the commons
      </h2>
      <p>
        Co-ops discovered more than a century ago why mixing financial shares
        with governance voice destroys mutual aid. When whoever brings the
        largest check automatically wins every assembly vote, stewardship turns
        into absentee landlordism within a season.
      </p>
      <p>
        Keeping{" "}
        <FollowLink href="/notes/separating-contribution-ledgers-from-governance-votes">
          shared economic value separate from democratic voice
        </FollowLink>{" "}
        gives social ecology an institutional spine. Contributors and community
        sponsors share transparently in agreed economic outcomes (Record A),
        while collective decisions honor <em>one member, one voice</em> (Record
        B)—making sure the people living with the consequences stay at the
        steering wheel. Explore how federated groups reconcile shared
        commitments in{" "}
        <FollowLink href="/patterns/reconciling-two-ledgers-without-a-central-custodian">
          keeping partner commons aligned without a gatekeeper
        </FollowLink>
        .
      </p>
      <Sources items={ecologySources} />
    </>
  ),

  "ownership-without-surveillance": () => (
    <>
      <p className="text-lg leading-relaxed">
        Every modern social platform makes an unspoken bargain with you: show up
        to connect or solve problems together, and in exchange every hesitation,
        private message, dwelling second, and social tie becomes raw material
        for targeted profiling. When communities ask for shared ownership tools,
        too many platforms swap in an equally hollow bargain—replacing
        surveillance ads with opaque streak points or gamified badges that
        pretend users &quot;own&quot; what they build while the database holds
        zero enforceable rights.
      </p>
      <p>
        ArdaNova stands on a plain rejection of both traps: technology for
        ownership, never for surveillance.
      </p>
      <h2 className="pt-2 text-xl font-bold tracking-[-0.015em]">
        What non-surveillance collaboration feels like day to day
      </h2>
      <p>
        Respecting privacy in a shared project does not mean hiding
        accountability. It means collecting and surfacing only the context a
        group explicitly agreed to track for its shared task—nothing covert, and
        nothing scraped from private conversation threads to score productivity.
      </p>
      <p>
        Consider our pattern on{" "}
        <FollowLink href="/patterns/informal-channels">
          informal conversations and shared memory
        </FollowLink>
        : when neighbors work out a logistical compromise on a porch or over
        group chat, they voluntarily copy the agreed decision—date,
        participants, and terms—into the common project log. Nobody installs
        workplace spying bots or archives private banter. Trust comes from
        voluntary clarity, not monitoring.
      </p>
      <h2 className="pt-2 text-xl font-bold tracking-[-0.015em]">
        Real ownership names the instrument, the state, and the contract
      </h2>
      <p>
        Just as honesty forbids pretending surveillance keeps communities safe,
        honesty also forbids waving &quot;ownership&quot; around as vague hype.
        A balance in an app is bookkeeping memory, not legal title on its own.
        As we unpack in{" "}
        <FollowLink href="/notes/a-record-is-not-a-right">
          A record is not a right
        </FollowLink>
        , true shared ownership always points to three plain facts:
      </p>
      <ul className="list-disc space-y-2 pl-5">
        <li>
          <strong>What is held:</strong> whether a person holds a membership
          credential with participation voice, a project unit share under clear
          utility terms, or a separately executed ownership agreement.
        </li>
        <li>
          <strong>What state it has reached:</strong> distinguishing a draft
          proposal or pending contribution from confirmed and reconciled terms.
        </li>
        <li>
          <strong>Which written agreement governs it:</strong> readable terms
          under applicable law that stay intact even if software interfaces
          change years from now.
        </li>
      </ul>
      <p>
        When stewardship memory stays accountable and documented rights remain
        legible, communities gain durable agency without handing over their data
        or trusting empty slogans.
      </p>
      <Sources items={ownershipSources} />
    </>
  ),

  "why-shared-work-breaks-between-orgs": () => (
    <>
      <p className="text-lg leading-relaxed">
        When two neighborhood guilds, a cooperative workshop, and a group of
        civic sponsors rally around a concrete problem, nobody starts expecting
        a dispute over notebooks. Week one feels vibrant: clear enthusiasm, a
        shared problem brief, and generous offers of time and materials. Yet by
        month two, meetings stall on quiet confusion—not because anyone stopped
        caring, but because three groups kept three incompatible memories of
        what was agreed.
      </p>
      <p>
        Shared projects rarely fracture from malice. They fray when nobody keeps
        a shared commons where commitments, work contributions, and mutual
        agreements point back to clear, dated community decisions.
      </p>
      <h2 className="pt-2 text-xl font-bold tracking-[-0.015em]">
        Three silent ways coalition memory unravels
      </h2>
      <p>
        First, <strong>choices stay locked in side rooms</strong>. Two stewards
        settle a boundary change during a quick phone call, update their local
        notes, and forget to inform the partner guild. Weeks later, neighbors on
        opposite sides act from conflicting promises.
      </p>
      <p>
        Second, <strong>charters get tidied in place</strong>. Trying to make a
        document look neat by overwriting last month&apos;s clause erases why
        the community adapted its plan—forcing every newcomer to guess what
        compromises actually passed.
      </p>
      <p>
        Third, <strong>money starts impersonating governance voice</strong>.
        Unless economic shares (Record A) stay plainly apart from democratic
        voting (Record B), whoever controls the largest spreadsheet quietly
        assumes veto authority over collective direction.
      </p>
      <h2 className="pt-2 text-xl font-bold tracking-[-0.015em]">
        Building mutual accountability without forcing everyone under one boss
      </h2>
      <p>
        Keeping autonomous groups aligned starts with everyday stewardship
        habits: tie contributions back to named agreements, publish corrections
        as open forward entries rather than quiet erasures, and pair split
        bookkeeping with idempotent safeguards like those verified in{" "}
        <FollowLink href="https://azoa.ardanova.com/workflows">
          AZOA workflows
        </FollowLink>
        . Explore the full patterns in{" "}
        <FollowLink href="/patterns/reconciling-two-ledgers-without-a-central-custodian">
          keeping partner commons aligned without a gatekeeper
        </FollowLink>{" "}
        and{" "}
        <FollowLink href="/patterns/append-only-amendments-and-retractions">
          open revisions beat silent edits
        </FollowLink>
        .
      </p>
      <Sources items={coalitionDriftSources} />
    </>
  ),

  "separating-contribution-ledgers-from-governance-votes": () => (
    <>
      <p className="text-lg leading-relaxed">
        Imagine a regional food cooperative deciding how to expand its shared
        cold-storage barn. Some neighbors contributed seasonal carpentry hours;
        local families contributed upfront funds so lumber could be bought
        before frost. Now imagine both contributions collapse into a single
        &quot;point balance&quot; that also determines how many votes each
        person holds in Saturday&apos;s assembly.
      </p>
      <p>
        Within a single season, economic generosity silently turns into unequal
        political power. Those with surplus savings outvote those stewarding the
        work every day—and if a majority assembly suddenly tries to rewrite
        agreed work compensation retroactively, builders lose faith in keeping
        honest books at all.
      </p>
      <h2 className="pt-2 text-xl font-bold tracking-[-0.015em]">
        Two planes that honor different kinds of fairness
      </h2>
      <p>
        ArdaNova protects shared initiatives by refusing to fold economic value
        and civic governance into a single score:
      </p>
      <ul className="list-disc space-y-2 pl-5">
        <li>
          <strong>Record A (Stewardship, work &amp; shared value):</strong>{" "}
          Captures skills contributed, material funds, agreed valuation shares,
          and settlement state transparently across participants.
        </li>
        <li>
          <strong>Record B (Community voice &amp; governance):</strong> Captures
          proposals, open questions, quorum standards, delegated roles, and
          democratic decisions without weighting voices by money.
        </li>
      </ul>
      <p>
        Primary cooperatives codified this boundary more than a century ago:
        equality of voice (<em>one member, one vote</em>) guarantees that
        community capital serves the mission rather than buying control of it.
        Read how this pattern works in{" "}
        <FollowLink href="/patterns/two-records-one-agreement">
          Two records, one agreement
        </FollowLink>{" "}
        and{" "}
        <FollowLink href="/patterns/decision-logs">decision logs</FollowLink>.
      </p>
      <Sources items={governanceSplitSources} />
    </>
  ),

  "a-record-is-not-a-right": () => (
    <>
      <p className="text-lg leading-relaxed">
        People pouring evenings, craft, or savings into a shared civic project
        naturally want one durable assurance: that their contribution is honored
        and the terms everyone shook hands on won&apos;t evaporate six months
        later. Software makes it dangerously tempting to fake that assurance—put
        a shiny percentage badge beside someone&apos;s name and act as though
        the interface itself settles ownership.
      </p>
      <p>
        Honest stewardship refuses that shortcut. A database entry is memory,
        not an enforceable right on its own, unless a readable covenant under
        real law stands behind it. ArdaNova keeps coordination tools and legal
        clarity cleanly separated: helping communities document proposals,
        effort, agreed shares, and collective decisions while stating plainly
        where workspace records stop and signed agreements govern.
      </p>
      <p>
        Even inside the{" "}
        <FollowLink href="https://primusneo.com">PrimusNeo</FollowLink> tooling
        stack alongside companion workflow engine{" "}
        <FollowLink href="https://azoa.ardanova.com/workflows">AZOA</FollowLink>
        , workspace tallies stay strictly inside project bookkeeping—they never
        claim to transfer legal title automatically or override executed
        agreements governed under Idaho law and reviewed by the people involved.
      </p>
      <p>
        Naming that limit openly protects the commons. When{" "}
        <FollowLink href="/patterns/a-record-is-not-a-right">
          stewardship memory stays honest about what it proves
        </FollowLink>
        , nobody gets misled by unbacked software scores—and every co-steward
        can inspect the real agreement holding their shared work together.
      </p>
      <Sources items={recordSources} />
    </>
  ),

  "two-records-one-agreement": () => (
    <>
      <p className="text-lg leading-relaxed">
        Every genuine cooperative venture tracks two completely different kinds
        of truth: what stewardship work and funds went into building something
        valuable, and how a community of equals governs its future. Compress
        both into one leaderboard number and neither truth survives intact.
      </p>
      <p>
        ArdaNova, built within the{" "}
        <FollowLink href="https://primusneo.com">PrimusNeo</FollowLink>{" "}
        ecosystem, separates those two planes intentionally.{" "}
        <strong>Record A</strong> documents work contributed, agreed value
        allocations, and shared economic interests. <strong>Record B</strong>{" "}
        documents proposals, assembly rules, delegated responsibilities, and
        democratic decisions—kept cleanly apart from financial balances.
      </p>
      <p>
        Neither record replaces readable community agreements under real law (in
        our hosted sandbox environment, under terms published by Escherbridge,
        LLC in Boise, Idaho). Instead, separating shared value from shared voice
        keeps power transparent across seasons—and ensures a community can even
        log an honest decision to pause or stop an effort when reality calls for
        a different path. Explore the architecture in{" "}
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
