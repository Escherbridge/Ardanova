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
      <Card className="bg-card border-border border-2">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-lg">
            <div className="bg-system/20 border-system/30 flex h-8 w-8 items-center justify-center rounded-none border">
              <Building2 className="text-system size-4" />
            </div>
            About the Guild
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-foreground leading-relaxed whitespace-pre-wrap">
            {guild.description || "No description provided."}
          </p>
        </CardContent>
      </Card>

      <Card className="bg-card border-border border-2">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-lg">
            <div className="bg-success/20 border-success/30 flex h-8 w-8 items-center justify-center rounded-none border">
              <Mail className="text-success size-4" />
            </div>
            Contact Information
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          {guild.email && (
            <div className="text-foreground flex items-center gap-3">
              <Mail className="text-muted-foreground size-4" />
              <a
                href={`mailto:${guild.email}`}
                className="hover:text-primary inline-flex min-h-11 items-center transition-colors"
              >
                {guild.email}
              </a>
            </div>
          )}
          {guild.phone && (
            <div className="text-foreground flex items-center gap-3">
              <Phone className="text-muted-foreground size-4" />
              <a
                href={`tel:${guild.phone}`}
                className="hover:text-primary inline-flex min-h-11 items-center transition-colors"
              >
                {guild.phone}
              </a>
            </div>
          )}
          {guild.website && (
            <div className="text-foreground flex items-center gap-3">
              <Globe className="text-muted-foreground size-4" />
              <a
                href={guild.website}
                target="_blank"
                rel="noopener noreferrer"
                className="hover:text-primary inline-flex min-h-11 items-center transition-colors"
              >
                {guild.website}
              </a>
            </div>
          )}
          {guild.address && (
            <div className="text-foreground flex items-center gap-3">
              <MapPin className="text-muted-foreground size-4" />
              <span>{guild.address}</span>
            </div>
          )}
          {!guild.email && !guild.phone && !guild.website && !guild.address && (
            <p className="text-muted-foreground">
              No contact information provided.
            </p>
          )}
        </CardContent>
      </Card>

      {guild.specialties && (
        <Card className="bg-card border-border border-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <div className="bg-primary/20 border-primary/30 flex h-8 w-8 items-center justify-center rounded-none border">
                <Briefcase className="text-primary size-4" />
              </div>
              Specialties & Expertise
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex flex-wrap gap-2">
              {guild.specialties
                .split(",")
                .map((specialty: string, i: number) => (
                  <Badge key={i} variant="secondary">
                    {specialty.trim()}
                  </Badge>
                ))}
            </div>
          </CardContent>
        </Card>
      )}

      {guild.portfolio && (
        <Card className="bg-card border-border border-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <div className="bg-primary/20 border-primary/30 flex h-8 w-8 items-center justify-center rounded-none border">
                <FileText className="text-primary size-4" />
              </div>
              Portfolio
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground leading-relaxed whitespace-pre-wrap">
              {guild.portfolio}
            </p>
          </CardContent>
        </Card>
      )}

      {(guild.email || guild.phone || guild.website) && (
        <Card className="border-primary bg-card border-2">
          <CardContent className="pt-6">
            <div className="flex items-start gap-4">
              <div className="bg-primary/20 border-primary/30 flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-none border">
                <Target className="text-primary size-5" />
              </div>
              <div className="flex-1">
                <h3 className="mb-2 text-lg font-semibold">Work With Us</h3>
                <p className="text-muted-foreground mb-4">
                  Looking for experienced professionals to bring your project to
                  life? Use the guild&apos;s published contact channel to
                  discuss the work.
                </p>
                {guild.email ? (
                  <Button asChild variant="default">
                    <a href={`mailto:${guild.email}`}>Email the guild</a>
                  </Button>
                ) : guild.website ? (
                  <Button asChild variant="default">
                    <a
                      href={guild.website}
                      rel="noopener noreferrer"
                      target="_blank"
                    >
                      Visit the guild website
                    </a>
                  </Button>
                ) : (
                  <Button asChild variant="default">
                    <a href={`tel:${guild.phone}`}>Call the guild</a>
                  </Button>
                )}
              </div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
