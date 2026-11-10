import {
  LegalDocument,
  LegalSection,
  OperatorContact,
} from "~/components/brand/legal-document";
import { publicPageMetadata } from "~/lib/page-metadata";

const description =
  "What ArdaNova stores when you sign in with Google, save a profile, or submit a workspace record. The deployed economy is simulated.";

export const metadata = publicPageMetadata({
  path: "/privacy",
  title: "Privacy notice",
  description,
});

export default function PrivacyPage() {
  return (
    <LegalDocument title="Privacy notice">
      <LegalSection heading="Who operates ArdaNova">
        <p>
          Escherbridge, LLC operates ArdaNova. This notice lists information the
          application stores when you use it. It does not state a retention
          period or an age rule. The settings page does not offer account
          closure.
        </p>
        <OperatorContact />
      </LegalSection>

      <LegalSection heading="Sign-in">
        <p>
          You sign in with Google. When Google reports a verified email, the
          application stores that email, the name Google sends if it sends one,
          the profile image address Google sends if it sends one, the time the
          email was marked verified, and the Google account identifier and
          account type used to recognize the same sign-in later.
        </p>
        <p>
          The session is a signed cookie. The account record created at sign-in
          does not store Google access credentials. Sign-in logs record a stable
          error or warning code, and they are written so they do not include the
          Google profile, the session cookie, or credentials.
        </p>
      </LegalSection>

      <LegalSection heading="Profile you choose to save">
        <p>
          If you edit your profile, the application stores the fields you
          submit: display name, bio, location, website, LinkedIn, X, and phone.
          It also stores each skill as a name and a level from 1 to 10, and each
          experience entry as a title, organization, description, start date,
          end date, and whether you marked it current.
        </p>
        <p>
          Phone is shown only to you. Name, bio, location, website, LinkedIn, X,
          skills, and experience are readable by anyone who can open your
          profile. Email is left off that public profile. On a project member
          list, email is filled in for you, for an administrator, and for
          someone who runs that project.
        </p>
      </LegalSection>

      <LegalSection heading="Work you record">
        <p>
          The application stores what you submit in the workspace: project
          descriptions, posts, comments, messages, proposals, and votes,
          including the option you chose and any reason you wrote. It also
          stores notifications it creates from those actions, such as a reply or
          a task being picked up. A message is stored for the people in that
          conversation.
        </p>
      </LegalSection>

      <LegalSection heading="Simulated activity records">
        <p>
          The deployed service reports a simulated economy. If you use the
          funding or unit-record flows, the application stores the related rows:
          who acted, which project, the amounts, the disclosure version, a
          snapshot of the terms shown at the time, unit balances, and sale
          requests. Platform records do not automatically create legal
          ownership, equity, or payment rights.
        </p>
        <p>
          If you open account setup, the application sends your account id to
          the settlement service and may store the identifiers that service
          sends back, plus a verification level derived from the status it
          reports. This application does not store card numbers. The
          identity-document upload route is retired and answers that it is no
          longer available; this application does not store an uploaded identity
          document from that route.
        </p>
      </LegalSection>
    </LegalDocument>
  );
}
