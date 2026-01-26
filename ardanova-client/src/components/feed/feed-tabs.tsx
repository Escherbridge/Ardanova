"use client";

import { motion } from "framer-motion";
import {
  Home,
  FolderKanban,
  Users,
  Store,
  TrendingUp,
  Bookmark,
  type LucideIcon,
} from "lucide-react";

import { cn } from "~/lib/utils";

export interface FeedTab {
  id: string;
  label: string;
  icon: LucideIcon;
  count?: number;
}

export const defaultFeedTabs: FeedTab[] = [
  { id: "for-you", label: "For You", icon: Home },
  { id: "projects", label: "Projects", icon: FolderKanban },
  { id: "guilds", label: "Guilds", icon: Users },
  { id: "marketplace", label: "Marketplace", icon: Store },
  { id: "trending", label: "Trending", icon: TrendingUp },
  { id: "saved", label: "Saved", icon: Bookmark },
];

interface FeedTabsProps {
  tabs?: FeedTab[];
  activeTab: string;
  onTabChange: (tabId: string) => void;
  variant?: "default" | "compact";
}

export function FeedTabs({
  tabs = defaultFeedTabs,
  activeTab,
  onTabChange,
  variant = "default",
}: FeedTabsProps) {
  return (
    <div className="border-b-2 border-border bg-card sticky top-0 z-30">
      <nav
        className={cn(
          "flex overflow-x-auto scrollbar-hide",
          variant === "default" ? "gap-1 px-4" : "gap-0"
        )}
        role="tablist"
      >
        {tabs.map((tab) => {
          const isActive = activeTab === tab.id;
          const Icon = tab.icon;

          return (
            <button
              key={tab.id}
              role="tab"
              aria-selected={isActive}
              onClick={() => onTabChange(tab.id)}
              className={cn(
                "relative flex items-center gap-2 px-4 py-3 text-sm font-medium transition-colors whitespace-nowrap",
                isActive
                  ? "text-foreground"
                  : "text-muted-foreground hover:text-foreground hover:bg-secondary/50"
              )}
            >
              <Icon className="size-4" />
              <span>{tab.label}</span>
              {tab.count !== undefined && tab.count > 0 && (
                <span
                  className={cn(
                    "px-1.5 py-0.5 text-xs font-medium",
                    isActive
                      ? "bg-primary text-primary-foreground"
                      : "bg-secondary text-secondary-foreground"
                  )}
                >
                  {tab.count > 99 ? "99+" : tab.count}
                </span>
              )}
              {isActive && (
                <motion.div
                  layoutId="feed-tab-indicator"
                  className="absolute bottom-0 left-0 right-0 h-0.5 bg-primary"
                  initial={false}
                  transition={{
                    type: "spring",
                    stiffness: 500,
                    damping: 30,
                  }}
                />
              )}
            </button>
          );
        })}
      </nav>
    </div>
  );
}

// Minimal inline tabs for entity pages
interface InlineTabsProps {
  tabs: { id: string; label: string; count?: number }[];
  activeTab: string;
  onTabChange: (tabId: string) => void;
}

export function InlineTabs({ tabs, activeTab, onTabChange }: InlineTabsProps) {
  return (
    <div className="flex gap-1 p-1 bg-secondary border-2 border-border">
      {tabs.map((tab) => {
        const isActive = activeTab === tab.id;

        return (
          <button
            key={tab.id}
            onClick={() => onTabChange(tab.id)}
            className={cn(
              "relative px-3 py-1.5 text-sm font-medium transition-all",
              isActive
                ? "bg-background text-foreground"
                : "text-muted-foreground hover:text-foreground"
            )}
          >
            <span>{tab.label}</span>
            {tab.count !== undefined && (
              <span className="ml-1.5 text-xs text-muted-foreground">
                ({tab.count})
              </span>
            )}
          </button>
        );
      })}
    </div>
  );
}
