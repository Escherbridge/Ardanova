"use client";

import {
  Target,
  Lightbulb,
  Clock,
  Users,
  TrendingUp,
  FileText,
} from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import {
  parseProjectMediaItems,
  type ProjectMediaItem,
} from "~/lib/contracts/project-create-contract";

/** The project brief. The lede and tags sit in the page header (see ./AGENTS.md §Project page). */
interface OverviewTabProps {
  project: {
    problemStatement?: string | null;
    solution?: string | null;
    expectedImpact?: string | null;
    targetAudience?: string | null;
    timeline?: string | null;
    /** JSON manifest; the canonical media column once the create contract carries it. */
    mediaPaths?: string | null;
    /** The comma-separated location columns the create contract writes today. */
    images?: string | null;
    videos?: string | null;
    documents?: string | null;
  };
}

/** One uploaded file, rendered as the thing it is rather than as its stored path. */
function MediaFigure({ item }: { item: ProjectMediaItem }) {
  const caption = item.fileName || "Project attachment";

  if (item.kind === "image" && item.url) {
    return (
      // next/image is not used here: bucket hosts are not in an images.remotePatterns
      // allow-list, and a project's media host is deployment-specific.
      // eslint-disable-next-line @next/next/no-img-element
      <img
        src={item.url}
        alt={caption}
        loading="lazy"
        className="border-border h-40 w-auto max-w-full rounded-lg border-2 object-cover"
      />
    );
  }

  if (item.kind === "video" && item.url) {
    return (
      <video
        src={item.url}
        controls
        preload="metadata"
        aria-label={caption}
        className="border-border h-40 w-auto max-w-full rounded-lg border-2"
      />
    );
  }

  const chip = (
    <span className="border-border bg-muted/40 flex h-40 w-56 flex-col items-center justify-center gap-2 rounded-lg border-2 p-4 text-center">
      <FileText className="text-muted-foreground size-6" aria-hidden="true" />
      <span className="text-sm break-all">{caption}</span>
    </span>
  );

  return item.url ? (
    <a
      href={item.url}
      target="_blank"
      rel="noopener noreferrer"
      className="focus-visible:ring-ring rounded-lg focus-visible:ring-2 focus-visible:outline-none"
    >
      {chip}
    </a>
  ) : (
    chip
  );
}

export default function OverviewTab({ project }: OverviewTabProps) {
  // The canonical manifest wins; otherwise the three location columns are merged back
  // into one strip, so a project reads the same however its media was recorded.
  const media = (() => {
    const canonical = parseProjectMediaItems(project.mediaPaths);
    if (canonical.length > 0) return canonical;
    const merged = [
      ...parseProjectMediaItems(project.images),
      ...parseProjectMediaItems(project.videos),
      ...parseProjectMediaItems(project.documents),
    ];
    // One location recorded in two columns is still one file, and one React key.
    return Array.from(
      new Map(merged.map((item) => [item.bucketPath, item])).values(),
    );
  })();

  return (
    <div className="space-y-6">
      {media.length > 0 && (
        <section aria-label="Project media">
          <ul className="flex list-none gap-3 overflow-x-auto pb-2">
            {media.map((item) => (
              <li key={item.bucketPath} className="shrink-0">
                <MediaFigure item={item} />
              </li>
            ))}
          </ul>
        </section>
      )}

      <Card className="bg-card border-border border-2">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-lg">
            <div className="bg-neon-pink/20 border-neon-pink/30 flex h-8 w-8 items-center justify-center rounded-lg border">
              <Target className="text-neon-pink size-4" />
            </div>
            Problem Statement
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-foreground leading-relaxed whitespace-pre-wrap">
            {project.problemStatement || "No problem statement provided."}
          </p>
        </CardContent>
      </Card>

      <Card className="bg-card border-border border-2">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-lg">
            <div className="bg-neon-green/20 border-neon-green/30 flex h-8 w-8 items-center justify-center rounded-lg border">
              <Lightbulb className="text-neon-green size-4" />
            </div>
            Proposed Solution
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-foreground leading-relaxed whitespace-pre-wrap">
            {project.solution || "No solution provided."}
          </p>
        </CardContent>
      </Card>

      {project.expectedImpact && (
        <Card className="bg-card border-border border-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <div className="bg-primary/20 border-primary/30 flex h-8 w-8 items-center justify-center rounded-lg border">
                <TrendingUp className="text-primary size-4" />
              </div>
              Expected Impact
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground leading-relaxed whitespace-pre-wrap">
              {project.expectedImpact}
            </p>
          </CardContent>
        </Card>
      )}

      {project.targetAudience && (
        <Card className="bg-card border-border border-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <div className="bg-neon-purple/20 border-neon-purple/30 flex h-8 w-8 items-center justify-center rounded-lg border">
                <Users className="text-neon-purple size-4" />
              </div>
              Target Audience
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground leading-relaxed whitespace-pre-wrap">
              {project.targetAudience}
            </p>
          </CardContent>
        </Card>
      )}

      {project.timeline && (
        <Card className="bg-card border-border border-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <div className="bg-warning/20 border-warning/30 flex h-8 w-8 items-center justify-center rounded-lg border">
                <Clock className="text-warning size-4" />
              </div>
              Timeline
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground leading-relaxed whitespace-pre-wrap">
              {project.timeline}
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
