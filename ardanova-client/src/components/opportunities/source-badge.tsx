"use client";

import Link from "next/link";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
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
    label: "Guild",
    href: (slug: string) => `/guilds/${slug}`,
  },
  project: {
    label: "Project",
    href: (slug: string) => `/projects/${slug}`,
  },
};

export function SourceBadge({ source, size = "sm" }: SourceBadgeProps) {
  const config = sourceConfig[source.type];

  return (
    <Link
      href={config.href(source.slug)}
      className="group inline-flex items-center gap-2"
    >
      <Avatar className={size === "sm" ? "size-5" : "size-6"}>
        <AvatarImage src={source.logo ?? undefined} alt={source.name} />
        <AvatarFallback className="text-xs">
          {source.type === "guild" ? (
            <Building2 className="size-3" aria-hidden="true" />
          ) : (
            <FolderKanban className="size-3" aria-hidden="true" />
          )}
        </AvatarFallback>
      </Avatar>
      <span className="text-muted-foreground group-hover:text-foreground text-sm transition-colors">
        {source.name}
      </span>
    </Link>
  );
}
