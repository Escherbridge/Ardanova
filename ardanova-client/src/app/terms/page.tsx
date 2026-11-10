import type { Metadata } from "next";

import {
  LegalDocument,
  LegalSection,
  OperatorContact,
} from "~/components/brand/legal-document";
import { IDAHO_GOVERNING_LAW } from "~/lib/legal/governing-law";
import { publicCanonical, shareImage } from "~/lib/site";

const canonical = publicCanonical("/terms");

export const metadata: Metadata = {
  title: "Terms of use",
  alternates: { canonical },
  openGraph: { url: canonical, images: [shareImage] },
};

export default function TermsPage() {
  return (
    <LegalDocument title="Terms of use">
      <LegalSection heading="Who operates ArdaNova">
        <p>
          Escherbridge, LLC operates the ArdaNova workspace. Questions about
          these terms go to the contact below.
        </p>
        <OperatorContact />
      </LegalSection>

      <LegalSection heading="What you are agreeing to">
        <p>
          ArdaNova is a workspace for describing a problem, coordinating work on
          it, and keeping records of decisions and activity. Creating an
          account, posting, voting, or saving a project record means you agree
          to these terms.
        </p>
      </LegalSection>

      <LegalSection heading="Records in a simulated economy">
        <p>
          The economy on the deployed service is simulated. Balances, unit
          counts, and funding records are bookkeeping inside the workspace.
          Platform records do not automatically create legal ownership, equity,
          or payment rights. A record here is not a promise that anyone will pay
          you, and it does not transfer property.
        </p>
        <p>
          Selling units and sending money to a bank stay unavailable unless this
          deployment offers those steps. Ending a holding period on a record
          does not pay anyone. A recorded vote does not, by itself, move funds,
          change who is a member, or rewrite a project&apos;s terms.
        </p>
      </LegalSection>

      <LegalSection heading="Your account and what you post">
        <p>
          You sign in with Google. You are responsible for activity on the
          account that sign-in links here. Post only what you have permission to
          share. We may refuse an action the software is built to refuse; a
          refusal means that action did not happen.
        </p>
      </LegalSection>

      <LegalSection heading="Governing law">
        <p>{IDAHO_GOVERNING_LAW}</p>
      </LegalSection>
    </LegalDocument>
  );
}
