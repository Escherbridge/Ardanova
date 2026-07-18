"use client";

import { useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import Image from "next/image";
import { useSession } from "next-auth/react";
import {
  ArrowLeft,
  Star,
  Briefcase,
  CheckCircle,
  Loader2,
  Edit,
  Shield,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { cn } from "~/lib/utils";
import { handleTabListKeyDown } from "~/lib/accessibility";
import { api } from "~/trpc/react";

import {
  OverviewTab,
  UpdatesTab,
  MembersTab,
  ReviewsTab,
  OpportunitiesTab,
} from "~/components/guilds";

const tabs: { id: string; label: string }[] = [
  { id: "overview", label: "Overview" },
  { id: "updates", label: "Updates" },
  { id: "members", label: "Members" },
  { id: "opportunities", label: "Opportunities" },
  { id: "reviews", label: "Reviews" },
];

export default function GuildDetailPage() {
  const params = useParams();
  const slug = params.slug as string;
  const [activeTab, setActiveTab] = useState("overview");
  const { data: session } = useSession();

  const {
    data: guild,
    isLoading,
    error: guildError,
  } = api.guild.getBySlug.useQuery({ slug });
  const {
    data: members,
    isLoading: membersLoading,
    error: membersError,
  } = api.guild.getMembers.useQuery(
    { guildId: guild?.id ?? "" },
    { enabled: !!guild?.id },
  );
  const {
    data: reviews,
    isLoading: reviewsLoading,
    error: reviewsError,
  } = api.guild.getReviews.useQuery(
    { guildId: guild?.id ?? "" },
    { enabled: !!guild?.id },
  );
  const {
    data: activeCredentials,
    isLoading: credentialsLoading,
    error: credentialsError,
  } = api.membershipCredential.getActiveByGuildId.useQuery(
    { guildId: guild?.id ?? "" },
    { enabled: !!guild?.id },
  );

  // Determine if current user is owner
  const currentUserId = session?.user?.id;
  const isOwner = !!currentUserId && guild?.ownerId === currentUserId;

  // Role from members list; if missing (legacy guilds created before owner row existed), infer OWNER from guild.ownerId
  const userMember = members?.find((member) => member.userId === currentUserId);
  const userRole = userMember?.role ?? (isOwner ? "OWNER" : undefined);

  if (isLoading) {
    return (
      <div className="bg-background flex min-h-screen items-center justify-center">
        <Loader2 className="text-primary size-8 animate-spin" />
      </div>
    );
  }

  if (guildError || !guild) {
    return (
      <div className="bg-background flex min-h-screen flex-col items-center justify-center">
        <h1 className="text-foreground text-2xl font-bold">
          {guildError ? "Guild could not be loaded" : "Guild not found"}
        </h1>
        <Button asChild className="mt-4">
          <Link href="/guilds">Back to Guilds</Link>
        </Button>
      </div>
    );
  }

  const renderRating = (rating: number | null | undefined) => {
    if (!rating)
      return <span className="text-muted-foreground">No ratings yet</span>;
    return (
      <div className="flex items-center gap-2">
        <div className="flex" aria-label={`Rating ${rating} out of 5`}>
          {[1, 2, 3, 4, 5].map((star) => (
            <Star
              key={star}
              aria-hidden="true"
              className={`h-5 w-5 ${
                star <= rating
                  ? "fill-yellow-400 text-yellow-400"
                  : "text-slate-300"
              }`}
            />
          ))}
        </div>
        <span className="font-semibold">{rating.toFixed(1)}</span>
        <span className="text-muted-foreground">
          {reviewsLoading
            ? "(loading reviews…)"
            : reviewsError
              ? "(review count unavailable)"
              : `(${reviews?.length ?? 0} reviews)`}
        </span>
      </div>
    );
  };

  return (
    <div className="bg-background min-h-screen">
      {/* Header */}
      <div className="border-border border-b-2">
        <div className="mx-auto max-w-4xl px-4 py-4">
          <Button variant="ghost" asChild className="mb-4 -ml-2">
            <Link href="/guilds">
              <ArrowLeft className="mr-2 size-4" />
              Back to Guilds
            </Link>
          </Button>

          {/* Guild Hero */}
          <div className="flex flex-col gap-4 sm:flex-row sm:items-start">
            {/* Logo */}
            <div className="flex-shrink-0">
              {guild.logo ? (
                <Image
                  src={guild.logo}
                  alt={guild.name}
                  width={96}
                  height={96}
                  unoptimized
                  className="border-border size-20 rounded-none border-2 object-cover sm:size-24"
                />
              ) : (
                <div className="border-border flex size-20 items-center justify-center rounded-none border-2 bg-slate-200 sm:size-24">
                  <Briefcase className="size-10 text-slate-400 sm:size-12" />
                </div>
              )}
            </div>

            <div className="min-w-0 flex-1">
              <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                <div className="min-w-0 flex-1">
                  <h1 className="text-foreground flex flex-wrap items-center gap-2 text-2xl font-bold sm:text-3xl">
                    {guild.name}
                    {guild.isVerified && (
                      <CheckCircle className="h-6 w-6 text-green-500" />
                    )}
                  </h1>
                  <div className="mt-2">{renderRating(guild.rating)}</div>
                </div>

                {isOwner && (
                  <Button
                    variant="outline"
                    asChild
                    className="w-full sm:w-auto"
                  >
                    <Link href={`/guilds/${slug}/edit`}>
                      <Edit className="mr-2 size-4" />
                      Edit Guild
                    </Link>
                  </Button>
                )}
              </div>

              <p className="text-muted-foreground mt-4 line-clamp-2">
                {guild.description}
              </p>

              {/* Specialties */}
              {guild.specialties && (
                <div className="mt-4 flex flex-wrap gap-2">
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
          <div className="mt-6 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5 lg:gap-4">
            <Card className="bg-card border-border border-2">
              <CardContent className="p-4 text-center">
                <p className="text-primary text-2xl font-bold">
                  {guild.projectsCount || 0}
                </p>
                <p className="text-muted-foreground text-sm">Projects</p>
              </CardContent>
            </Card>
            <Card className="bg-card border-border border-2">
              <CardContent className="p-4 text-center">
                <p className="text-foreground text-2xl font-bold">
                  {membersLoading
                    ? "…"
                    : membersError
                      ? "—"
                      : (members?.length ?? 0)}
                </p>
                <p className="text-muted-foreground text-sm">Members</p>
              </CardContent>
            </Card>
            <Card className="bg-card border-border border-2">
              <CardContent className="p-4 text-center">
                <div className="flex items-center justify-center gap-1">
                  <Shield className="text-system size-5" />
                  <p className="text-system text-2xl font-bold">
                    {credentialsLoading
                      ? "…"
                      : credentialsError
                        ? "—"
                        : (activeCredentials?.length ?? 0)}
                  </p>
                </div>
                <p className="text-muted-foreground text-sm">Credentialed</p>
              </CardContent>
            </Card>
            <Card className="bg-card border-border border-2">
              <CardContent className="p-4 text-center">
                <p className="text-success text-2xl font-bold">
                  {reviewsLoading
                    ? "…"
                    : reviewsError
                      ? "—"
                      : (reviews?.length ?? 0)}
                </p>
                <p className="text-muted-foreground text-sm">Reviews</p>
              </CardContent>
            </Card>
            <Card className="bg-card border-border border-2">
              <CardContent className="p-4 text-center">
                <p className="text-foreground text-2xl font-bold">
                  {guild.rating ? guild.rating.toFixed(1) : "-"}
                </p>
                <p className="text-muted-foreground text-sm">Rating</p>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="border-border bg-background relative z-10 border-b-2">
        <div className="mx-auto max-w-4xl px-4">
          <div
            className="flex overflow-x-auto"
            role="tablist"
            aria-label="Guild details"
            onKeyDown={handleTabListKeyDown}
          >
            {tabs.map((tab) => {
              return (
                <button
                  key={tab.id}
                  type="button"
                  onClick={() => setActiveTab(tab.id)}
                  role="tab"
                  id={`guild-workspace-tab-${tab.id}`}
                  aria-selected={activeTab === tab.id}
                  aria-controls={`guild-workspace-panel-${tab.id}`}
                  tabIndex={activeTab === tab.id ? 0 : -1}
                  className={cn(
                    "relative flex min-h-11 items-center gap-2 px-4 py-3 text-sm font-medium whitespace-nowrap",
                    activeTab === tab.id
                      ? "text-primary"
                      : "text-muted-foreground hover:text-foreground",
                  )}
                >
                  {tab.label}
                  {activeTab === tab.id && (
                    <span className="bg-primary absolute right-0 bottom-0 left-0 h-0.5" />
                  )}
                </button>
              );
            })}
          </div>
        </div>
      </div>

      {/* Tab Content */}
      <div
        className="mx-auto max-w-4xl px-4 py-6"
        role="tabpanel"
        id={`guild-workspace-panel-${activeTab}`}
        aria-labelledby={`guild-workspace-tab-${activeTab}`}
      >
        {activeTab === "overview" && <OverviewTab guild={guild} />}
        {activeTab === "updates" && (
          <UpdatesTab guildId={guild.id} isOwner={isOwner} />
        )}
        {activeTab === "members" && (
          <MembersTab guildId={guild.id} isOwner={isOwner} />
        )}
        {activeTab === "opportunities" && (
          <OpportunitiesTab
            guildId={guild.id}
            guildSlug={slug}
            isOwner={isOwner}
            userRole={userRole}
          />
        )}
        {activeTab === "reviews" && (
          <ReviewsTab
            guildId={guild.id}
            isOwner={isOwner}
            currentUserId={currentUserId}
          />
        )}
      </div>
    </div>
  );
}
