"use client";

import {
  BookOpen,
  Package,
  Truck,
  RefreshCw,
  Shield,
  Facebook,
  Twitter,
  Instagram,
  Linkedin,
} from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Button } from "~/components/ui/button";

// Note: Fields below are optional and represent future features not yet in backend
interface AboutTabProps {
  shop: {
    name?: string;
    description?: string | null;
    // Future fields (not yet implemented in backend):
    story?: string | null;
    returnPolicy?: string | null;
    shippingPolicy?: string | null;
    privacyPolicy?: string | null;
    facebookUrl?: string | null;
    twitterUrl?: string | null;
    instagramUrl?: string | null;
    linkedinUrl?: string | null;
  };
}

export default function AboutTab({ shop }: AboutTabProps) {
  const socialLinks = [
    { icon: Facebook, url: shop.facebookUrl, label: "Facebook", color: "hover:text-[#1877F2]" },
    { icon: Twitter, url: shop.twitterUrl, label: "Twitter", color: "hover:text-[#1DA1F2]" },
    { icon: Instagram, url: shop.instagramUrl, label: "Instagram", color: "hover:text-[#E4405F]" },
    { icon: Linkedin, url: shop.linkedinUrl, label: "LinkedIn", color: "hover:text-[#0A66C2]" },
  ].filter((link) => link.url);

  const hasSocialLinks = socialLinks.length > 0;

  return (
    <div className="space-y-6">
      {/* Future Feature Notice */}
      <Card className="bg-card border-2 border-neon-purple/30">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <div className="w-8 h-8 bg-neon-purple/20 rounded-lg flex items-center justify-center border border-neon-purple/30">
              <BookOpen className="size-4 text-neon-purple" />
            </div>
            About This Shop
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            <p className="text-foreground leading-relaxed">
              {shop.description || "No description available."}
            </p>
            <div className="pt-3 border-t border-border">
              <p className="text-sm text-muted-foreground italic">
                Extended shop details (story, policies, social links) will be available in a future update.
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Shop Story (if available) */}
      {shop.story && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-neon-pink/20 rounded-lg flex items-center justify-center border border-neon-pink/30">
                <BookOpen className="size-4 text-neon-pink" />
              </div>
              Our Story
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground whitespace-pre-wrap leading-relaxed">
              {shop.story}
            </p>
          </CardContent>
        </Card>
      )}

      {/* Shipping Policy */}
      {shop.shippingPolicy && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-neon-green/20 rounded-lg flex items-center justify-center border border-neon-green/30">
                <Truck className="size-4 text-neon-green" />
              </div>
              Shipping Policy
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground whitespace-pre-wrap leading-relaxed">
              {shop.shippingPolicy}
            </p>
          </CardContent>
        </Card>
      )}

      {/* Return Policy */}
      {shop.returnPolicy && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-primary/20 rounded-lg flex items-center justify-center border border-primary/30">
                <RefreshCw className="size-4 text-primary" />
              </div>
              Return Policy
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground whitespace-pre-wrap leading-relaxed">
              {shop.returnPolicy}
            </p>
          </CardContent>
        </Card>
      )}

      {/* Privacy Policy */}
      {shop.privacyPolicy && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-neon-purple/20 rounded-lg flex items-center justify-center border border-neon-purple/30">
                <Shield className="size-4 text-neon-purple" />
              </div>
              Privacy Policy
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground whitespace-pre-wrap leading-relaxed">
              {shop.privacyPolicy}
            </p>
          </CardContent>
        </Card>
      )}

      {/* Policies Placeholder if none are set */}
      {!shop.shippingPolicy && !shop.returnPolicy && !shop.privacyPolicy && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-warning/20 rounded-lg flex items-center justify-center border border-warning/30">
                <Package className="size-4 text-warning" />
              </div>
              Shop Policies
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3 text-foreground/80">
              <div>
                <h4 className="font-medium text-foreground mb-1">Standard Shipping</h4>
                <p className="text-sm">
                  We offer standard shipping on all orders. Contact us for specific shipping rates and delivery times.
                </p>
              </div>
              <div>
                <h4 className="font-medium text-foreground mb-1">Returns & Exchanges</h4>
                <p className="text-sm">
                  We accept returns within 30 days of purchase. Items must be in original condition. Contact us to initiate a return.
                </p>
              </div>
              <div>
                <h4 className="font-medium text-foreground mb-1">Privacy</h4>
                <p className="text-sm">
                  Your privacy is important to us. We handle your personal information with care and never share it with third parties without consent.
                </p>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Social Links */}
      {hasSocialLinks && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg">Connect With Us</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex flex-wrap gap-3">
              {socialLinks.map((social) => {
                const Icon = social.icon;
                return (
                  <Button
                    key={social.label}
                    variant="outline"
                    size="default"
                    asChild
                    className={`transition-colors ${social.color}`}
                  >
                    <a
                      href={social.url!}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="flex items-center gap-2"
                    >
                      <Icon className="size-4" />
                      {social.label}
                    </a>
                  </Button>
                );
              })}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
