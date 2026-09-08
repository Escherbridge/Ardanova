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
import { Badge } from "~/components/ui/badge";
import {
  parseProjectMediaItems,
  type ProjectMediaItem,
} from "~/lib/contracts/project-create-contract";

interface OverviewTabProps {
  project: {
    /**
     * The one-line ask. It arrives once `projectDtoSchema` carries the field; until
     * then the create wizard writes the same sentence to `description`, which is read
     * as the fallback below. No other surface renders `description`, so the lede is
     * shown exactly once either way.
     */
    summary?: string | null;
    description?: string | null;
    problemStatement?: string | null;
    solution?: string | null;
    expectedImpact?: string | null;
    targetAudience?: string | null;
    timeline?: string | null;
    tags?: string | null;
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
  // Projects created before the wizard had a summary carry a copy of the solution in
  // `description`; showing that as a lede would print the same paragraph twice.
  const lede = project.summary?.trim() || project.description?.trim();
  const summary = lede && lede !== project.solution?.trim() ? lede : undefined;

  return (
    <div className="space-y-6">
      {summary && (
        <p className="text-foreground border-system border-l-4 pl-4 text-lg leading-relaxed">
          {summary}
        </p>
      )}

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

      <Card className="bg-card border-2 border-border">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <div className="w-8 h-8 bg-neon-pink/20 rounded-lg flex items-center justify-center border border-neon-pink/30">
              <Target className="size-4 text-neon-pink" />
            </div>
            Problem Statement
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-foreground whitespace-pre-wrap leading-relaxed">
            {project.problemStatement || "No problem statement provided."}
          </p>
        </CardContent>
      </Card>

      <Card className="bg-card border-2 border-border">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <div className="w-8 h-8 bg-neon-green/20 rounded-lg flex items-center justify-center border border-neon-green/30">
              <Lightbulb className="size-4 text-neon-green" />
            </div>
            Proposed Solution
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-foreground whitespace-pre-wrap leading-relaxed">
            {project.solution || "No solution provided."}
          </p>
        </CardContent>
      </Card>

      {project.expectedImpact && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-primary/20 rounded-lg flex items-center justify-center border border-primary/30">
                <TrendingUp className="size-4 text-primary" />
              </div>
              Expected Impact
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground whitespace-pre-wrap leading-relaxed">
              {project.expectedImpact}
            </p>
          </CardContent>
        </Card>
      )}

      {project.targetAudience && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-neon-purple/20 rounded-lg flex items-center justify-center border border-neon-purple/30">
                <Users className="size-4 text-neon-purple" />
              </div>
              Target Audience
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground whitespace-pre-wrap leading-relaxed">
              {project.targetAudience}
            </p>
          </CardContent>
        </Card>
      )}

      {project.timeline && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-warning/20 rounded-lg flex items-center justify-center border border-warning/30">
                <Clock className="size-4 text-warning" />
              </div>
              Timeline
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground whitespace-pre-wrap leading-relaxed">
              {project.timeline}
            </p>
          </CardContent>
        </Card>
      )}

      {project.tags && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg">Tags & Skills</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex flex-wrap gap-2">
              {project.tags.split(",").map((tag: string, i: number) => (
                <Badge key={i} variant="secondary">
                  {tag.trim()}
                </Badge>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
