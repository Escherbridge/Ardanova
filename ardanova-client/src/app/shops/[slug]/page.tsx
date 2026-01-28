"use client";

import { useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { useSession } from "next-auth/react";
import {
  ArrowLeft,
  Heart,
  Share2,
  Bookmark,
  MoreHorizontal,
  Store,
  ShoppingBag,
  Star,
  Info,
  Briefcase,
  Loader2,
  Edit,
  Trash2,
} from "lucide-react";
import { toast } from "sonner";

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
import { useShopMutations } from "~/hooks/use-shop-mutations";

import {
  OverviewTab,
  ProductsTab,
  ReviewsTab,
  AboutTab,
  OpportunitiesTab,
} from "~/components/shops";

const tabs = [
  { id: "overview", label: "Overview", icon: Info },
  { id: "products", label: "Products", icon: ShoppingBag },
  { id: "opportunities", label: "Opportunities", icon: Briefcase },
  { id: "reviews", label: "Reviews", icon: Star },
  { id: "about", label: "About", icon: Store },
];

const categoryVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary"> = {
  RETAIL: "neon",
  SERVICES: "warning",
  DIGITAL_PRODUCTS: "neon",
  FOOD_BEVERAGE: "neon-green",
  HEALTH_WELLNESS: "neon-pink",
  TECHNOLOGY: "neon",
  FASHION: "neon-pink",
  HOME_GARDEN: "neon-green",
  ARTS_CRAFTS: "neon-purple",
  EDUCATION: "neon-purple",
  OTHER: "secondary",
};

export default function ShopDetailPage() {
  const params = useParams();
  const slug = params.slug as string;
  const [activeTab, setActiveTab] = useState("overview");
  const { data: session } = useSession();

  const { data: shop, isLoading } = api.shop.getById.useQuery({ id: slug });

  // Use shop mutations hook for optimistic updates
  const {
    followShop,
    unfollowShop,
    isFollowing: isFollowPending,
    isUnfollowing: isUnfollowPending
  } = useShopMutations();

  // Track follow state from shop data (will be populated when backend returns this)
  const isFollowing = (shop as any)?.isFollowing ?? false;
  const followerCount = (shop as any)?.followerCount ?? 0;

  // Determine if current user is owner
  const currentUserId = session?.user?.id;
  const isOwner = !!currentUserId && shop?.ownerId === currentUserId;

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="size-8 animate-spin text-primary" />
      </div>
    );
  }

  if (!shop) {
    return (
      <div className="min-h-screen bg-background flex flex-col items-center justify-center">
        <h1 className="text-2xl font-bold text-foreground">Shop not found</h1>
        <Button asChild className="mt-4">
          <Link href="/shops">Back to Shops</Link>
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
            <Link href="/shops">
              <ArrowLeft className="size-4 mr-2" />
              Back to Shops
            </Link>
          </Button>

          {/* Shop Hero */}
          <div className="flex items-start gap-4">
            <div className="flex-1">
              <div className="flex items-center gap-4 mb-2">
                {/* Shop Logo */}
                <div className="size-16 rounded-lg bg-card border-2 border-border flex items-center justify-center">
                  {shop.logo ? (
                    <img src={shop.logo} alt={shop.name} className="size-full rounded-lg object-cover" />
                  ) : (
                    <Store className="size-8 text-muted-foreground" />
                  )}
                </div>
                <div>
                  <h1 className="text-3xl font-bold text-foreground">{shop.name}</h1>
                  <div className="flex items-center gap-2 mt-1">
                    <Badge variant={categoryVariants[shop.category] || "secondary"}>
                      {shop.category.replace("_", " ")}
                    </Badge>
                    <Badge variant={shop.isActive ? "neon-green" : "secondary"}>
                      {shop.isActive ? "Active" : "Inactive"}
                    </Badge>
                  </div>
                </div>
              </div>

              <div className="flex items-center gap-3 mt-3">
                <Avatar className="size-8">
                  <AvatarImage src={(shop as any).owner?.image} />
                  <AvatarFallback>{(shop as any).owner?.name?.charAt(0) || "U"}</AvatarFallback>
                </Avatar>
                <div>
                  <p className="font-medium text-sm text-foreground">
                    {(shop as any).owner?.name || "Unknown"}
                  </p>
                  <p className="text-xs text-muted-foreground">Shop Owner</p>
                </div>
              </div>
            </div>

            {/* Action Buttons */}
            <div className="flex items-center gap-2">
              {!isOwner && (
                <Button
                  variant={isFollowing ? "outline" : "neon"}
                  onClick={() => {
                    if (isFollowing) {
                      unfollowShop.mutate({ shopId: shop.id });
                    } else {
                      followShop.mutate({ shopId: shop.id });
                    }
                  }}
                  disabled={isFollowPending || isUnfollowPending}
                >
                  <Heart className={cn("size-4 mr-2", isFollowing && "fill-current")} />
                  {isFollowPending || isUnfollowPending
                    ? "..."
                    : isFollowing
                      ? "Following"
                      : "Follow"}
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
                      Edit Shop
                    </DropdownMenuItem>
                    <DropdownMenuItem className="text-destructive">
                      <Trash2 className="size-4 mr-2" />
                      Delete Shop
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
                <p className="text-2xl font-bold text-primary">0</p>
                <p className="text-sm text-muted-foreground">Products</p>
              </CardContent>
            </Card>
            <Card className="bg-card border-2 border-border">
              <CardContent className="p-4 text-center">
                <p className="text-2xl font-bold text-foreground">0</p>
                <p className="text-sm text-muted-foreground">Reviews</p>
              </CardContent>
            </Card>
            <Card className="bg-card border-2 border-border">
              <CardContent className="p-4 text-center">
                <p className="text-2xl font-bold text-neon-green">{followerCount}</p>
                <p className="text-sm text-muted-foreground">Followers</p>
              </CardContent>
            </Card>
            <Card className="bg-card border-2 border-border">
              <CardContent className="p-4 text-center">
                <p className="text-2xl font-bold text-foreground">0</p>
                <p className="text-sm text-muted-foreground">Views</p>
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
        {activeTab === "overview" && <OverviewTab shop={shop} />}
        {activeTab === "products" && <ProductsTab shopId={shop.id} isOwner={isOwner} />}
        {activeTab === "opportunities" && <OpportunitiesTab shopId={shop.id} shopSlug={slug} isOwner={isOwner} />}
        {activeTab === "reviews" && <ReviewsTab shopId={shop.id} />}
        {activeTab === "about" && <AboutTab shop={shop} />}
      </div>
    </div>
  );
}
