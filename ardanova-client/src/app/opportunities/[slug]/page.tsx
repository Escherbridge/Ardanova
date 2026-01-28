"use client";

import { useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { useSession } from "next-auth/react";
import {
  ArrowLeft,
  Share2,
  Bookmark,
  MoreHorizontal,
  FileText,
  MessageCircle,
  Users,
  Loader2,
  Edit,
  Trash2,
  DollarSign,
  MapPin,
  Calendar,
  Clock,
  Briefcase,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "~/components/ui/dropdown-menu";
import { cn } from "~/lib/utils";
import { api } from "~/trpc/react";

import {
  OverviewTab,
  ApplicationsTab,
  UpdatesTab,
  CommentsTab,
} from "~/components/opportunities";

const statusVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary" | "destructive"> = {
  DRAFT: "secondary",
  OPEN: "neon",
  IN_REVIEW: "warning",
  FILLED: "neon-green",
  CLOSED: "secondary",
  CANCELLED: "destructive",
};

const typeVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary"> = {
  Bounty: "neon-green",
  Freelance: "neon-purple",
  Contract: "neon",
  "Part-time": "warning",
  "Full-time": "neon-pink",
};

export default function OpportunityDetailPage() {
  const params = useParams();
  const slug = params.slug as string;
  const [activeTab, setActiveTab] = useState("overview");
  const { data: session } = useSession();

  const { data: opportunity, isLoading } = api.opportunity.getById.useQuery({ id: slug });

  // Determine if current user is owner based on session
  const currentUserId = session?.user?.id;
  const isOwner = !!currentUserId && opportunity?.posterId === currentUserId;

  // Define tabs - Applications tab only visible to owner
  const tabs = [
    { id: "overview", label: "Overview", icon: FileText },
    ...(isOwner ? [{ id: "applications", label: "Applications", icon: Users }] : []),
    { id: "updates", label: "Updates", icon: Clock },
    { id: "comments", label: "Comments", icon: MessageCircle },
  ];

  // Calculate days left
  const daysLeft = opportunity?.deadline
    ? Math.ceil((new Date(opportunity.deadline).getTime() - new Date().getTime()) / (1000 * 60 * 60 * 24))
    : null;

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="size-8 animate-spin text-primary" />
      </div>
    );
  }

  if (!opportunity) {
    return (
      <div className="min-h-screen bg-background flex flex-col items-center justify-center">
        <h1 className="text-2xl font-bold text-foreground">Opportunity not found</h1>
        <Button asChild className="mt-4">
          <Link href="/opportunities">Back to Opportunities</Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
      <div className="border-b-2 border-border">
        <div className="max-w-4xl mx-auto px-4 py-4">
          <Button variant="ghost" asChild className="-ml-2 mb-4">
            <Link href="/opportunities">
              <ArrowLeft className="size-4 mr-2" />
              Back to Opportunities
            </Link>
          </Button>

          {/* Opportunity Hero */}
          <div className="flex items-start gap-4">
            <div className="flex-1">
              <div className="flex items-center gap-2 mb-2">
                <Badge variant={statusVariants[opportunity.status] || "secondary"}>
                  {opportunity.status.replace("_", " ")}
                </Badge>
                <Badge variant={typeVariants[opportunity.type] || "secondary"}>
                  {opportunity.type}
                </Badge>
              </div>

              <h1 className="text-3xl font-bold text-foreground">{opportunity.title}</h1>

              <div className="flex items-center gap-3 mt-3">
                <Avatar className="size-8">
                  <AvatarImage src={opportunity.poster?.image} />
                  <AvatarFallback>{opportunity.poster?.name?.charAt(0) || "U"}</AvatarFallback>
                </Avatar>
                <div>
                  <p className="font-medium text-sm text-foreground">
                    {opportunity.poster?.name || "Unknown"}
                  </p>
                  <p className="text-xs text-muted-foreground">Posted by</p>
                </div>
              </div>
            </div>

            {/* Action Buttons */}
            <div className="flex items-center gap-2">
              {!isOwner && opportunity.status === "OPEN" && (
                <Button variant="neon">
                  <Briefcase className="size-4 mr-2" />
                  Apply Now
                </Button>
              )}
              <Button variant="outline">
                <Share2 className="size-4" />
              </Button>
              <Button variant="outline">
                <Bookmark className="size-4" />
              </Button>
              {isOwner && (
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="outline" size="icon">
                      <MoreHorizontal className="size-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem>
                      <Edit className="size-4 mr-2" />
                      Edit Opportunity
                    </DropdownMenuItem>
                    <DropdownMenuItem className="text-destructive">
                      <Trash2 className="size-4 mr-2" />
                      Delete Opportunity
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              )}
            </div>
          </div>

          {/* Stats */}
          <div className="grid grid-cols-4 gap-4 mt-6">
            <Card className="bg-card border-2 border-border">
              <CardContent className="p-4 text-center">
                <p className="text-2xl font-bold text-primary">{opportunity.applicationsCount || 0}</p>
                <p className="text-sm text-muted-foreground">Applicants</p>
              </CardContent>
            </Card>
            <Card className="bg-card border-2 border-border">
              <CardContent className="p-4 text-center">
                <p className={cn(
                  "text-2xl font-bold",
                  daysLeft !== null && daysLeft < 7 ? "text-destructive" : "text-foreground"
                )}>
                  {daysLeft !== null ? `${daysLeft}d` : "N/A"}
                </p>
                <p className="text-sm text-muted-foreground">Days Left</p>
              </CardContent>
            </Card>
            <Card className="bg-card border-2 border-border">
              <CardContent className="p-4 text-center">
                <p className="text-2xl font-bold text-neon-green">
                  {opportunity.compensation ? `$${Number(opportunity.compensation).toLocaleString()}` : "Negotiable"}
                </p>
                <p className="text-sm text-muted-foreground">Compensation</p>
              </CardContent>
            </Card>
            <Card className="bg-card border-2 border-border">
              <CardContent className="p-4 text-center">
                <div className="flex items-center justify-center gap-1">
                  {opportunity.isRemote ? (
                    <>
                      <MapPin className="size-4 text-neon-green" />
                      <p className="text-lg font-bold text-neon-green">Remote</p>
                    </>
                  ) : (
                    <>
                      <MapPin className="size-4 text-foreground" />
                      <p className="text-lg font-bold text-foreground line-clamp-1">{opportunity.location || "N/A"}</p>
                    </>
                  )}
                </div>
                <p className="text-sm text-muted-foreground">Location</p>
              </CardContent>
            </Card>
          </div>

          {/* Quick Details */}
          <div className="mt-6 flex flex-wrap gap-4 text-sm">
            {opportunity.experienceLevel && (
              <div className="flex items-center gap-2">
                <span className="text-muted-foreground">Experience:</span>
                <Badge variant="secondary">{opportunity.experienceLevel}</Badge>
              </div>
            )}
            {opportunity.deadline && (
              <div className="flex items-center gap-2 text-muted-foreground">
                <Calendar className="size-4" />
                <span>Deadline: {new Date(opportunity.deadline).toLocaleDateString()}</span>
              </div>
            )}
            {opportunity.maxApplications && (
              <div className="flex items-center gap-2 text-muted-foreground">
                <Users className="size-4" />
                <span>Max: {opportunity.maxApplications} applicants</span>
              </div>
            )}
            {opportunity.compensationType && (
              <div className="flex items-center gap-2 text-muted-foreground">
                <DollarSign className="size-4" />
                <span>{opportunity.compensationType}</span>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="border-b-2 border-border sticky top-0 bg-background z-10">
        <div className="max-w-4xl mx-auto px-4">
          <div className="flex overflow-x-auto">
            {tabs.map((tab) => {
              const Icon = tab.icon;
              return (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={cn(
                    "flex items-center gap-2 px-4 py-3 text-sm font-medium whitespace-nowrap relative",
                    activeTab === tab.id
                      ? "text-primary"
                      : "text-muted-foreground hover:text-foreground"
                  )}
                >
                  <Icon className="size-4" />
                  {tab.label}
                  {activeTab === tab.id && (
                    <span className="absolute bottom-0 left-0 right-0 h-0.5 bg-primary" />
                  )}
                </button>
              );
            })}
          </div>
        </div>
      </div>

      {/* Tab Content */}
      <div className="max-w-4xl mx-auto px-4 py-6">
        {activeTab === "overview" && <OverviewTab opportunity={opportunity} />}
        {activeTab === "applications" && isOwner && <ApplicationsTab opportunityId={opportunity.id} isOwner={isOwner} />}
        {activeTab === "updates" && <UpdatesTab opportunityId={opportunity.id} isOwner={isOwner} />}
        {activeTab === "comments" && <CommentsTab opportunityId={opportunity.id} />}
      </div>
    </div>
  );
}
