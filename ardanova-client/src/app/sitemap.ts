import type { MetadataRoute } from "next";

import { indexableUrls } from "~/lib/public-urls";

export default function sitemap(): MetadataRoute.Sitemap {
  return indexableUrls().map((entry) => ({
    url: entry.url,
    lastModified: entry.lastModified,
  }));
}
