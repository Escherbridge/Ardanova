"use client";

import { useState } from "react";
import Link from "next/link";
import {
  Search,
  Filter,
  Briefcase,
  Plus,
  MessageCircle,
  Share2,
  Bookmark,
  MoreHorizontal,
  Sparkles,
  Clock,
  TrendingUp,
  DollarSign,
  MapPin,
  Calendar,
  Zap,
  Target,
  Users,
  SlidersHorizontal,
  X,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "~/components/ui/dropdown-menu";
import { cn } from "~/lib/utils";

// Feed tabs for opportunities
const opportunityTabs = [
  { id: "all", label: "All", icon: Briefcase },
  { id: "bounties", label: "Bounties", icon: Target },
  { id: "freelance", label: "Freelance", icon: Zap },
  { id: "full-time", label: "Full-time", icon: Users },
];

// Type badge variants
const typeVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary"> = {
  "Bounty": "neon-green",
  "Freelance": "neon",
  "Part-time": "neon-purple",
  "Full-time": "neon-pink",
  "Contract": "warning",
};

// Skill badge variants
const skillVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary"> = {
  "React": "neon",
  "TypeScript": "neon",
  "Node.js": "neon-green",
  "Python": "neon-green",
  "UI/UX": "neon-pink",
  "Design": "neon-pink",
  "Solidity": "neon-purple",
  "Web3": "neon-purple",
  "Marketing": "warning",
  "Writing": "secondary",
};

// Filter options
const typeFilters = [
  { id: "all", label: "All Types" },
  { id: "Bounty", label: "Bounty" },
  { id: "Freelance", label: "Freelance" },
  { id: "Contract", label: "Contract" },
  { id: "Part-time", label: "Part-time" },
  { id: "Full-time", label: "Full-time" },
];

const skillFilters = [
  { id: "all", label: "All Skills" },
  { id: "React", label: "React" },
  { id: "TypeScript", label: "TypeScript" },
  { id: "Node.js", label: "Node.js" },
  { id: "Python", label: "Python" },
  { id: "UI/UX", label: "UI/UX" },
  { id: "Solidity", label: "Solidity" },
  { id: "Web3", label: "Web3" },
  { id: "Marketing", label: "Marketing" },
];

const compensationFilters = [
  { id: "all", label: "Any Budget" },
  { id: "0-1000", label: "Under $1,000" },
  { id: "1000-5000", label: "$1,000 - $5,000" },
  { id: "5000-10000", label: "$5,000 - $10,000" },
  { id: "10000+", label: "Over $10,000" },
];

const locationFilters = [
  { id: "all", label: "All Locations" },
  { id: "Remote", label: "Remote" },
  { id: "US", label: "United States" },
  { id: "EU", label: "Europe" },
  { id: "Asia", label: "Asia" },
];

// Sample opportunities data
const sampleOpportunities = [
  {
    id: "o1",
    title: "Build Mobile App for EcoWaste Platform",
    type: "Bounty",
    project: { id: "p1", name: "EcoWaste Solutions", slug: "ecowaste-solutions" },
    poster: { id: "u1", name: "Sarah Chen", avatar: "https://i.pravatar.cc/150?u=sarah" },
    description: "We need a React Native developer to build our mobile app for waste tracking and recycling rewards. The app should integrate with our existing API.",
    skills: ["React", "TypeScript", "Node.js"],
    compensation: { amount: 2500, currency: "USD", type: "fixed" },
    location: "Remote",
    deadline: new Date(Date.now() + 1000 * 60 * 60 * 24 * 14),
    applicants: 8,
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 2),
    isUrgent: true,
  },
  {
    id: "o2",
    title: "Smart Contract Audit for Token Launch",
    type: "Freelance",
    project: { id: "p2", name: "HealthTrack", slug: "healthtrack" },
    poster: { id: "u2", name: "Marcus Rodriguez", avatar: "https://i.pravatar.cc/150?u=marcus" },
    description: "Looking for an experienced Solidity auditor to review our governance token smart contracts before mainnet deployment.",
    skills: ["Solidity", "Web3"],
    compensation: { amount: 150, currency: "USD", type: "hourly" },
    location: "Remote",
    deadline: new Date(Date.now() + 1000 * 60 * 60 * 24 * 7),
    applicants: 3,
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 1),
    isUrgent: false,
  },
  {
    id: "o3",
    title: "Full-Stack Developer for EdTech Platform",
    type: "Full-time",
    project: { id: "p3", name: "EduConnect", slug: "educonnect" },
    poster: { id: "u5", name: "Jordan Lee", avatar: "https://i.pravatar.cc/150?u=jordan" },
    description: "Join our team as a full-time developer to build and maintain our mentorship platform. You'll work on both frontend and backend features.",
    skills: ["React", "TypeScript", "Python"],
    compensation: { amount: 95000, currency: "USD", type: "yearly" },
    location: "San Francisco, CA",
    deadline: null,
    applicants: 24,
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 5),
    isUrgent: false,
  },
  {
    id: "o4",
    title: "Design System Creation for Design Guild",
    type: "Contract",
    project: { id: "g1", name: "Design Guild", slug: "design-guild" },
    poster: { id: "u6", name: "Emma Watson", avatar: "https://i.pravatar.cc/150?u=emma" },
    description: "Create a comprehensive design system including components, tokens, and documentation. 3-month contract with potential extension.",
    skills: ["UI/UX", "Design"],
    compensation: { amount: 8000, currency: "USD", type: "fixed" },
    location: "Remote",
    deadline: new Date(Date.now() + 1000 * 60 * 60 * 24 * 30),
    applicants: 12,
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 3),
    isUrgent: false,
  },
];

function formatRelativeTime(date: Date): string {
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffDay = Math.floor(diffMs / (1000 * 60 * 60 * 24));

  if (diffDay === 0) return "Today";
  if (diffDay === 1) return "Yesterday";
  if (diffDay < 7) return `${diffDay}d ago`;
  return date.toLocaleDateString();
}

function formatDeadline(date: Date | null): string {
  if (!date) return "Open";
  const now = new Date();
  const diffMs = date.getTime() - now.getTime();
  const diffDay = Math.floor(diffMs / (1000 * 60 * 60 * 24));

  if (diffDay < 0) return "Expired";
  if (diffDay === 0) return "Today";
  if (diffDay === 1) return "Tomorrow";
  if (diffDay < 7) return `${diffDay} days left`;
  return `${Math.floor(diffDay / 7)} weeks left`;
}

function formatCompensation(comp: { amount: number; currency: string; type: string }): string {
  const formatted = new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: comp.currency,
    maximumFractionDigits: 0,
  }).format(comp.amount);

  switch (comp.type) {
    case "hourly":
      return `${formatted}/hr`;
    case "yearly":
      return `${formatted}/yr`;
    default:
      return formatted;
  }
}

export default function MarketplacePage() {
  const [activeTab, setActiveTab] = useState("all");
  const [searchQuery, setSearchQuery] = useState("");
  const [showFilters, setShowFilters] = useState(false);
  const [selectedType, setSelectedType] = useState("all");
  const [selectedSkill, setSelectedSkill] = useState("all");
  const [selectedCompensation, setSelectedCompensation] = useState("all");
  const [selectedLocation, setSelectedLocation] = useState("all");

  // Filter opportunities based on all criteria
  const filteredOpportunities = sampleOpportunities.filter((opp) => {
    // Tab filter
    if (activeTab === "bounties" && opp.type !== "Bounty") return false;
    if (activeTab === "freelance" && opp.type !== "Freelance" && opp.type !== "Contract") return false;
    if (activeTab === "full-time" && opp.type !== "Full-time" && opp.type !== "Part-time") return false;

    // Search query
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      const matchesTitle = opp.title.toLowerCase().includes(query);
      const matchesDescription = opp.description.toLowerCase().includes(query);
      const matchesSkills = opp.skills.some((s) => s.toLowerCase().includes(query));
      if (!matchesTitle && !matchesDescription && !matchesSkills) return false;
    }

    // Type filter
    if (selectedType !== "all" && opp.type !== selectedType) return false;

    // Skill filter
    if (selectedSkill !== "all" && !opp.skills.includes(selectedSkill)) return false;

    // Compensation filter
    if (selectedCompensation !== "all" && opp.compensation.type === "fixed") {
      const amount = opp.compensation.amount;
      if (selectedCompensation === "0-1000" && amount >= 1000) return false;
      if (selectedCompensation === "1000-5000" && (amount < 1000 || amount >= 5000)) return false;
      if (selectedCompensation === "5000-10000" && (amount < 5000 || amount >= 10000)) return false;
      if (selectedCompensation === "10000+" && amount < 10000) return false;
    }

    // Location filter
    if (selectedLocation !== "all") {
      if (selectedLocation === "Remote" && opp.location !== "Remote") return false;
      if (selectedLocation === "US" && !opp.location.includes("CA") && !opp.location.includes("NY") && !opp.location.includes("US")) return false;
    }

    return true;
  });

  const hasActiveFilters =
    searchQuery ||
    selectedType !== "all" ||
    selectedSkill !== "all" ||
    selectedCompensation !== "all" ||
    selectedLocation !== "all";

  const clearFilters = () => {
    setSearchQuery("");
    setSelectedType("all");
    setSelectedSkill("all");
    setSelectedCompensation("all");
    setSelectedLocation("all");
  };

  const activeFilterCount =
    (selectedType !== "all" ? 1 : 0) +
    (selectedSkill !== "all" ? 1 : 0) +
    (selectedCompensation !== "all" ? 1 : 0) +
    (selectedLocation !== "all" ? 1 : 0);

  // Stats for sidebar
  const stats = {
    total: sampleOpportunities.length,
    bounties: sampleOpportunities.filter((o) => o.type === "Bounty").length,
    totalValue: sampleOpportunities
      .filter((o) => o.compensation.type === "fixed")
      .reduce((sum, o) => sum + o.compensation.amount, 0),
    totalApplicants: sampleOpportunities.reduce((sum, o) => sum + o.applicants, 0),
  };

  // Hot opportunities for sidebar
  const hotOpportunities = [...sampleOpportunities]
    .sort((a, b) => b.applicants - a.applicants)
    .slice(0, 3);

  return (
    <div className="min-h-screen bg-background">
      <div className="flex">
        {/* Main Feed Column */}
        <div className="w-full max-w-2xl border-x-2 border-border">
          {/* Header */}
          <div className="sticky top-0 z-10 bg-background/95 backdrop-blur border-b-2 border-border">
            <div className="p-4 flex items-center justify-between">
              <h1 className="text-xl font-bold text-foreground flex items-center gap-2">
                <Briefcase className="size-5 text-warning" />
                Opportunities
              </h1>
              <Button variant="neon" size="sm" asChild>
                <Link href="/marketplace/create">
                  <Plus className="size-4 mr-2" />
                  Post Job
                </Link>
              </Button>
            </div>

            {/* Search */}
            <div className="px-4 pb-3 flex items-center gap-2">
              <div className="relative flex-1">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
                <input
                  type="text"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  placeholder="Search opportunities..."
                  className="w-full pl-10 pr-4 py-2 bg-card border-2 border-border text-foreground text-sm placeholder:text-muted-foreground focus:border-primary focus:outline-none"
                />
                {searchQuery && (
                  <button
                    onClick={() => setSearchQuery("")}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                  >
                    <X className="size-4" />
                  </button>
                )}
              </div>
              <Button
                variant={showFilters ? "neon" : "outline"}
                size="sm"
                onClick={() => setShowFilters(!showFilters)}
                className="gap-1.5"
              >
                <SlidersHorizontal className="size-4" />
                Filters
                {activeFilterCount > 0 && !showFilters && (
                  <Badge variant="neon" size="sm" className="ml-1">
                    {activeFilterCount}
                  </Badge>
                )}
              </Button>
            </div>

            {/* Expanded Filters */}
            {showFilters && (
              <div className="px-4 pb-4 space-y-3 border-t border-border pt-3">
                <div className="grid grid-cols-2 gap-3">
                  {/* Type Filter */}
                  <div>
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Job Type
                    </label>
                    <select
                      value={selectedType}
                      onChange={(e) => setSelectedType(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
                    >
                      {typeFilters.map((filter) => (
                        <option key={filter.id} value={filter.id}>
                          {filter.label}
                        </option>
                      ))}
                    </select>
                  </div>

                  {/* Skill Filter */}
                  <div>
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Required Skill
                    </label>
                    <select
                      value={selectedSkill}
                      onChange={(e) => setSelectedSkill(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
                    >
                      {skillFilters.map((filter) => (
                        <option key={filter.id} value={filter.id}>
                          {filter.label}
                        </option>
                      ))}
                    </select>
                  </div>

                  {/* Compensation Filter */}
                  <div>
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Budget Range
                    </label>
                    <select
                      value={selectedCompensation}
                      onChange={(e) => setSelectedCompensation(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
                    >
                      {compensationFilters.map((filter) => (
                        <option key={filter.id} value={filter.id}>
                          {filter.label}
                        </option>
                      ))}
                    </select>
                  </div>

                  {/* Location Filter */}
                  <div>
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Location
                    </label>
                    <select
                      value={selectedLocation}
                      onChange={(e) => setSelectedLocation(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
                    >
                      {locationFilters.map((filter) => (
                        <option key={filter.id} value={filter.id}>
                          {filter.label}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                {hasActiveFilters && (
                  <div className="flex items-center justify-between pt-2">
                    <div className="flex flex-wrap gap-2">
                      {searchQuery && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          Search: {searchQuery}
                          <button onClick={() => setSearchQuery("")}>
                            <X className="size-3" />
                          </button>
                        </Badge>
                      )}
                      {selectedType !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {typeFilters.find((f) => f.id === selectedType)?.label}
                          <button onClick={() => setSelectedType("all")}>
                            <X className="size-3" />
                          </button>
                        </Badge>
                      )}
                      {selectedSkill !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {skillFilters.find((f) => f.id === selectedSkill)?.label}
                          <button onClick={() => setSelectedSkill("all")}>
                            <X className="size-3" />
                          </button>
                        </Badge>
                      )}
                      {selectedCompensation !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {compensationFilters.find((f) => f.id === selectedCompensation)?.label}
                          <button onClick={() => setSelectedCompensation("all")}>
                            <X className="size-3" />
                          </button>
                        </Badge>
                      )}
                      {selectedLocation !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {locationFilters.find((f) => f.id === selectedLocation)?.label}
                          <button onClick={() => setSelectedLocation("all")}>
                            <X className="size-3" />
                          </button>
                        </Badge>
                      )}
                    </div>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={clearFilters}
                      className="text-muted-foreground"
                    >
                      Clear all
                    </Button>
                  </div>
                )}
              </div>
            )}

            {/* Tabs */}
            <div className="flex border-b-2 border-border">
              {opportunityTabs.map((tab) => {
                const Icon = tab.icon;
                return (
                  <button
                    key={tab.id}
                    onClick={() => setActiveTab(tab.id)}
                    className={cn(
                      "flex-1 flex items-center justify-center gap-2 py-3 text-sm font-medium transition-colors relative",
                      activeTab === tab.id
                        ? "text-primary"
                        : "text-muted-foreground hover:text-foreground hover:bg-card"
                    )}
                  >
                    <Icon className="size-4" />
                    <span className="hidden sm:inline">{tab.label}</span>
                    {activeTab === tab.id && (
                      <span className="absolute bottom-0 left-0 right-0 h-0.5 bg-primary" />
                    )}
                  </button>
                );
              })}
            </div>
          </div>

          {/* Opportunities Feed */}
          <div>
            {filteredOpportunities.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-20 px-4">
                <Briefcase className="size-12 text-muted-foreground mb-4" />
                <p className="text-lg font-medium text-foreground">No opportunities found</p>
                <p className="text-muted-foreground mt-1">Check back later or post your own!</p>
                <Button variant="neon" className="mt-4" asChild>
                  <Link href="/marketplace/create">
                    <Plus className="size-4 mr-2" />
                    Post Job
                  </Link>
                </Button>
              </div>
            ) : (
              filteredOpportunities.map((opp) => (
                <article
                  key={opp.id}
                  className="border-b-2 border-border bg-card hover:bg-card/80 transition-colors"
                >
                  <div className="p-4">
                    {/* Header */}
                    <div className="flex items-start gap-3">
                      <Link
                        href={`/dashboard/profile/${opp.poster.id}`}
                        className="shrink-0"
                      >
                        <Avatar className="size-10 border-2 border-border hover:border-primary transition-colors">
                          <AvatarImage src={opp.poster.avatar} />
                          <AvatarFallback>{opp.poster.name.charAt(0)}</AvatarFallback>
                        </Avatar>
                      </Link>

                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 flex-wrap">
                          <Link
                            href={`/dashboard/profile/${opp.poster.id}`}
                            className="font-medium text-foreground hover:text-primary transition-colors"
                          >
                            {opp.poster.name}
                          </Link>
                          <span className="text-muted-foreground text-sm">·</span>
                          <Link
                            href={`/projects/${opp.project.slug}`}
                            className="text-sm text-muted-foreground hover:text-primary transition-colors"
                          >
                            {opp.project.name}
                          </Link>
                          <span className="text-muted-foreground text-sm">·</span>
                          <span className="text-muted-foreground text-sm">
                            {formatRelativeTime(opp.createdAt)}
                          </span>
                        </div>
                        <p className="text-sm text-muted-foreground mt-0.5">
                          posted a new opportunity
                        </p>
                      </div>

                      <div className="flex items-center gap-2">
                        {opp.isUrgent && (
                          <Badge variant="destructive" size="sm">Urgent</Badge>
                        )}
                        <Badge
                          variant={typeVariants[opp.type] ?? "secondary"}
                          size="sm"
                        >
                          {opp.type}
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
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </div>
                    </div>

                    {/* Opportunity Content */}
                    <Link href={`/marketplace/${opp.id}`} className="block mt-3 pl-13">
                      <h3 className="font-semibold text-lg text-foreground hover:text-primary transition-colors">
                        {opp.title}
                      </h3>
                      <p className="text-foreground mt-2 line-clamp-2">
                        {opp.description}
                      </p>

                      {/* Skills */}
                      <div className="flex flex-wrap gap-2 mt-3">
                        {opp.skills.map((skill, i) => (
                          <Badge
                            key={i}
                            variant={skillVariants[skill] ?? "secondary"}
                            size="sm"
                          >
                            {skill}
                          </Badge>
                        ))}
                      </div>

                      {/* Meta Info */}
                      <div className="flex flex-wrap items-center gap-4 mt-3 text-sm text-muted-foreground">
                        <div className="flex items-center gap-1 text-neon-green font-medium">
                          <DollarSign className="size-4" />
                          <span>{formatCompensation(opp.compensation)}</span>
                        </div>
                        <div className="flex items-center gap-1">
                          <MapPin className="size-4" />
                          <span>{opp.location}</span>
                        </div>
                        <div className="flex items-center gap-1">
                          <Calendar className="size-4" />
                          <span>{formatDeadline(opp.deadline)}</span>
                        </div>
                        <div className="flex items-center gap-1">
                          <Users className="size-4" />
                          <span>{opp.applicants} applicants</span>
                        </div>
                      </div>
                    </Link>

                    {/* Actions */}
                    <div className="mt-4 pl-13 flex items-center gap-1">
                      <Button
                        variant="neon"
                        size="sm"
                        className="gap-1.5"
                      >
                        <Zap className="size-4" />
                        <span className="text-xs">Apply Now</span>
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="gap-1.5 text-muted-foreground hover:text-primary"
                      >
                        <MessageCircle className="size-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="gap-1.5 text-muted-foreground hover:text-neon-green"
                      >
                        <Share2 className="size-4" />
                      </Button>
                      <div className="flex-1" />
                      <Button
                        variant="ghost"
                        size="icon-sm"
                        className="text-muted-foreground hover:text-neon-yellow"
                      >
                        <Bookmark className="size-4" />
                      </Button>
                    </div>
                  </div>
                </article>
              ))
            )}

            {/* Load More */}
            {filteredOpportunities.length > 0 && (
              <div className="flex justify-center py-6">
                <Button variant="outline">Load more opportunities</Button>
              </div>
            )}
          </div>
        </div>

        {/* Right Sidebar - Fixed to right edge */}
        <div className="hidden xl:block fixed right-0 top-0 w-80 p-4 space-y-4 h-screen overflow-y-auto border-l-2 border-border bg-background">
          {/* Stats */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base flex items-center gap-2">
                <Sparkles className="size-4 text-neon-yellow" />
                Marketplace Stats
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Open Opportunities</span>
                <span className="font-medium text-foreground">{stats.total}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Active Bounties</span>
                <span className="font-medium text-neon-green">{stats.bounties}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Total Value</span>
                <span className="font-medium text-foreground">${stats.totalValue.toLocaleString()}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Total Applicants</span>
                <span className="font-medium text-foreground">{stats.totalApplicants}</span>
              </div>
            </CardContent>
          </Card>

          {/* Hot Opportunities */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base flex items-center gap-2">
                <TrendingUp className="size-4 text-neon-pink" />
                Hot Opportunities
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {hotOpportunities.map((opp) => (
                <Link
                  key={opp.id}
                  href={`/marketplace/${opp.id}`}
                  className="block"
                >
                  <div className="flex items-start justify-between mb-1">
                    <p className="font-medium text-sm text-foreground hover:text-primary transition-colors line-clamp-1">
                      {opp.title}
                    </p>
                    <Badge
                      variant={typeVariants[opp.type] ?? "secondary"}
                      size="sm"
                    >
                      {opp.type}
                    </Badge>
                  </div>
                  <div className="flex items-center gap-2 text-xs text-muted-foreground">
                    <span className="text-neon-green font-medium">
                      {formatCompensation(opp.compensation)}
                    </span>
                    <span>·</span>
                    <span>{opp.applicants} applicants</span>
                  </div>
                </Link>
              ))}
              <Button variant="ghost" className="w-full text-sm" asChild>
                <Link href="/marketplace?tab=all">View all opportunities</Link>
              </Button>
            </CardContent>
          </Card>

          {/* Skills Filter */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base flex items-center gap-2">
                <Filter className="size-4 text-neon-purple" />
                Skills
              </CardTitle>
            </CardHeader>
            <CardContent className="flex flex-wrap gap-2">
              {Object.keys(skillVariants).map((skill) => (
                <Badge
                  key={skill}
                  variant={skillVariants[skill]}
                  size="sm"
                  className="cursor-pointer hover:opacity-80"
                >
                  {skill}
                </Badge>
              ))}
            </CardContent>
          </Card>

          {/* Footer */}
          <div className="text-xs text-muted-foreground space-x-2 px-2">
            <Link href="/terms" className="hover:underline">Terms</Link>
            <span>·</span>
            <Link href="/privacy" className="hover:underline">Privacy</Link>
            <span>·</span>
            <Link href="/help" className="hover:underline">Help</Link>
            <p className="mt-2">&copy; 2024 ArdaNova</p>
          </div>
        </div>
      </div>
    </div>
  );
}
