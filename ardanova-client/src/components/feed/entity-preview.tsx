"use client";

import { motion, AnimatePresence } from "framer-motion";
import Link from "next/link";
import {
  X,
  ExternalLink,
  Users,
  FolderKanban,
  Store,
  Calendar,
  TrendingUp,
  ChevronRight,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Progress } from "~/components/ui/progress";
import { InlineTabs } from "./feed-tabs";
import { useState } from "react";

export interface EntityData {
  id: string;
  type: "project" | "guild" | "shop";
  name: string;
  slug: string;
  description?: string;
  image?: string;
  stats?: {
    members?: number;
    tasks?: number;
    completed?: number;
    funding?: number;
    fundingGoal?: number;
  };
  tags?: string[];
  recentMembers?: {
    id: string;
    name: string;
    avatar?: string;
  }[];
  createdAt?: Date;
}

interface EntityPreviewProps {
  entity: EntityData | null;
  isOpen: boolean;
  onClose: () => void;
  onNavigate?: (entity: EntityData) => void;
}

const entityIcons = {
  project: FolderKanban,
  guild: Users,
  shop: Store,
};

const entityColors = {
  project: "neon-cyan",
  guild: "neon-pink",
  shop: "neon-yellow",
};

export function EntityPreview({
  entity,
  isOpen,
  onClose,
  onNavigate,
}: EntityPreviewProps) {
  const [activeTab, setActiveTab] = useState("overview");

  if (!entity) return null;

  const Icon = entityIcons[entity.type];
  const colorClass = entityColors[entity.type];

  const tabs = [
    { id: "overview", label: "Overview" },
    { id: "activity", label: "Activity", count: 12 },
    { id: "members", label: "Members", count: entity.stats?.members },
  ];

  const progress = entity.stats?.fundingGoal
    ? ((entity.stats.funding || 0) / entity.stats.fundingGoal) * 100
    : 0;

  return (
    <AnimatePresence>
      {isOpen && (
        <>
          {/* Backdrop */}
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={onClose}
            className="fixed inset-0 bg-black/50 z-50"
          />

          {/* Panel */}
          <motion.div
            initial={{ x: "100%" }}
            animate={{ x: 0 }}
            exit={{ x: "100%" }}
            transition={{ type: "spring", damping: 30, stiffness: 300 }}
            className="fixed right-0 top-0 bottom-0 w-full max-w-lg bg-background border-l-2 border-border z-50 flex flex-col"
          >
            {/* Header */}
            <div className="flex items-center justify-between p-4 border-b-2 border-border">
              <div className="flex items-center gap-3">
                <div
                  className={`size-10 border-2 border-${colorClass} flex items-center justify-center`}
                >
                  <Icon className={`size-5 text-${colorClass}`} />
                </div>
                <div>
                  <Badge variant="secondary" size="sm" className="mb-1">
                    {entity.type}
                  </Badge>
                  <h2 className="font-bold text-foreground">{entity.name}</h2>
                </div>
              </div>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline-neon"
                  size="sm"
                  asChild
                  onClick={() => onNavigate?.(entity)}
                >
                  <Link href={`/${entity.type}s/${entity.slug}`}>
                    <span>View Full</span>
                    <ExternalLink className="size-3.5 ml-1.5" />
                  </Link>
                </Button>
                <Button variant="ghost" size="icon-sm" onClick={onClose}>
                  <X className="size-4" />
                </Button>
              </div>
            </div>

            {/* Tabs */}
            <div className="px-4 pt-4">
              <InlineTabs
                tabs={tabs}
                activeTab={activeTab}
                onTabChange={setActiveTab}
              />
            </div>

            {/* Content */}
            <div className="flex-1 overflow-y-auto p-4">
              <AnimatePresence mode="wait">
                {activeTab === "overview" && (
                  <motion.div
                    key="overview"
                    initial={{ opacity: 0, y: 10 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: -10 }}
                    className="space-y-6"
                  >
                    {/* Image/Banner */}
                    {entity.image && (
                      <div className="aspect-video border-2 border-border overflow-hidden">
                        <img
                          src={entity.image}
                          alt={entity.name}
                          className="w-full h-full object-cover"
                        />
                      </div>
                    )}

                    {/* Description */}
                    {entity.description && (
                      <p className="text-foreground">{entity.description}</p>
                    )}

                    {/* Tags */}
                    {entity.tags && entity.tags.length > 0 && (
                      <div className="flex flex-wrap gap-2">
                        {entity.tags.map((tag) => (
                          <Badge key={tag} variant="secondary">
                            {tag}
                          </Badge>
                        ))}
                      </div>
                    )}

                    {/* Stats */}
                    {entity.stats && (
                      <div className="grid grid-cols-2 gap-4">
                        {entity.stats.members !== undefined && (
                          <div className="p-3 border-2 border-border">
                            <p className="text-2xl font-bold text-foreground">
                              {entity.stats.members}
                            </p>
                            <p className="text-sm text-muted-foreground">
                              Co-owners
                            </p>
                          </div>
                        )}
                        {entity.stats.tasks !== undefined && (
                          <div className="p-3 border-2 border-border">
                            <p className="text-2xl font-bold text-foreground">
                              {entity.stats.tasks}
                            </p>
                            <p className="text-sm text-muted-foreground">
                              Open Tasks
                            </p>
                          </div>
                        )}
                        {entity.stats.completed !== undefined && (
                          <div className="p-3 border-2 border-border">
                            <p className="text-2xl font-bold text-neon-green">
                              {entity.stats.completed}
                            </p>
                            <p className="text-sm text-muted-foreground">
                              Completed
                            </p>
                          </div>
                        )}
                      </div>
                    )}

                    {/* Funding Progress */}
                    {entity.stats?.fundingGoal && (
                      <div className="space-y-2">
                        <div className="flex items-center justify-between text-sm">
                          <span className="text-muted-foreground">
                            Funding Progress
                          </span>
                          <span className="font-medium text-foreground">
                            ${entity.stats.funding?.toLocaleString() || 0} / $
                            {entity.stats.fundingGoal.toLocaleString()}
                          </span>
                        </div>
                        <Progress value={progress} variant="neon" />
                      </div>
                    )}

                    {/* Recent Members */}
                    {entity.recentMembers && entity.recentMembers.length > 0 && (
                      <div>
                        <h3 className="text-sm font-medium text-foreground mb-3">
                          Recent Contributors
                        </h3>
                        <div className="space-y-2">
                          {entity.recentMembers.map((member) => (
                            <div
                              key={member.id}
                              className="flex items-center gap-3 p-2 border-2 border-border hover:border-primary transition-colors cursor-pointer"
                            >
                              <Avatar className="size-8">
                                <AvatarImage src={member.avatar} />
                                <AvatarFallback>
                                  {member.name.charAt(0)}
                                </AvatarFallback>
                              </Avatar>
                              <span className="font-medium text-foreground flex-1">
                                {member.name}
                              </span>
                              <ChevronRight className="size-4 text-muted-foreground" />
                            </div>
                          ))}
                        </div>
                      </div>
                    )}

                    {/* Created date */}
                    {entity.createdAt && (
                      <div className="flex items-center gap-2 text-sm text-muted-foreground">
                        <Calendar className="size-4" />
                        <span>
                          Created{" "}
                          {entity.createdAt.toLocaleDateString("en-US", {
                            month: "long",
                            year: "numeric",
                          })}
                        </span>
                      </div>
                    )}
                  </motion.div>
                )}

                {activeTab === "activity" && (
                  <motion.div
                    key="activity"
                    initial={{ opacity: 0, y: 10 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: -10 }}
                    className="text-center py-12"
                  >
                    <TrendingUp className="size-12 text-muted-foreground mx-auto mb-3" />
                    <p className="text-muted-foreground">
                      Activity feed coming soon
                    </p>
                  </motion.div>
                )}

                {activeTab === "members" && (
                  <motion.div
                    key="members"
                    initial={{ opacity: 0, y: 10 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: -10 }}
                    className="text-center py-12"
                  >
                    <Users className="size-12 text-muted-foreground mx-auto mb-3" />
                    <p className="text-muted-foreground">
                      Members list coming soon
                    </p>
                  </motion.div>
                )}
              </AnimatePresence>
            </div>

            {/* Footer Actions */}
            <div className="p-4 border-t-2 border-border flex gap-3">
              <Button variant="neon" className="flex-1" asChild>
                <Link href={`/${entity.type}s/${entity.slug}`}>
                  View {entity.type}
                </Link>
              </Button>
              <Button variant="outline" className="flex-1">
                Join
              </Button>
            </div>
          </motion.div>
        </>
      )}
    </AnimatePresence>
  );
}
