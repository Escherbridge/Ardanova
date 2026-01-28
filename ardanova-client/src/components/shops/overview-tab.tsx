"use client";

import {
  Store,
  Mail,
  Phone,
  Globe,
  MapPin,
  Building2,
} from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";

interface OverviewTabProps {
  shop: {
    description?: string | null;
    email?: string | null;
    phone?: string | null;
    website?: string | null;
    address?: string | null;
    industry?: string | null;
    category?: string | null;
  };
}

export default function OverviewTab({ shop }: OverviewTabProps) {
  return (
    <div className="space-y-6">
      <Card className="bg-card border-2 border-border">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <div className="w-8 h-8 bg-neon-pink/20 rounded-lg flex items-center justify-center border border-neon-pink/30">
              <Store className="size-4 text-neon-pink" />
            </div>
            About This Shop
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-foreground whitespace-pre-wrap leading-relaxed">
            {shop.description || "No description provided."}
          </p>
        </CardContent>
      </Card>

      {shop.email && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-neon-green/20 rounded-lg flex items-center justify-center border border-neon-green/30">
                <Mail className="size-4 text-neon-green" />
              </div>
              Email
            </CardTitle>
          </CardHeader>
          <CardContent>
            <a
              href={`mailto:${shop.email}`}
              className="text-foreground hover:text-neon-green transition-colors"
            >
              {shop.email}
            </a>
          </CardContent>
        </Card>
      )}

      {shop.phone && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-primary/20 rounded-lg flex items-center justify-center border border-primary/30">
                <Phone className="size-4 text-primary" />
              </div>
              Phone
            </CardTitle>
          </CardHeader>
          <CardContent>
            <a
              href={`tel:${shop.phone}`}
              className="text-foreground hover:text-primary transition-colors"
            >
              {shop.phone}
            </a>
          </CardContent>
        </Card>
      )}

      {shop.website && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-neon-purple/20 rounded-lg flex items-center justify-center border border-neon-purple/30">
                <Globe className="size-4 text-neon-purple" />
              </div>
              Website
            </CardTitle>
          </CardHeader>
          <CardContent>
            <a
              href={shop.website}
              target="_blank"
              rel="noopener noreferrer"
              className="text-foreground hover:text-neon-purple transition-colors"
            >
              {shop.website}
            </a>
          </CardContent>
        </Card>
      )}

      {shop.address && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-warning/20 rounded-lg flex items-center justify-center border border-warning/30">
                <MapPin className="size-4 text-warning" />
              </div>
              Address
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground whitespace-pre-wrap leading-relaxed">
              {shop.address}
            </p>
          </CardContent>
        </Card>
      )}

      {shop.industry && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-info/20 rounded-lg flex items-center justify-center border border-info/30">
                <Building2 className="size-4 text-info" />
              </div>
              Industry
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground whitespace-pre-wrap leading-relaxed">
              {shop.industry}
            </p>
          </CardContent>
        </Card>
      )}

      {shop.category && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg">Category</CardTitle>
          </CardHeader>
          <CardContent>
            <Badge variant="secondary" className="text-base px-3 py-1">
              {shop.category}
            </Badge>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
