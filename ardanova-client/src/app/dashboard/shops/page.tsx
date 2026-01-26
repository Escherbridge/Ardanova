"use client";

import Link from "next/link";
import { Store, Plus, Search, Filter } from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";

// Sample shop data - in production this would come from API
const sampleShops = [
  {
    id: "s1",
    name: "Eco Products Store",
    description: "Sustainable and eco-friendly products from our community projects.",
    owner: { id: "u1", name: "Sarah Chen", avatar: "https://i.pravatar.cc/150?u=sarah" },
    category: "Environment",
    productsCount: 24,
    rating: 4.8,
  },
  {
    id: "s2",
    name: "Tech Gadgets Hub",
    description: "Innovative tech products built by our developer community.",
    owner: { id: "u7", name: "David Park", avatar: "https://i.pravatar.cc/150?u=david" },
    category: "Technology",
    productsCount: 15,
    rating: 4.5,
  },
  {
    id: "s3",
    name: "Design Resources",
    description: "Templates, icons, and design assets from the Design Guild.",
    owner: { id: "u6", name: "Emma Watson", avatar: "https://i.pravatar.cc/150?u=emma" },
    category: "Design",
    productsCount: 42,
    rating: 4.9,
  },
];

export default function ShopsPage() {
  return (
    <div className="min-h-screen bg-background p-6">
      <div className="max-w-6xl mx-auto">
        {/* Header */}
        <div className="flex items-center justify-between mb-6">
          <div>
            <h1 className="text-2xl font-bold text-foreground flex items-center gap-2">
              <Store className="size-6 text-primary" />
              Shops
            </h1>
            <p className="text-muted-foreground mt-1">
              Discover products and services from the community
            </p>
          </div>
          <Button variant="neon">
            <Plus className="size-4 mr-2" />
            Create Shop
          </Button>
        </div>

        {/* Search and Filter */}
        <div className="flex gap-4 mb-6">
          <div className="flex-1 relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
            <input
              type="text"
              placeholder="Search shops..."
              className="w-full pl-10 pr-4 py-2 bg-card border-2 border-border text-foreground placeholder:text-muted-foreground focus:border-primary focus:outline-none"
            />
          </div>
          <Button variant="outline">
            <Filter className="size-4 mr-2" />
            Filters
          </Button>
        </div>

        {/* Shops Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {sampleShops.map((shop) => (
            <Card key={shop.id} className="hover:border-primary transition-colors">
              <CardHeader>
                <div className="flex items-start justify-between">
                  <div className="flex items-center gap-3">
                    <div className="size-12 bg-primary/10 flex items-center justify-center border-2 border-primary">
                      <Store className="size-6 text-primary" />
                    </div>
                    <div>
                      <CardTitle className="text-lg">{shop.name}</CardTitle>
                      <Badge variant="secondary" size="sm">{shop.category}</Badge>
                    </div>
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground mb-4">
                  {shop.description}
                </p>
                <div className="flex items-center justify-between">
                  <Link
                    href={`/dashboard/profile/${shop.owner.id}`}
                    className="flex items-center gap-2"
                  >
                    <Avatar className="size-6">
                      <AvatarImage src={shop.owner.avatar} />
                      <AvatarFallback>{shop.owner.name.charAt(0)}</AvatarFallback>
                    </Avatar>
                    <span className="text-sm text-muted-foreground hover:text-primary transition-colors">
                      {shop.owner.name}
                    </span>
                  </Link>
                  <div className="text-sm text-muted-foreground">
                    {shop.productsCount} products
                  </div>
                </div>
                <Button variant="outline" className="w-full mt-4" asChild>
                  <Link href={`/dashboard/shops/${shop.id}`}>Visit Shop</Link>
                </Button>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    </div>
  );
}
