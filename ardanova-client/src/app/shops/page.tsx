"use client";

import { useState } from "react";
import Link from "next/link";
import {
  Search,
  Filter,
  Store,
  Plus,
  MessageCircle,
  Share2,
  Bookmark,
  MoreHorizontal,
  Sparkles,
  Clock,
  TrendingUp,
  Star,
  Package,
  ShoppingBag,
  Heart,
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

// Feed tabs for shops
const shopTabs = [
  { id: "all", label: "All Shops", icon: Store },
  { id: "trending", label: "Trending", icon: TrendingUp },
  { id: "newest", label: "Newest", icon: Clock },
  { id: "top-rated", label: "Top Rated", icon: Star },
];

// Category badge variants
const categoryVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary"> = {
  "Technology": "neon",
  "Design": "neon-pink",
  "Sustainability": "neon-green",
  "Handmade": "neon-purple",
  "Digital": "neon",
  "Services": "warning",
  "Food": "neon-green",
  "Fashion": "neon-pink",
};

// Filter options
const categoryFilters = [
  { id: "all", label: "All Categories" },
  { id: "Technology", label: "Technology" },
  { id: "Design", label: "Design" },
  { id: "Sustainability", label: "Sustainability" },
  { id: "Handmade", label: "Handmade" },
  { id: "Digital", label: "Digital" },
  { id: "Services", label: "Services" },
  { id: "Food", label: "Food" },
  { id: "Fashion", label: "Fashion" },
];

const ratingFilters = [
  { id: "all", label: "Any Rating" },
  { id: "4.5+", label: "4.5+ Stars" },
  { id: "4+", label: "4+ Stars" },
  { id: "3+", label: "3+ Stars" },
];

const productsFilters = [
  { id: "all", label: "Any Products" },
  { id: "10+", label: "10+ Products" },
  { id: "25+", label: "25+ Products" },
  { id: "50+", label: "50+ Products" },
];

const timeFilters = [
  { id: "all", label: "All Time" },
  { id: "week", label: "This Week" },
  { id: "month", label: "This Month" },
  { id: "quarter", label: "This Quarter" },
];

// Sample shop data
const sampleShops = [
  {
    id: "s1",
    name: "Eco Products Store",
    slug: "eco-products-store",
    description: "Sustainable and eco-friendly products from our community projects. We offer a range of items from recycled materials to organic produce.",
    owner: { id: "u1", name: "Sarah Chen", avatar: "https://i.pravatar.cc/150?u=sarah" },
    category: "Sustainability",
    productsCount: 24,
    rating: 4.8,
    reviewsCount: 156,
    isActive: true,
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 30),
  },
  {
    id: "s2",
    name: "Tech Gadgets Hub",
    slug: "tech-gadgets-hub",
    description: "Innovative tech products built by our developer community. From smart home devices to productivity tools.",
    owner: { id: "u7", name: "David Park", avatar: "https://i.pravatar.cc/150?u=david" },
    category: "Technology",
    productsCount: 15,
    rating: 4.5,
    reviewsCount: 89,
    isActive: true,
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 45),
  },
  {
    id: "s3",
    name: "Design Resources",
    slug: "design-resources",
    description: "Premium templates, icons, and design assets created by the Design Guild. Perfect for startups and projects.",
    owner: { id: "u6", name: "Emma Watson", avatar: "https://i.pravatar.cc/150?u=emma" },
    category: "Design",
    productsCount: 42,
    rating: 4.9,
    reviewsCount: 234,
    isActive: true,
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 60),
  },
  {
    id: "s4",
    name: "Handcrafted Goods",
    slug: "handcrafted-goods",
    description: "Unique handmade items from local artisans. Each piece tells a story and supports community craftspeople.",
    owner: { id: "u8", name: "Lisa Chen", avatar: "https://i.pravatar.cc/150?u=lisa" },
    category: "Handmade",
    productsCount: 67,
    rating: 4.7,
    reviewsCount: 178,
    isActive: true,
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 15),
  },
];

function formatRelativeTime(date: Date): string {
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffDay = Math.floor(diffMs / (1000 * 60 * 60 * 24));

  if (diffDay === 0) return "Today";
  if (diffDay === 1) return "Yesterday";
  if (diffDay < 7) return `${diffDay}d ago`;
  if (diffDay < 30) return `${Math.floor(diffDay / 7)}w ago`;
  return `${Math.floor(diffDay / 30)}mo ago`;
}

function renderRating(rating: number) {
  return (
    <div className="flex items-center gap-1 text-sm">
      <Star className="size-4 fill-neon-yellow text-neon-yellow" />
      <span className="font-medium text-foreground">{rating.toFixed(1)}</span>
    </div>
  );
}

export default function ShopsPage() {
  const [activeTab, setActiveTab] = useState("all");
  const [searchQuery, setSearchQuery] = useState("");
  const [showFilters, setShowFilters] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState("all");
  const [selectedRating, setSelectedRating] = useState("all");
  const [selectedProducts, setSelectedProducts] = useState("all");
  const [selectedTime, setSelectedTime] = useState("all");

  // Filter shops based on all criteria
  const filteredShops = sampleShops.filter((shop) => {
    // Tab filter
    if (activeTab === "trending" && shop.reviewsCount <= 100) return false;
    if (activeTab === "top-rated" && shop.rating < 4.7) return false;

    // Search query
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      const matchesName = shop.name.toLowerCase().includes(query);
      const matchesDescription = shop.description.toLowerCase().includes(query);
      const matchesCategory = shop.category.toLowerCase().includes(query);
      if (!matchesName && !matchesDescription && !matchesCategory) return false;
    }

    // Category filter
    if (selectedCategory !== "all" && shop.category !== selectedCategory) return false;

    // Rating filter
    if (selectedRating !== "all") {
      if (selectedRating === "4.5+" && shop.rating < 4.5) return false;
      if (selectedRating === "4+" && shop.rating < 4) return false;
      if (selectedRating === "3+" && shop.rating < 3) return false;
    }

    // Products filter
    if (selectedProducts !== "all") {
      if (selectedProducts === "10+" && shop.productsCount < 10) return false;
      if (selectedProducts === "25+" && shop.productsCount < 25) return false;
      if (selectedProducts === "50+" && shop.productsCount < 50) return false;
    }

    // Time filter
    if (selectedTime !== "all") {
      const now = new Date();
      const shopDate = new Date(shop.createdAt);
      const diffDays = (now.getTime() - shopDate.getTime()) / (1000 * 60 * 60 * 24);

      if (selectedTime === "week" && diffDays > 7) return false;
      if (selectedTime === "month" && diffDays > 30) return false;
      if (selectedTime === "quarter" && diffDays > 90) return false;
    }

    return true;
  });

  const hasActiveFilters =
    searchQuery ||
    selectedCategory !== "all" ||
    selectedRating !== "all" ||
    selectedProducts !== "all" ||
    selectedTime !== "all";

  const clearFilters = () => {
    setSearchQuery("");
    setSelectedCategory("all");
    setSelectedRating("all");
    setSelectedProducts("all");
    setSelectedTime("all");
  };

  const activeFilterCount =
    (selectedCategory !== "all" ? 1 : 0) +
    (selectedRating !== "all" ? 1 : 0) +
    (selectedProducts !== "all" ? 1 : 0) +
    (selectedTime !== "all" ? 1 : 0);

  // Stats for sidebar
  const stats = {
    total: sampleShops.length,
    totalProducts: sampleShops.reduce((sum, s) => sum + s.productsCount, 0),
    totalReviews: sampleShops.reduce((sum, s) => sum + s.reviewsCount, 0),
    avgRating: (sampleShops.reduce((sum, s) => sum + s.rating, 0) / sampleShops.length).toFixed(1),
  };

  // Top rated shops for sidebar
  const topRatedShops = [...sampleShops]
    .sort((a, b) => b.rating - a.rating)
    .slice(0, 3);

  return (
    <div className="min-h-screen bg-background">
      <div className="flex justify-center">
        {/* Main Feed Column - Centered */}
        <div className="w-full max-w-2xl border-x-2 border-border">
          {/* Header */}
          <div className="sticky top-0 z-10 bg-background/95 backdrop-blur border-b-2 border-border">
            <div className="p-4 flex items-center justify-between">
              <h1 className="text-xl font-bold text-foreground flex items-center gap-2">
                <Store className="size-5 text-neon-green" />
                Shops
              </h1>
              <Button variant="neon" size="sm" asChild>
                <Link href="/shops/create">
                  <Plus className="size-4 mr-2" />
                  Open Shop
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
                  placeholder="Search shops..."
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
                  {/* Category Filter */}
                  <div>
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Category
                    </label>
                    <select
                      value={selectedCategory}
                      onChange={(e) => setSelectedCategory(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
                    >
                      {categoryFilters.map((filter) => (
                        <option key={filter.id} value={filter.id}>
                          {filter.label}
                        </option>
                      ))}
                    </select>
                  </div>

                  {/* Rating Filter */}
                  <div>
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Minimum Rating
                    </label>
                    <select
                      value={selectedRating}
                      onChange={(e) => setSelectedRating(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
                    >
                      {ratingFilters.map((filter) => (
                        <option key={filter.id} value={filter.id}>
                          {filter.label}
                        </option>
                      ))}
                    </select>
                  </div>

                  {/* Products Filter */}
                  <div>
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Products Count
                    </label>
                    <select
                      value={selectedProducts}
                      onChange={(e) => setSelectedProducts(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
                    >
                      {productsFilters.map((filter) => (
                        <option key={filter.id} value={filter.id}>
                          {filter.label}
                        </option>
                      ))}
                    </select>
                  </div>

                  {/* Time Filter */}
                  <div>
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Created
                    </label>
                    <select
                      value={selectedTime}
                      onChange={(e) => setSelectedTime(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
                    >
                      {timeFilters.map((filter) => (
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
                      {selectedCategory !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {categoryFilters.find((f) => f.id === selectedCategory)?.label}
                          <button onClick={() => setSelectedCategory("all")}>
                            <X className="size-3" />
                          </button>
                        </Badge>
                      )}
                      {selectedRating !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {ratingFilters.find((f) => f.id === selectedRating)?.label}
                          <button onClick={() => setSelectedRating("all")}>
                            <X className="size-3" />
                          </button>
                        </Badge>
                      )}
                      {selectedProducts !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {productsFilters.find((f) => f.id === selectedProducts)?.label}
                          <button onClick={() => setSelectedProducts("all")}>
                            <X className="size-3" />
                          </button>
                        </Badge>
                      )}
                      {selectedTime !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {timeFilters.find((f) => f.id === selectedTime)?.label}
                          <button onClick={() => setSelectedTime("all")}>
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
              {shopTabs.map((tab) => {
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

          {/* Shops Feed */}
          <div>
            {filteredShops.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-20 px-4">
                <Store className="size-12 text-muted-foreground mb-4" />
                <p className="text-lg font-medium text-foreground">No shops found</p>
                <p className="text-muted-foreground mt-1">Be the first to open one!</p>
                <Button variant="neon" className="mt-4" asChild>
                  <Link href="/shops/create">
                    <Plus className="size-4 mr-2" />
                    Open Shop
                  </Link>
                </Button>
              </div>
            ) : (
              filteredShops.map((shop) => (
                <article
                  key={shop.id}
                  className="border-b-2 border-border bg-card hover:bg-card/80 transition-colors"
                >
                  <div className="p-4">
                    {/* Header */}
                    <div className="flex items-start gap-3">
                      <Link href={`/shops/${shop.slug}`} className="shrink-0">
                        <div className="size-12 bg-neon-green/20 border-2 border-neon-green flex items-center justify-center hover:border-primary transition-colors">
                          <Store className="size-6 text-neon-green" />
                        </div>
                      </Link>

                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 flex-wrap">
                          <Link
                            href={`/shops/${shop.slug}`}
                            className="font-medium text-foreground hover:text-primary transition-colors"
                          >
                            {shop.name}
                          </Link>
                          <Badge variant="neon-green" size="sm">Shop</Badge>
                          <span className="text-muted-foreground text-sm">·</span>
                          <span className="text-muted-foreground text-sm">
                            {formatRelativeTime(shop.createdAt)}
                          </span>
                        </div>
                        <div className="flex items-center gap-3 mt-1">
                          {renderRating(shop.rating)}
                          <span className="text-sm text-muted-foreground">
                            {shop.reviewsCount} reviews
                          </span>
                        </div>
                      </div>

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

                    {/* Shop Content */}
                    <Link href={`/shops/${shop.slug}`} className="block mt-3 pl-15">
                      <p className="text-foreground line-clamp-3">
                        {shop.description}
                      </p>

                      {/* Category */}
                      <div className="flex flex-wrap gap-2 mt-3">
                        <Badge
                          variant={categoryVariants[shop.category] ?? "secondary"}
                          size="sm"
                        >
                          {shop.category}
                        </Badge>
                      </div>

                      {/* Stats */}
                      <div className="flex items-center gap-4 mt-3 text-sm text-muted-foreground">
                        <div className="flex items-center gap-1">
                          <Package className="size-4" />
                          <span>{shop.productsCount} products</span>
                        </div>
                        <div className="flex items-center gap-1">
                          <ShoppingBag className="size-4" />
                          <span>by</span>
                          <Link
                            href={`/dashboard/profile/${shop.owner.id}`}
                            className="hover:text-primary transition-colors"
                          >
                            {shop.owner.name}
                          </Link>
                        </div>
                      </div>
                    </Link>

                    {/* Actions */}
                    <div className="mt-4 pl-15 flex items-center gap-1">
                      <Button
                        variant="ghost"
                        size="sm"
                        className="gap-1.5 text-muted-foreground hover:text-neon-pink"
                      >
                        <Heart className="size-4" />
                        <span className="text-xs">Follow</span>
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="gap-1.5 text-muted-foreground hover:text-primary"
                      >
                        <MessageCircle className="size-4" />
                        <span className="text-xs">Contact</span>
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
            {filteredShops.length > 0 && (
              <div className="flex justify-center py-6">
                <Button variant="outline">Load more shops</Button>
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
                <span className="text-sm text-muted-foreground">Total Shops</span>
                <span className="font-medium text-foreground">{stats.total}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Total Products</span>
                <span className="font-medium text-foreground">{stats.totalProducts}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Total Reviews</span>
                <span className="font-medium text-foreground">{stats.totalReviews}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Avg Rating</span>
                <span className="font-medium text-neon-yellow">{stats.avgRating} / 5</span>
              </div>
            </CardContent>
          </Card>

          {/* Top Rated Shops */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base flex items-center gap-2">
                <Star className="size-4 text-neon-yellow" />
                Top Rated Shops
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {topRatedShops.map((shop) => (
                <Link
                  key={shop.id}
                  href={`/shops/${shop.slug}`}
                  className="flex items-center gap-3"
                >
                  <div className="size-10 bg-neon-green/20 border-2 border-border flex items-center justify-center">
                    <Store className="size-4 text-neon-green" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="font-medium text-sm text-foreground hover:text-primary transition-colors truncate">
                      {shop.name}
                    </p>
                    <div className="flex items-center gap-2">
                      {renderRating(shop.rating)}
                      <span className="text-xs text-muted-foreground">
                        ({shop.reviewsCount})
                      </span>
                    </div>
                  </div>
                </Link>
              ))}
              <Button variant="ghost" className="w-full text-sm" asChild>
                <Link href="/shops?tab=top-rated">View all top rated</Link>
              </Button>
            </CardContent>
          </Card>

          {/* Categories */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base flex items-center gap-2">
                <Filter className="size-4 text-neon-pink" />
                Categories
              </CardTitle>
            </CardHeader>
            <CardContent className="flex flex-wrap gap-2">
              {Object.keys(categoryVariants).map((category) => (
                <Badge
                  key={category}
                  variant={categoryVariants[category]}
                  size="sm"
                  className="cursor-pointer hover:opacity-80"
                >
                  {category}
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
