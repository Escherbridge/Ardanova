import Link from "next/link";
import { MessageCircle, Share2, Bookmark, MoreHorizontal, MapPin, Clock, Users, DollarSign, Zap } from "lucide-react";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "~/components/ui/dropdown-menu";
import { SourceBadge } from "./source-badge";

interface OpportunityCardProps {
  opportunity: {
    id: string;
    title: string;
    slug: string;
    description: string;
    type: string;
    status: string;
    skills?: string;
    compensation?: number;
    compensationDetails?: string;
    location?: string;
    isRemote: boolean;
    deadline?: string;
    applicationsCount: number;
    createdAt: string;
    poster?: {
      id: string;
      name?: string;
      image?: string;
    };
    // Optional: project or guild name for context
    projectId?: string;
    guildId?: string;
    source?: {
      type: "guild" | "project";
      id: string;
      name: string;
      logo?: string | null;
      slug: string;
    };
  };
}

const typeVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary"> = {
  Bounty: "neon-green",
  Freelance: "neon-purple",
  Contract: "neon",
  "Part-time": "warning",
  "Full-time": "neon-pink",
};

function formatRelativeTime(date: Date): string {
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffSec = Math.floor(diffMs / 1000);
  const diffMin = Math.floor(diffSec / 60);
  const diffHour = Math.floor(diffMin / 60);
  const diffDay = Math.floor(diffHour / 24);

  if (diffSec < 60) return "just now";
  if (diffMin < 60) return `${diffMin}m ago`;
  if (diffHour < 24) return `${diffHour}h ago`;
  if (diffDay < 7) return `${diffDay}d ago`;
  return date.toLocaleDateString();
}

function formatTimeLeft(deadline: string): string {
  const now = new Date();
  const deadlineDate = new Date(deadline);
  const diffMs = deadlineDate.getTime() - now.getTime();
  const diffDays = Math.ceil(diffMs / (1000 * 60 * 60 * 24));

  if (diffDays < 0) return "Expired";
  if (diffDays === 0) return "Today";
  if (diffDays === 1) return "1 day left";
  if (diffDays < 7) return `${diffDays} days left`;
  const weeks = Math.ceil(diffDays / 7);
  return `${weeks} week${weeks > 1 ? 's' : ''} left`;
}

function formatCompensation(amount?: number, details?: string): string {
  if (!amount) return "Negotiable";
  const formatted = amount >= 1000 ? `$${(amount / 1000).toFixed(amount % 1000 === 0 ? 0 : 1)}k` : `$${amount}`;
  if (details === "hourly") return `${formatted}/hr`;
  return formatted;
}

function isUrgent(status: string, deadline?: string): boolean {
  if (status !== "OPEN" || !deadline) return false;
  const now = new Date();
  const deadlineDate = new Date(deadline);
  const diffMs = deadlineDate.getTime() - now.getTime();
  const diffDays = Math.ceil(diffMs / (1000 * 60 * 60 * 24));
  return diffDays >= 0 && diffDays < 3;
}

export default function OpportunityCard({ opportunity }: OpportunityCardProps) {
  const skillsList = opportunity.skills ? opportunity.skills.split(',').map(s => s.trim()) : [];
  const isUrgentOpportunity = isUrgent(opportunity.status, opportunity.deadline);
  const typeVariant = typeVariants[opportunity.type] || "secondary";

  return (
    <div className="border-b-2 border-border p-4 transition hover:bg-card/80">
      {/* Header: Avatar, User Info, Type Badges, Timestamp */}
      <div className="mb-3 flex items-start justify-between">
        <div className="flex items-start gap-3">
          <Avatar>
            <AvatarImage src={opportunity.poster?.image} alt={opportunity.poster?.name} />
            <AvatarFallback>
              {opportunity.poster?.name?.charAt(0).toUpperCase() || "U"}
            </AvatarFallback>
          </Avatar>

          <div className="flex flex-col">
            <div className="flex items-center gap-2">
              <span className="font-semibold">{opportunity.poster?.name || "Anonymous"}</span>
              <span className="text-sm text-muted-foreground">
                {formatRelativeTime(new Date(opportunity.createdAt))}
              </span>
            </div>

            {/* Type badges */}
            <div className="mt-1 flex flex-wrap gap-1.5">
              {isUrgentOpportunity && (
                <Badge variant="destructive" className="text-xs">
                  <Zap className="mr-1 h-3 w-3" />
                  Urgent
                </Badge>
              )}
              <Badge variant={typeVariant} className="text-xs">
                {opportunity.type}
              </Badge>
            </div>

            {/* Source Badge */}
            {opportunity.source && (
              <div className="mt-2 flex items-center gap-2">
                <span className="text-xs text-muted-foreground">from</span>
                <SourceBadge source={opportunity.source} />
              </div>
            )}
          </div>
        </div>

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" className="h-8 w-8">
              <MoreHorizontal className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem>Report</DropdownMenuItem>
            <DropdownMenuItem>Hide</DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      {/* Title */}
      <Link
        href={`/opportunities/${opportunity.slug}`}
        className="mb-2 block text-xl font-bold transition hover:text-primary"
      >
        {opportunity.title}
      </Link>

      {/* Description */}
      <p className="mb-3 line-clamp-3 text-muted-foreground">
        {opportunity.description}
      </p>

      {/* Skills */}
      {skillsList.length > 0 && (
        <div className="mb-3 flex flex-wrap gap-1.5">
          {skillsList.map((skill, idx) => (
            <Badge key={idx} variant="outline" className="text-xs">
              {skill}
            </Badge>
          ))}
        </div>
      )}

      {/* Metadata: Compensation, Location, Time Left, Applicants */}
      <div className="mb-4 flex flex-wrap gap-3 text-sm text-muted-foreground">
        <div className="flex items-center gap-1">
          <DollarSign className="h-4 w-4" />
          <span>{formatCompensation(opportunity.compensation, opportunity.compensationDetails)}</span>
        </div>

        <div className="flex items-center gap-1">
          <MapPin className="h-4 w-4" />
          <span>{opportunity.isRemote ? "Remote" : opportunity.location || "On-site"}</span>
        </div>

        {opportunity.deadline && (
          <div className="flex items-center gap-1">
            <Clock className="h-4 w-4" />
            <span>{formatTimeLeft(opportunity.deadline)}</span>
          </div>
        )}

        <div className="flex items-center gap-1">
          <Users className="h-4 w-4" />
          <span>{opportunity.applicationsCount} {opportunity.applicationsCount === 1 ? "applicant" : "applicants"}</span>
        </div>
      </div>

      {/* Action Buttons */}
      <div className="flex items-center justify-between">
        <div className="flex gap-2">
          <Button variant="ghost" size="sm" className="gap-1.5">
            <MessageCircle className="h-4 w-4" />
            Comment
          </Button>
          <Button variant="ghost" size="sm" className="gap-1.5">
            <Share2 className="h-4 w-4" />
            Share
          </Button>
          <Button variant="ghost" size="sm" className="gap-1.5">
            <Bookmark className="h-4 w-4" />
            Bookmark
          </Button>
        </div>

        <Button variant="neon" size="sm">
          Apply Now
        </Button>
      </div>
    </div>
  );
}
