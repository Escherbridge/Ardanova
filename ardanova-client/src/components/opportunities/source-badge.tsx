"use client";

import Link from "next/link";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Badge } from "~/components/ui/badge";
import { Building2, FolderKanban } from "lucide-react";

interface SourceBadgeProps {
  source: {
    type: "guild" | "project";
    id: string;
    name: string;
    logo?: string | null;
    slug: string;
  };
  size?: "sm" | "md";
}

const sourceConfig = {
  guild: {
    icon: Building2,
    label: "Guild",
    variant: "neon-purple" as const,
    href: (slug: string) => `/guilds/${slug}`
  },
  project: {
    icon: FolderKanban,
    label: "Project",
    variant: "neon" as const,
    href: (slug: string) => `/projects/${slug}`
  },
};

export function SourceBadge({ source, size = "sm" }: SourceBadgeProps) {
  const config = sourceConfig[source.type];
  const Icon = config.icon;

  return (
    <Link href={config.href(source.slug)} className="inline-flex items-center gap-2 group">
      <Avatar className={size === "sm" ? "size-5" : "size-6"}>
        <AvatarImage src={source.logo ?? undefined} alt={source.name} />
        <AvatarFallback className="text-xs">
          <Icon className="size-3" />
        </AvatarFallback>
      </Avatar>
      <span className="text-sm text-muted-foreground group-hover:text-foreground transition-colors">
        {source.name}
      </span>
    </Link>
  );
}
