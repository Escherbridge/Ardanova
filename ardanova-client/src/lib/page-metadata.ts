import type { Metadata } from "next";

import { publicCanonical, shareImage } from "~/lib/site";

const feedType = {
  "application/rss+xml": publicCanonical("/feed.xml"),
} as const;

/** Inner-page metadata. The homepage canonical is emitted separately. */
export function publicPageMetadata(options: {
  path: `/${string}`;
  title: string;
  description: string;
  kind?: "website" | "article";
  publishedAt?: string;
  /** Set only when the page states a real review date that differs from publication. */
  modifiedAt?: string;
  noindex?: boolean;
}): Metadata {
  const canonical = publicCanonical(options.path);
  const socialTitle = `${options.title} — ArdaNova`;
  const robots = options.noindex
    ? { index: false, follow: true }
    : { index: true, follow: true };
  const image = {
    url: shareImage.url,
    alt: shareImage.alt,
    width: shareImage.width,
    height: shareImage.height,
  };
  const shared = {
    title: options.title,
    description: options.description,
    authors: [{ name: "Escherbridge" }],
    alternates: { canonical, types: feedType },
    robots,
    twitter: {
      card: "summary_large_image" as const,
      title: socialTitle,
      description: options.description,
      images: [image],
    },
  };

  if (options.kind === "article") {
    return {
      ...shared,
      openGraph: {
        type: "article",
        url: canonical,
        title: socialTitle,
        description: options.description,
        images: [shareImage],
        publishedTime: options.publishedAt,
        ...(options.modifiedAt ? { modifiedTime: options.modifiedAt } : {}),
      },
    };
  }

  return {
    ...shared,
    openGraph: {
      type: "website",
      url: canonical,
      title: socialTitle,
      description: options.description,
      images: [shareImage],
    },
  };
}
