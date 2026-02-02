"use client";

import { useState } from "react";
import Link from "next/link";
import { motion } from "framer-motion";
import {
  Heart,
  MessageCircle,
  Share2,
  MoreHorizontal,
  Bookmark,
  Users,
  FolderKanban,
  TrendingUp,
  Award,
  Vote,
} from "lucide-react";

import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "~/components/ui/dropdown-menu";
import { cn } from "~/lib/utils";

export type FeedItemType =
  | "post"
  | "project_update"
  | "guild_activity"
  | "task_completed"
  | "milestone"
  | "proposal";

export interface FeedCardProps {
  id: string;
  type: FeedItemType;
  author: {
    id: string;
    name: string;
    avatar?: string;
    badge?: string;
  };
  entity?: {
    id: string;
    type: "project" | "guild";
    name: string;
    slug: string;
  };
  content: {
    text?: string;
    title?: string;
    media?: { type: "image" | "video"; url: string }[];
    metadata?: Record<string, string | number>;
  };
  engagement: {
    likes: number;
    comments: number;
    shares: number;
  };
  timestamp: Date;
  isLiked?: boolean;
  isBookmarked?: boolean;
  onEntityClick?: (entity: FeedCardProps["entity"]) => void;
  onAuthorClick?: (author: FeedCardProps["author"]) => void;
}

const typeIcons: Record<FeedItemType, typeof FolderKanban> = {
  post: MessageCircle,
  project_update: FolderKanban,
  guild_activity: Users,
  task_completed: Award,
  milestone: TrendingUp,
  proposal: Vote,
};

const typeLabels: Record<FeedItemType, string> = {
  post: "Post",
  project_update: "Project Update",
  guild_activity: "Guild Activity",
  task_completed: "Task Completed",
  milestone: "Milestone",
  proposal: "Proposal",
};

const typeBadgeVariants: Record<FeedItemType, string> = {
  post: "secondary",
  project_update: "neon",
  guild_activity: "neon-pink",
  task_completed: "neon-green",
  milestone: "neon",
  proposal: "neon-purple",
};

function formatRelativeTime(date: Date): string {
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffSec = Math.floor(diffMs / 1000);
  const diffMin = Math.floor(diffSec / 60);
  const diffHour = Math.floor(diffMin / 60);
  const diffDay = Math.floor(diffHour / 24);

  if (diffSec < 60) return "just now";
  if (diffMin < 60) return `${diffMin}m`;
  if (diffHour < 24) return `${diffHour}h`;
  if (diffDay < 7) return `${diffDay}d`;
  return date.toLocaleDateString();
}

export function FeedCard({
  id,
  type,
  author,
  entity,
  content,
  engagement,
  timestamp,
  isLiked = false,
  isBookmarked = false,
  onEntityClick,
  onAuthorClick,
}: FeedCardProps) {
  const [liked, setLiked] = useState(isLiked);
  const [likeCount, setLikeCount] = useState(engagement.likes);
  const [bookmarked, setBookmarked] = useState(isBookmarked);

  const TypeIcon = typeIcons[type];

  const handleLike = () => {
    setLiked(!liked);
    setLikeCount(liked ? likeCount - 1 : likeCount + 1);
  };

  const handleEntityClick = (e: React.MouseEvent) => {
    if (entity && onEntityClick) {
      e.preventDefault();
      onEntityClick(entity);
    }
  };

  return (
    <motion.article
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -20 }}
      className="border-b-2 border-border bg-card hover:bg-card/80 transition-colors"
    >
      <div className="p-4">
        {/* Header */}
        <div className="flex items-start gap-3">
          <button
            onClick={() => onAuthorClick?.(author)}
            className="shrink-0"
          >
            <Avatar className="size-10 border-2 border-border hover:border-primary transition-colors">
              <AvatarImage src={author.avatar} alt={author.name} />
              <AvatarFallback>{author.name.charAt(0)}</AvatarFallback>
            </Avatar>
          </button>

          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <button
                onClick={() => onAuthorClick?.(author)}
                className="font-medium text-foreground hover:text-primary transition-colors"
              >
                {author.name}
              </button>
              {author.badge && (
                <Badge variant="secondary" size="sm">
                  {author.badge}
                </Badge>
              )}
              <span className="text-muted-foreground text-sm">·</span>
              <span className="text-muted-foreground text-sm">
                {formatRelativeTime(timestamp)}
              </span>
            </div>

            {/* Entity context */}
            {entity && (
              <button
                onClick={handleEntityClick}
                className="flex items-center gap-1.5 text-sm text-muted-foreground hover:text-primary transition-colors mt-0.5"
              >
                <TypeIcon className="size-3.5" />
                <span>in</span>
                <span className="font-medium">{entity.name}</span>
              </button>
            )}
          </div>

          <div className="flex items-center gap-2">
            <Badge
              variant={typeBadgeVariants[type] as "secondary" | "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning"}
              size="sm"
            >
              {typeLabels[type]}
            </Badge>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="icon-sm">
                  <MoreHorizontal className="size-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem>Copy link</DropdownMenuItem>
                <DropdownMenuItem>Report</DropdownMenuItem>
                <DropdownMenuItem>Mute author</DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </div>

        {/* Content */}
        <div className="mt-3 pl-13">
          {content.title && (
            <h3 className="font-semibold text-foreground mb-2">
              {content.title}
            </h3>
          )}
          {content.text && (
            <p className="text-foreground whitespace-pre-wrap break-words">
              {content.text}
            </p>
          )}

          {/* Media */}
          {content.media && content.media.length > 0 && (
            <div
              className={cn(
                "mt-3 grid gap-2",
                content.media.length === 1 && "grid-cols-1",
                content.media.length === 2 && "grid-cols-2",
                content.media.length >= 3 && "grid-cols-2"
              )}
            >
              {content.media.slice(0, 4).map((item, i) => (
                <div
                  key={i}
                  className={cn(
                    "relative border-2 border-border overflow-hidden",
                    content.media!.length === 3 && i === 0 && "row-span-2"
                  )}
                >
                  {item.type === "image" ? (
                    <img
                      src={item.url}
                      alt=""
                      className="w-full h-full object-cover"
                    />
                  ) : (
                    <video src={item.url} controls className="w-full h-full" />
                  )}
                </div>
              ))}
            </div>
          )}

          {/* Metadata for specific types */}
          {content.metadata && Object.keys(content.metadata).length > 0 && (
            <div className="mt-3 flex flex-wrap gap-4 text-sm">
              {Object.entries(content.metadata).map(([key, value]) => (
                <div key={key} className="flex items-center gap-1.5">
                  <span className="text-muted-foreground">{key}:</span>
                  <span className="font-medium text-foreground">{value}</span>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Actions */}
        <div className="mt-4 pl-13 flex items-center gap-1">
          <Button
            variant="ghost"
            size="sm"
            className={cn(
              "gap-1.5 text-muted-foreground hover:text-neon-pink",
              liked && "text-neon-pink"
            )}
            onClick={handleLike}
          >
            <Heart
              className={cn("size-4", liked && "fill-current")}
            />
            <span className="text-xs">{likeCount > 0 && likeCount}</span>
          </Button>

          <Button
            variant="ghost"
            size="sm"
            className="gap-1.5 text-muted-foreground hover:text-primary"
          >
            <MessageCircle className="size-4" />
            <span className="text-xs">
              {engagement.comments > 0 && engagement.comments}
            </span>
          </Button>

          <Button
            variant="ghost"
            size="sm"
            className="gap-1.5 text-muted-foreground hover:text-neon-green"
          >
            <Share2 className="size-4" />
            <span className="text-xs">
              {engagement.shares > 0 && engagement.shares}
            </span>
          </Button>

          <div className="flex-1" />

          <Button
            variant="ghost"
            size="icon-sm"
            className={cn(
              "text-muted-foreground hover:text-neon-yellow",
              bookmarked && "text-neon-yellow"
            )}
            onClick={() => setBookmarked(!bookmarked)}
          >
            <Bookmark
              className={cn("size-4", bookmarked && "fill-current")}
            />
          </Button>
        </div>
      </div>
    </motion.article>
  );
}
