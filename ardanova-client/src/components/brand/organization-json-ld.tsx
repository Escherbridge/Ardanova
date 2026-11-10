import { ESCHERBRIDGE_PROJECTS } from "~/components/brand/escherbridge-projects";
import { SITE_ORIGIN } from "~/lib/site";

const organization = {
  "@context": "https://schema.org",
  "@type": "Organization",
  name: "ArdaNova",
  url: SITE_ORIGIN,
  sameAs: ESCHERBRIDGE_PROJECTS.map((site) => site.href),
};

/** Sister sites on the organization record. Canonical tags stay in page metadata. */
export function OrganizationJsonLd() {
  return (
    <script
      type="application/ld+json"
      dangerouslySetInnerHTML={{
        __html: JSON.stringify(organization).replaceAll("<", "\\u003c"),
      }}
    />
  );
}
