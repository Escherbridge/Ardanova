"use client";

import { useState } from "react";
import Link from "next/link";
import {
  Heart,
  DollarSign,
  Users,
  Folder,
  Calendar,
  ExternalLink,
  Loader2,
  X,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { cn } from "~/lib/utils";
// import { api } from "~/trpc/react";

const supportTypes = [
  { id: "all", label: "All" },
  { id: "SUBSCRIPTION", label: "Subscriptions" },
  { id: "VOTE", label: "Votes" },
  { id: "VOLUNTEER", label: "Volunteering" },
  { id: "RESOURCE", label: "Resources" },
];

const supportTypeVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "secondary"> = {
  SUBSCRIPTION: "neon",
  VOTE: "neon-pink",
  VOLUNTEER: "neon-green",
  RESOURCE: "neon-purple",
};

// Sample data - replace with API call when endpoints are available
const sampleSupports = [
  {
    id: "s1",
    projectId: "p1",
    supportType: "SUBSCRIPTION",
    monthlyAmount: 25,
    message: "Love what you're building! Keep up the great work.",
    isActive: true,
    createdAt: new Date("2025-01-01"),
    project: {
      id: "p1",
      title: "EcoWaste Solutions",
      slug: "ecowaste-solutions",
      images: "/placeholder-project.jpg",
    },
  },
  {
    id: "s2",
    projectId: "p2",
    supportType: "VOLUNTEER",
    monthlyAmount: null,
    message: "Excited to contribute my design skills to this project!",
    isActive: true,
    createdAt: new Date("2025-01-10"),
    project: {
      id: "p2",
      title: "HealthTrack Platform",
      slug: "healthtrack",
      images: "/placeholder-health.jpg",
    },
  },
  {
    id: "s3",
    projectId: "p3",
    supportType: "VOTE",
    monthlyAmount: null,
    message: null,
    isActive: true,
    createdAt: new Date("2025-01-15"),
    project: {
      id: "p3",
      title: "EduConnect Initiative",
      slug: "educonnect",
      images: null,
    },
  },
  {
    id: "s4",
    projectId: "p4",
    supportType: "SUBSCRIPTION",
    monthlyAmount: 50,
    message: "This is exactly the kind of innovation we need.",
    isActive: false,
    createdAt: new Date("2024-12-01"),
    project: {
      id: "p4",
      title: "Climate Action Network",
      slug: "climate-action",
      images: "/placeholder-climate.jpg",
    },
  },
];

export default function SubscriptionsPage() {
  const [activeFilter, setActiveFilter] = useState("all");
  const [cancellingId, setCancellingId] = useState<string | null>(null);
  const [showCancelDialog, setShowCancelDialog] = useState<string | null>(null);

  // TODO: Replace with actual API call when endpoint is available
  // const { data: supports, isLoading, refetch } = api.project.getMySupports.useQuery();
  const supports = sampleSupports;
  const isLoading = false;

  // TODO: Replace with actual mutation when endpoint is available
  // const cancelMutation = api.project.cancelSupport.useMutation({
  //   onSuccess: () => {
  //     refetch();
  //     setCancellingId(null);
  //     setShowCancelDialog(null);
  //   },
  // });

  const handleCancel = (supportId: string) => {
    setCancellingId(supportId);
    // TODO: Call actual mutation
    // cancelMutation.mutate({ supportId });

    // Simulate API call
    setTimeout(() => {
      setCancellingId(null);
      setShowCancelDialog(null);
      // In production, this would trigger a refetch
      console.log("Cancelled support:", supportId);
    }, 1000);
  };

  // Filter supports
  const filteredSupports = (supports ?? []).filter((support) => {
    if (activeFilter === "all") return true;
    return support.supportType === activeFilter;
  });

  // Calculate stats
  const stats = {
    total: supports?.length ?? 0,
    monthlyTotal: supports
      ?.filter((s) => s.supportType === "SUBSCRIPTION" && s.isActive)
      .reduce((sum, s) => sum + (s.monthlyAmount ?? 0), 0) ?? 0,
    activeSubscriptions: supports?.filter((s) => s.supportType === "SUBSCRIPTION" && s.isActive).length ?? 0,
  };

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-4xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold">My Subscriptions</h1>
          <p className="text-muted-foreground mt-2">
            Manage your project supports and subscriptions
          </p>
        </div>

        {/* Stats */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-8">
          <Card className="bg-card">
            <CardContent className="p-4">
              <div className="flex items-center gap-3">
                <div className="p-2 bg-primary/20 rounded-lg">
                  <Heart className="size-5 text-primary" />
                </div>
                <div>
                  <p className="text-2xl font-bold">{stats.total}</p>
                  <p className="text-sm text-muted-foreground">Total Supports</p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card className="bg-card">
            <CardContent className="p-4">
              <div className="flex items-center gap-3">
                <div className="p-2 bg-neon-green/20 rounded-lg">
                  <DollarSign className="size-5 text-neon-green" />
                </div>
                <div>
                  <p className="text-2xl font-bold">${stats.monthlyTotal}/mo</p>
                  <p className="text-sm text-muted-foreground">Monthly Total</p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card className="bg-card">
            <CardContent className="p-4">
              <div className="flex items-center gap-3">
                <div className="p-2 bg-neon-purple/20 rounded-lg">
                  <Folder className="size-5 text-neon-purple" />
                </div>
                <div>
                  <p className="text-2xl font-bold">{stats.activeSubscriptions}</p>
                  <p className="text-sm text-muted-foreground">Active Subscriptions</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Filter Tabs */}
        <div className="flex gap-2 mb-6 overflow-x-auto pb-2">
          {supportTypes.map((type) => (
            <Button
              key={type.id}
              variant={activeFilter === type.id ? "neon" : "outline"}
              size="sm"
              onClick={() => setActiveFilter(type.id)}
            >
              {type.label}
            </Button>
          ))}
        </div>

        {/* Supports List */}
        {isLoading ? (
          <div className="flex items-center justify-center py-20">
            <Loader2 className="size-8 animate-spin text-primary" />
          </div>
        ) : filteredSupports.length === 0 ? (
          <Card className="bg-card">
            <CardContent className="flex flex-col items-center justify-center py-16">
              <Heart className="size-12 text-muted-foreground mb-4" />
              <h3 className="text-lg font-medium">No subscriptions yet</h3>
              <p className="text-muted-foreground mt-1 mb-4">
                Support projects you care about
              </p>
              <Button variant="neon" asChild>
                <Link href="/projects">Browse Projects</Link>
              </Button>
            </CardContent>
          </Card>
        ) : (
          <div className="space-y-4">
            {filteredSupports.map((support) => (
              <Card key={support.id} className="bg-card">
                <CardContent className="p-4">
                  <div className="flex items-start gap-4">
                    {/* Project Info */}
                    <Avatar className="size-12 rounded-lg">
                      <AvatarImage src={support.project?.images?.split(",")[0]} />
                      <AvatarFallback className="rounded-lg">
                        {support.project?.title?.charAt(0) || "P"}
                      </AvatarFallback>
                    </Avatar>

                    <div className="flex-1 min-w-0">
                      <div className="flex flex-wrap items-center gap-2">
                        <Link
                          href={`/projects/${support.project?.slug || support.projectId}`}
                          className="font-medium hover:text-primary transition-colors"
                        >
                          {support.project?.title || "Unknown Project"}
                        </Link>
                        <Badge variant={supportTypeVariants[support.supportType] || "secondary"} size="sm">
                          {support.supportType}
                        </Badge>
                        {!support.isActive && (
                          <Badge variant="secondary" size="sm">Cancelled</Badge>
                        )}
                      </div>

                      <div className="flex flex-wrap items-center gap-4 mt-2 text-sm text-muted-foreground">
                        {support.monthlyAmount && (
                          <span className="flex items-center gap-1">
                            <DollarSign className="size-3" />
                            ${support.monthlyAmount}/month
                          </span>
                        )}
                        <span className="flex items-center gap-1">
                          <Calendar className="size-3" />
                          Since {new Date(support.createdAt).toLocaleDateString()}
                        </span>
                      </div>

                      {support.message && (
                        <p className="text-sm text-muted-foreground mt-2 line-clamp-2">
                          &quot;{support.message}&quot;
                        </p>
                      )}
                    </div>

                    {/* Actions */}
                    <div className="flex items-center gap-2">
                      <Button variant="ghost" size="sm" asChild>
                        <Link href={`/projects/${support.project?.slug || support.projectId}`}>
                          <ExternalLink className="size-4" />
                        </Link>
                      </Button>

                      {support.isActive && (
                        <Button
                          variant="ghost"
                          size="sm"
                          className="text-destructive hover:text-destructive"
                          onClick={() => setShowCancelDialog(support.id)}
                        >
                          <X className="size-4" />
                        </Button>
                      )}
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </div>

      {/* Cancel Dialog */}
      {showCancelDialog && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <Card className="bg-card max-w-md w-full">
            <CardContent className="p-6">
              <h3 className="text-xl font-bold mb-2">Cancel Support?</h3>
              <p className="text-muted-foreground mb-6">
                Are you sure you want to cancel your support for{" "}
                {supports.find(s => s.id === showCancelDialog)?.project?.title}?
                This action cannot be undone.
              </p>
              <div className="flex gap-3 justify-end">
                <Button
                  variant="outline"
                  onClick={() => setShowCancelDialog(null)}
                  disabled={cancellingId === showCancelDialog}
                >
                  Keep Supporting
                </Button>
                <Button
                  variant="destructive"
                  onClick={() => handleCancel(showCancelDialog)}
                  disabled={cancellingId === showCancelDialog}
                >
                  {cancellingId === showCancelDialog ? (
                    <>
                      <Loader2 className="size-4 animate-spin" />
                      Cancelling...
                    </>
                  ) : (
                    "Cancel Support"
                  )}
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
}
