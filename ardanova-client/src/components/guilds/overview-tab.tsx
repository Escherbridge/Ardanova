"use client";

import {
  Building2,
  Mail,
  Phone,
  Globe,
  MapPin,
  Briefcase,
  FileText,
  Target,
} from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";

interface OverviewTabProps {
  guild: {
    description?: string | null;
    email?: string | null;
    phone?: string | null;
    website?: string | null;
    address?: string | null;
    specialties?: string | null;
    portfolio?: string | null;
  };
}

export function OverviewTab({ guild }: OverviewTabProps) {
  return (
    <div className="space-y-6">
      <Card className="bg-card border-2 border-border">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <div className="w-8 h-8 bg-neon-purple/20 rounded-lg flex items-center justify-center border border-neon-purple/30">
              <Building2 className="size-4 text-neon-purple" />
            </div>
            About the Guild
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-foreground whitespace-pre-wrap leading-relaxed">
            {guild.description || "No description provided."}
          </p>
        </CardContent>
      </Card>

      <Card className="bg-card border-2 border-border">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <div className="w-8 h-8 bg-neon-green/20 rounded-lg flex items-center justify-center border border-neon-green/30">
              <Mail className="size-4 text-neon-green" />
            </div>
            Contact Information
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          {guild.email && (
            <div className="flex items-center gap-3 text-foreground">
              <Mail className="size-4 text-muted-foreground" />
              <a
                href={`mailto:${guild.email}`}
                className="hover:text-primary transition-colors"
              >
                {guild.email}
              </a>
            </div>
          )}
          {guild.phone && (
            <div className="flex items-center gap-3 text-foreground">
              <Phone className="size-4 text-muted-foreground" />
              <a
                href={`tel:${guild.phone}`}
                className="hover:text-primary transition-colors"
              >
                {guild.phone}
              </a>
            </div>
          )}
          {guild.website && (
            <div className="flex items-center gap-3 text-foreground">
              <Globe className="size-4 text-muted-foreground" />
              <a
                href={guild.website}
                target="_blank"
                rel="noopener noreferrer"
                className="hover:text-primary transition-colors"
              >
                {guild.website}
              </a>
            </div>
          )}
          {guild.address && (
            <div className="flex items-center gap-3 text-foreground">
              <MapPin className="size-4 text-muted-foreground" />
              <span>{guild.address}</span>
            </div>
          )}
          {!guild.email && !guild.phone && !guild.website && !guild.address && (
            <p className="text-muted-foreground">No contact information provided.</p>
          )}
        </CardContent>
      </Card>

      {guild.specialties && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-neon-pink/20 rounded-lg flex items-center justify-center border border-neon-pink/30">
                <Briefcase className="size-4 text-neon-pink" />
              </div>
              Specialties & Expertise
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex flex-wrap gap-2">
              {guild.specialties.split(",").map((specialty: string, i: number) => (
                <Badge key={i} variant="secondary">
                  {specialty.trim()}
                </Badge>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {guild.portfolio && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-primary/20 rounded-lg flex items-center justify-center border border-primary/30">
                <FileText className="size-4 text-primary" />
              </div>
              Portfolio
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground whitespace-pre-wrap leading-relaxed">
              {guild.portfolio}
            </p>
          </CardContent>
        </Card>
      )}

      <Card className="bg-gradient-to-br from-neon-pink/10 to-neon-purple/10 border-2 border-neon-pink/30">
        <CardContent className="pt-6">
          <div className="flex items-start gap-4">
            <div className="w-10 h-10 bg-neon-pink/20 rounded-lg flex items-center justify-center border border-neon-pink/30 flex-shrink-0">
              <Target className="size-5 text-neon-pink" />
            </div>
            <div className="flex-1">
              <h3 className="text-lg font-semibold mb-2">Work With Us</h3>
              <p className="text-muted-foreground mb-4">
                Looking for experienced professionals to bring your project to life?
                Get in touch to discuss how we can help achieve your goals.
              </p>
              <Button variant="default">
                Get in Touch
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
