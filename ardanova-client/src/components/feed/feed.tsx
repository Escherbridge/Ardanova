"use client";

import { useState, useCallback } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { Loader2, RefreshCw } from "lucide-react";

import { FeedCard, type FeedCardProps } from "./feed-card";
import { FeedTabs, type FeedTab, defaultFeedTabs } from "./feed-tabs";
import { Button } from "~/components/ui/button";

interface FeedProps {
  tabs?: FeedTab[];
  initialTab?: string;
  items: FeedCardProps[];
  isLoading?: boolean;
  hasMore?: boolean;
  onLoadMore?: () => void;
  onRefresh?: () => void;
  onTabChange?: (tabId: string) => void;
  onEntityClick?: (entity: FeedCardProps["entity"]) => void;
  onAuthorClick?: (author: FeedCardProps["author"]) => void;
  header?: React.ReactNode;
  emptyState?: React.ReactNode;
}

export function Feed({
  tabs = defaultFeedTabs,
  initialTab = "for-you",
  items,
  isLoading = false,
  hasMore = false,
  onLoadMore,
  onRefresh,
  onTabChange,
  onEntityClick,
  onAuthorClick,
  header,
  emptyState,
}: FeedProps) {
  const [activeTab, setActiveTab] = useState(initialTab);

  const handleTabChange = useCallback(
    (tabId: string) => {
      setActiveTab(tabId);
      onTabChange?.(tabId);
    },
    [onTabChange]
  );

  const feedVariants = {
    hidden: { opacity: 0 },
    visible: {
      opacity: 1,
      transition: {
        staggerChildren: 0.05,
      },
    },
  };

  return (
    <div className="flex flex-col h-full">
      {/* Header (optional compose area, etc.) */}
      {header}

      {/* Feed Tabs */}
      <FeedTabs
        tabs={tabs}
        activeTab={activeTab}
        onTabChange={handleTabChange}
      />

      {/* Refresh button */}
      {onRefresh && (
        <div className="flex justify-center py-2 border-b border-border">
          <Button
            variant="ghost"
            size="sm"
            onClick={onRefresh}
            disabled={isLoading}
            className="text-primary"
          >
            <RefreshCw
              className={`size-4 mr-2 ${isLoading ? "animate-spin" : ""}`}
            />
            Refresh feed
          </Button>
        </div>
      )}

      {/* Feed Content */}
      <div className="flex-1 overflow-y-auto">
        <AnimatePresence mode="wait">
          {isLoading && items.length === 0 ? (
            <motion.div
              key="loading"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="flex items-center justify-center py-20"
            >
              <Loader2 className="size-8 animate-spin text-primary" />
            </motion.div>
          ) : items.length === 0 ? (
            <motion.div
              key="empty"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -20 }}
              className="flex flex-col items-center justify-center py-20 px-4"
            >
              {emptyState || (
                <div className="text-center">
                  <p className="text-lg font-medium text-foreground">
                    No posts yet
                  </p>
                  <p className="text-muted-foreground mt-1">
                    Be the first to share something!
                  </p>
                </div>
              )}
            </motion.div>
          ) : (
            <motion.div
              key={activeTab}
              variants={feedVariants}
              initial="hidden"
              animate="visible"
              exit="hidden"
            >
              {items.map((item) => (
                <FeedCard
                  key={item.id}
                  {...item}
                  onEntityClick={onEntityClick}
                  onAuthorClick={onAuthorClick}
                />
              ))}

              {/* Load more */}
              {hasMore && (
                <div className="flex justify-center py-6">
                  <Button
                    variant="outline"
                    onClick={onLoadMore}
                    disabled={isLoading}
                  >
                    {isLoading ? (
                      <>
                        <Loader2 className="size-4 mr-2 animate-spin" />
                        Loading...
                      </>
                    ) : (
                      "Load more"
                    )}
                  </Button>
                </div>
              )}
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    </div>
  );
}
