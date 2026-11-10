import { ESCHERBRIDGE_PROJECTS } from "~/components/brand/escherbridge-projects";
import { publicCanonical, shareImage, SITE_ORIGIN } from "~/lib/site";

import type { Citation } from "./writing";

const organizationId = `${SITE_ORIGIN}/#organization`;

/** Sitewide graph. Rendered in the first HTML response, not after load. */
export function siteGraph() {
  return {
    "@context": "https://schema.org",
    "@graph": [
      {
        "@type": "Organization",
        "@id": organizationId,
        name: "ArdaNova",
        url: SITE_ORIGIN,
        email: "contact@escherbridge.com",
        logo: `${SITE_ORIGIN}/icon-192.png`,
        parentOrganization: {
          "@type": "Organization",
          name: "Escherbridge, LLC",
        },
        sameAs: ESCHERBRIDGE_PROJECTS.map((site) => site.href),
      },
      {
        "@type": "WebSite",
        "@id": `${SITE_ORIGIN}/#website`,
        name: "ArdaNova",
        url: SITE_ORIGIN,
        inLanguage: "en-US",
        publisher: { "@id": organizationId },
      },
    ],
  };
}

export function breadcrumbList(
  items: readonly { name: string; path: "/" | `/${string}` }[],
) {
  return {
    "@context": "https://schema.org",
    "@type": "BreadcrumbList",
    itemListElement: items.map((item, index) => ({
      "@type": "ListItem",
      position: index + 1,
      name: item.name,
      item: publicCanonical(item.path),
    })),
  };
}

export function itemList(name: string, urls: readonly string[]) {
  return {
    "@context": "https://schema.org",
    "@type": "CollectionPage",
    name,
    mainEntity: {
      "@type": "ItemList",
      itemListElement: urls.map((url, index) => ({
        "@type": "ListItem",
        position: index + 1,
        url,
      })),
    },
  };
}

export function articleSchema(entry: {
  headline: string;
  description: string;
  publishedAt: string;
  dateModified?: string;
  canonical: string;
  citations: readonly Citation[];
}) {
  return {
    "@context": "https://schema.org",
    "@type": "Article",
    headline: entry.headline,
    description: entry.description,
    datePublished: entry.publishedAt,
    ...(entry.dateModified ? { dateModified: entry.dateModified } : {}),
    author: { "@type": "Organization", name: "Escherbridge" },
    publisher: { "@id": organizationId },
    mainEntityOfPage: entry.canonical,
    image: shareImage.url,
    citation: entry.citations.map((source) => ({
      "@type": "CreativeWork",
      name: source.label,
      url: source.href,
      ...(source.datePublished ? { datePublished: source.datePublished } : {}),
    })),
  };
}
