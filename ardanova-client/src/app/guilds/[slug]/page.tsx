"use client";

import { useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { useSession } from "next-auth/react";
import {
  ArrowLeft,
  Star,
  Users,
  Briefcase,
  CheckCircle,
  Mail,
  MoreHorizontal,
  FileText,
  Bell,
  Loader2,
  Edit,
  Trash2,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
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
  MembersTab,
  ProjectsTab,
  ReviewsTab,
} from "~/components/guilds";

const tabs = [
  { id: "overview", label: "Overview", icon: FileText },
  { id: "updates", label: "Updates", icon: Bell },
  { id: "members", label: "Members", icon: Users },
  { id: "projects", label: "Projects", icon: Briefcase },
  { id: "reviews", label: "Reviews", icon: Star },
];

export default function GuildDetailPage() {
  const params = useParams();
  const slug = params.slug as string;
  const [activeTab, setActiveTab] = useState("overview");
  const { data: session } = useSession();

  const { data: guild, isLoading } = api.guild.getBySlug.useQuery({ slug });
  const { data: members } = api.guild.getMembers.useQuery(
    { guildId: guild?.id ?? "" },
    { enabled: !!guild?.id }
  );
  const { data: reviews } = api.guild.getReviews.useQuery(
    { guildId: guild?.id ?? "" },
    { enabled: !!guild?.id }
  );

  // Determine if current user is owner
  const currentUserId = session?.user?.id;
  const isOwner = !!currentUserId && guild?.ownerId === currentUserId;

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="size-8 animate-spin text-primary" />
      </div>
    );
  }

  if (!guild) {
    return (
      <div className="min-h-screen bg-background flex flex-col items-center justify-center">
        <h1 className="text-2xl font-bold text-foreground">Guild not found</h1>
        <Button asChild className="mt-4">
          <Link href="/guilds">Back to Guilds</Link>
        </Button>
      </div>
    );
  }

  const renderRating = (rating: number | null) => {
    if (!rating) return <span className="text-muted-foreground">No ratings yet</span>;
    return (
      <div className="flex items-center gap-2">
        <div className="flex">
          {[1, 2, 3, 4, 5].map((star) => (
            <Star
              key={star}
              className={`h-5 w-5 ${
                star <= rating
                  ? "fill-yellow-400 text-yellow-400"
                  : "text-slate-300"
              }`}
            />
          ))}
        </div>
        <span className="font-semibold">{rating.toFixed(1)}</span>
        <span className="text-muted-foreground">({reviews?.length || 0} reviews)</span>
      </div>
    );
  };

  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
      <div className="border-b-2 border-border">
        <div className="max-w-4xl mx-auto px-4 py-4">
          <Button variant="ghost" asChild className="-ml-2 mb-4">
            <Link href="/guilds">
              <ArrowLeft className="size-4 mr-2" />
              Back to Guilds
            </Link>
          </Button>

          {/* Guild Hero */}
          <div className="flex items-start gap-4">
            {/* Logo */}
            <div className="flex-shrink-0">
              {guild.logo ? (
                <img
                  src={guild.logo}
                  alt={guild.name}
                  className="w-24 h-24 rounded-lg object-cover border-2 border-border"
                />
              ) : (
                <div className="w-24 h-24 bg-slate-200 rounded-lg flex items-center justify-center border-2 border-border">
                  <Briefcase className="h-12 w-12 text-slate-400" />
                </div>
              )}
            </div>

            <div className="flex-1">
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <h1 className="text-3xl font-bold text-foreground flex items-center gap-2">
                    {guild.name}
                    {guild.isVerified && (
                      <CheckCircle className="h-6 w-6 text-green-500" />
                    )}
                  </h1>
                  <div className="mt-2">{renderRating(guild.rating)}</div>
                </div>

                {/* Action Buttons */}
                <div className="flex items-center gap-2">
                  <Button variant="neon">
                    <Mail className="size-4 mr-2" />
                    Contact
                  </Button>
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="outline" size="icon">
                        <MoreHorizontal className="size-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      {isOwner && (
                        <>
                          <DropdownMenuItem asChild>
                            <Link href={`/guilds/${slug}/edit`}>
                              <Edit className="size-4 mr-2" />
                              Edit Guild
                            </Link>
                          </DropdownMenuItem>
                          <DropdownMenuItem className="text-destructive">
                            <Trash2 className="size-4 mr-2" />
                            Delete Guild
                          </DropdownMenuItem>
                        </>
                      )}
                      {!isOwner && (
                        <DropdownMenuItem>
                          <Star className="size-4 mr-2" />
                          Submit Review
                        </DropdownMenuItem>
                      )}
                    </DropdownMenuContent>
                  </DropdownMenu>
                </div>
              </div>

              <p className="text-muted-foreground mt-4 line-clamp-2">
                {guild.description}
              </p>

              {/* Specialties */}
              {guild.specialties && (
                <div className="flex flex-wrap gap-2 mt-4">
                  {guild.specialties.split(",").map((specialty, index) => (
                    <Badge key={index} variant="secondary">
                      {specialty.trim()}
                    </Badge>
                  ))}
                </div>
              )}
            </div>
          </div>

          {/* Stats */}
          <div className="grid grid-cols-4 gap-4 mt-6">
            <Card className="bg-card border-2 border-border">
              <CardContent className="p-4 text-center">
                <p className="text-2xl font-bold text-primary">{guild.projectsCount || 0}</p>
                <p className="text-sm text-muted-foreground">Projects</p>
              </CardContent>
            </Card>
            <Card className="bg-card border-2 border-border">
              <CardContent className="p-4 text-center">
                <p className="text-2xl font-bold text-foreground">{members?.length || 0}</p>
                <p className="text-sm text-muted-foreground">Members</p>
              </CardContent>
            </Card>
            <Card className="bg-card border-2 border-border">
              <CardContent className="p-4 text-center">
                <p className="text-2xl font-bold text-neon-green">{reviews?.length || 0}</p>
                <p className="text-sm text-muted-foreground">Reviews</p>
              </CardContent>
            </Card>
            <Card className="bg-card border-2 border-border">
              <CardContent className="p-4 text-center">
                <p className="text-2xl font-bold text-foreground">
                  {guild.rating ? guild.rating.toFixed(1) : "-"}
                </p>
                <p className="text-sm text-muted-foreground">Rating</p>
              </CardContent>
            </Card>
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
        {activeTab === "overview" && <OverviewTab guild={guild} />}
        {activeTab === "updates" && <UpdatesTab guildId={guild.id} isOwner={isOwner} />}
        {activeTab === "members" && <MembersTab guildId={guild.id} isOwner={isOwner} />}
        {activeTab === "projects" && <ProjectsTab guildId={guild.id} />}
        {activeTab === "reviews" && <ReviewsTab guildId={guild.id} isOwner={isOwner} currentUserId={currentUserId} />}
      </div>
    </div>
  );
}
