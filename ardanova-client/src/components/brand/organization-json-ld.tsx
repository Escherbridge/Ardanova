import { JsonLd } from "~/components/brand/json-ld";
import { siteGraph } from "~/lib/schema";

/** Organization and WebSite records. Canonical tags stay in page metadata. */
export function OrganizationJsonLd() {
  return <JsonLd data={siteGraph()} />;
}
