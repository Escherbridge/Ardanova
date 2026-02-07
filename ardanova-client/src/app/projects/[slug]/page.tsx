"use client";

import { useState } from "react";
import { useParams, useSearchParams } from "next/navigation";
import Link from "next/link";
import { useSession } from "next-auth/react";
import {
  ArrowLeft,
  Heart,
  Share2,
  Bookmark,
  MoreHorizontal,
  FileText,
  Bell,
  MessageCircle,
  Vote,
  Users,
  Loader2,
  Edit,
  Trash2,
  Target,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Progress } from "~/components/ui/progress";
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
  UpdatesTab,
  TeamTab,
  ProposalsTab,
  CommentsTab,
  OpportunitiesTab,
} from "~/components/projects";

const tabs = [
  { id: "overview", label: "Overview", icon: FileText },
  { id: "updates", label: "Updates", icon: Bell },
  { id: "team", label: "Team", icon: Users },
  { id: "proposals", label: "Proposals", icon: Vote },
  { id: "opportunities", label: "Work", icon: Target },
  { id: "comments", label: "Comments", icon: MessageCircle },
];

const statusVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary" | "destructive"> = {
  DRAFT: "secondary",
  PUBLISHED: "neon",
  SEEKING_SUPPORT: "warning",
  FUNDED: "neon-green",
  IN_PROGRESS: "neon-purple",
  COMPLETED: "neon-green",
  CANCELLED: "destructive",
};

const categoryVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary"> = {
  TECHNOLOGY: "neon",
  HEALTHCARE: "neon-pink",
  EDUCATION: "neon-purple",
  ENVIRONMENT: "neon-green",
  SOCIAL_IMPACT: "neon-pink",
  BUSINESS: "secondary",
  ARTS_CULTURE: "neon-purple",
  AGRICULTURE: "neon-green",
  FINANCE: "warning",
  OTHER: "secondary",
};

const projectTypeLabels: Record<string, string> = {
  TEMPORARY: "Temporary",
  LONG_TERM: "Long Term",
  FOUNDATION: "Foundation",
  BUSINESS: "Business",
  PRODUCT: "Product",
  OPEN_SOURCE: "Open Source",
  COMMUNITY: "Community",
};

const durationLabels: Record<string, string> = {
  ONE_TWO_WEEKS: "1-2 weeks",
  ONE_THREE_MONTHS: "1-3 months",
  THREE_SIX_MONTHS: "3-6 months",
  SIX_TWELVE_MONTHS: "6-12 months",
  ONE_TWO_YEARS: "1-2 years",
  TWO_PLUS_YEARS: "2+ years",
  ONGOING: "Ongoing",
};

export default function ProjectDetailPage() {
  const params = useParams();
  const searchParams = useSearchParams();
  const slug = params.slug as string;
  const proposalId = searchParams.get("proposalId");
  const [activeTab, setActiveTab] = useState(proposalId ? "proposals" : "overview");
  const { data: session } = useSession();

  const { data: project, isLoading } = api.project.getById.useQuery({ id: slug });
  const { data: members } = api.project.getMembers.useQuery(
    { projectId: project?.id ?? "" },
    { enabled: !!project?.id }
  );

  // Determine if current user is owner or member based on session
  const currentUserId = session?.user?.id;
  const isOwner = !!currentUserId && project?.createdById === currentUserId;
  const isMember = isOwner || (!!currentUserId && members?.some((m: any) => m.userId === currentUserId));

  // Determine user's role in project from members list
  const userMember = members?.find((m: any) => m.userId === currentUserId);
  const userRole = userMember?.role;

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="size-8 animate-spin text-primary" />
      </div>
    );
  }

  if (!project) {
    return (
      <div className="min-h-screen bg-background flex flex-col items-center justify-center">
        <h1 className="text-2xl font-bold text-foreground">Project not found</h1>
        <Button asChild className="mt-4">
          <Link href="/projects">Back to Projects</Link>
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
            <Link href="/projects">
              <ArrowLeft className="size-4 mr-2" />
              Back to Projects
            </Link>
          </Button>

          {/* Project Hero */}
          <div className="flex items-start gap-4">
            <div className="flex-1">
              <div className="flex items-center gap-2 mb-2">
                <Badge variant={statusVariants[project.status] || "secondary"}>
                  {project.status.replace("_", " ")}
                </Badge>
                {(project.categories ?? []).map((cat) => (
                  <Badge key={cat} variant={categoryVariants[cat] || "secondary"}>
                    {cat.replace("_", " ")}
                  </Badge>
                ))}
                {(project as any).projectType && (
                  <Badge variant="neon-purple">
                    {projectTypeLabels[(project as any).projectType] || (project as any).projectType}
                  </Badge>
                )}
                {(project as any).duration && (
                  <Badge variant="secondary">
                    {durationLabels[(project as any).duration] || (project as any).duration}
                  </Badge>
                )}
              </div>

              <h1 className="text-3xl font-bold text-foreground">{project.title}</h1>

              <div className="flex items-center gap-3 mt-3">
                <Avatar className="size-8">
                  <AvatarImage src={(project as any).createdBy?.image} />
                  <AvatarFallback>{(project as any).createdBy?.name?.charAt(0) || "U"}</AvatarFallback>
                </Avatar>
                <div>
                  <p className="font-medium text-sm text-foreground">
                    {(project as any).createdBy?.name || "Unknown"}
                  </p>
                  <p className="text-xs text-muted-foreground">Project Founder</p>
                </div>
              </div>
            </div>

            {/* Action Buttons */}
            <div className="flex items-center gap-2">
              <Button variant="neon">
                <Heart className="size-4 mr-2" />
                Support
              </Button>
              <Button variant="outline">
                <Share2 className="size-4" />
              </Button>
              <Button variant="outline">
                <Bookmark className="size-4" />
              </Button>
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" size="icon">
                    <MoreHorizontal className="size-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem>
                    <Edit className="size-4 mr-2" />
                    Edit Project
                  </DropdownMenuItem>
                  <DropdownMenuItem className="text-destructive">
                    <Trash2 className="size-4 mr-2" />
                    Delete Project
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </div>

          {/* Stats */}
          <div className="grid grid-cols-4 gap-4 mt-6">
            <Card className="bg-card border-2 border-border">
              <CardContent className="p-4 text-center">
                <p className="text-2xl font-bold text-primary">{project.supportersCount || 0}</p>
                <p className="text-sm text-muted-foreground">Supporters</p>
              </CardContent>
            </Card>
            <Card className="bg-card border-2 border-border">
              <CardContent className="p-4 text-center">
                <p className="text-2xl font-bold text-foreground">{project.votesCount || 0}</p>
                <p className="text-sm text-muted-foreground">Votes</p>
              </CardContent>
            </Card>
            <Card className="bg-card border-2 border-border">
              <CardContent className="p-4 text-center">
                <p className="text-2xl font-bold text-neon-green">
                  ${Number(project.currentFunding || 0).toLocaleString()}
                </p>
                <p className="text-sm text-muted-foreground">Raised</p>
              </CardContent>
            </Card>
            <Card className="bg-card border-2 border-border">
              <CardContent className="p-4 text-center">
                <p className="text-2xl font-bold text-foreground">{project.viewsCount || 0}</p>
                <p className="text-sm text-muted-foreground">Views</p>
              </CardContent>
            </Card>
          </div>

          {/* Funding Progress */}
          {project.fundingGoal && Number(project.fundingGoal) > 0 && (
            <div className="mt-6">
              <div className="flex justify-between text-sm mb-2">
                <span className="text-muted-foreground">Funding Progress</span>
                <span className="font-medium text-foreground">
                  ${Number(project.currentFunding || 0).toLocaleString()} of ${Number(project.fundingGoal).toLocaleString()}
                </span>
              </div>
              <Progress
                value={(Number(project.currentFunding || 0) / Number(project.fundingGoal)) * 100}
                variant="neon"
              />
            </div>
          )}
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
        {activeTab === "overview" && <OverviewTab project={project} />}
        {activeTab === "updates" && <UpdatesTab projectId={project.id} isOwner={isOwner} />}
        {activeTab === "team" && <TeamTab projectId={project.id} isOwner={isOwner} />}
        {activeTab === "proposals" && (
          <ProposalsTab
            projectId={project.id}
            isOwner={isOwner}
            isMember={isMember}
            selectedProposalId={proposalId || undefined}
            userId={currentUserId}
          />
        )}
        {activeTab === "opportunities" && <OpportunitiesTab projectId={project.id} projectSlug={slug} isOwner={isOwner} userRole={userRole} />}
        {activeTab === "comments" && <CommentsTab projectId={project.id} />}
      </div>
    </div>
  );
}
