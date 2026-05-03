"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useSession } from "next-auth/react";
import Link from "next/link";
import {
  ArrowLeft,
  Loader2,
  Shield,
  Info,
  Mail,
  MapPin,
  Sparkles,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { api } from "~/trpc/react";
import type { Guild } from "~/lib/api";

interface GuildFormProps {
  mode: "create" | "edit";
  guild?: Guild;
}

export function GuildForm({ mode, guild }: GuildFormProps) {
  const router = useRouter();
  const { data: session } = useSession();

  const [formData, setFormData] = useState({
    name: guild?.name ?? "",
    description: guild?.description ?? "",
    email: guild?.email ?? "",
    website: guild?.website ?? "",
    phone: guild?.phone ?? "",
    address: guild?.address ?? "",
    logo: guild?.logo ?? "",
    portfolio: guild?.portfolio ?? "",
    specialties: guild?.specialties ?? "",
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  const createMutation = api.guild.create.useMutation({
    onSuccess: (data: any) => {
      router.push(`/guilds/${data.slug as string}`);
    },
    onError: (error) => {
      setErrors({ submit: error.message });
    },
  });

  const updateMutation = api.guild.update.useMutation({
    onSuccess: (data: any) => {
      router.push(`/guilds/${data.slug as string}`);
    },
    onError: (error) => {
      setErrors({ submit: error.message });
    },
  });

  const isLoading = createMutation.isPending || updateMutation.isPending;

  const handleChange = (field: string, value: string) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[field];
        return newErrors;
      });
    }
  };

  const validateForm = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.name.trim()) {
      newErrors.name = "Guild name is required";
    }

    if (!formData.description.trim()) {
      newErrors.description = "Description is required";
    } else if (formData.description.length < 10) {
      newErrors.description = "Description must be at least 10 characters";
    }

    if (!formData.email.trim()) {
      newErrors.email = "Email is required";
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      newErrors.email = "Invalid email address";
    }

    if (formData.website && !/^https?:\/\/.+/.test(formData.website)) {
      newErrors.website = "Website must start with http:// or https://";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    if (mode === "create") {
      createMutation.mutate({
        name: formData.name,
        description: formData.description,
        email: formData.email,
        website: formData.website || undefined,
        phone: formData.phone || undefined,
      });
    } else if (guild) {
      updateMutation.mutate({
        id: guild.id,
        data: {
          name: formData.name,
          description: formData.description,
          email: formData.email,
          website: formData.website || null,
          phone: formData.phone || null,
          address: formData.address || null,
          logo: formData.logo || null,
          portfolio: formData.portfolio || null,
          specialties: formData.specialties || null,
        },
      });
    }
  };

  if (!session) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <Card className="w-full max-w-md bg-card border-border">
          <CardHeader>
            <CardTitle>Authentication Required</CardTitle>
            <p className="text-sm text-muted-foreground">
              You need to be signed in to {mode === "create" ? "create" : "edit"} a guild.
            </p>
          </CardHeader>
          <CardContent>
            <Button asChild className="w-full bg-neon hover:bg-neon/90 text-black font-semibold">
              <Link href="/api/auth/signin">Sign In</Link>
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-2xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <Button variant="ghost" asChild className="mb-4 -ml-2">
            <Link href="/guilds">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Guilds
            </Link>
          </Button>
          <h1 className="text-3xl font-bold flex items-center gap-3">
            <div className="w-10 h-10 bg-neon/20 rounded-lg flex items-center justify-center">
              <Shield className="h-5 w-5 text-neon" />
            </div>
            {mode === "create" ? "Create Guild" : "Edit Guild"}
          </h1>
          <p className="text-muted-foreground mt-2">
            {mode === "create"
              ? "Set up your professional guild to start bidding on projects"
              : "Update your guild information"}
          </p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Guild Details */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">Guild Details</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="text-sm font-medium mb-2 block">
                  Guild Name <span className="text-neon">*</span>
                </label>
                <input
                  type="text"
                  value={formData.name}
                  onChange={(e) => handleChange("name", e.target.value)}
                  placeholder="Enter your guild name"
                  className={`w-full px-4 py-3 bg-muted/50 border-2 rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 ${
                    errors.name ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.name && (
                  <p className="text-sm text-destructive mt-1">{errors.name}</p>
                )}
              </div>

              <div>
                <label className="text-sm font-medium mb-2 block">
                  Description <span className="text-neon">*</span>
                </label>
                <textarea
                  value={formData.description}
                  onChange={(e) => handleChange("description", e.target.value)}
                  placeholder="Describe what your guild does, your expertise, and what makes you unique..."
                  rows={4}
                  className={`w-full px-4 py-3 bg-muted/50 border-2 rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 resize-none ${
                    errors.description ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.description && (
                  <p className="text-sm text-destructive mt-1">{errors.description}</p>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Contact Information */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg flex items-center gap-2">
                <Mail className="h-5 w-5 text-neon-purple" />
                Contact Information
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="text-sm font-medium mb-2 block">
                  Contact Email <span className="text-neon">*</span>
                </label>
                <input
                  type="email"
                  value={formData.email}
                  onChange={(e) => handleChange("email", e.target.value)}
                  placeholder="contact@yourguild.com"
                  className={`w-full px-4 py-3 bg-muted/50 border-2 rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 ${
                    errors.email ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.email && (
                  <p className="text-sm text-destructive mt-1">{errors.email}</p>
                )}
              </div>

              <div>
                <label className="text-sm font-medium mb-2 block">
                  Website
                </label>
                <input
                  type="url"
                  value={formData.website}
                  onChange={(e) => handleChange("website", e.target.value)}
                  placeholder="https://yourguild.com"
                  className={`w-full px-4 py-3 bg-muted/50 border-2 rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 ${
                    errors.website ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.website && (
                  <p className="text-sm text-destructive mt-1">{errors.website}</p>
                )}
              </div>

              <div>
                <label className="text-sm font-medium mb-2 block">
                  Phone Number
                </label>
                <input
                  type="tel"
                  value={formData.phone}
                  onChange={(e) => handleChange("phone", e.target.value)}
                  placeholder="+1 (555) 000-0000"
                  className="w-full px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                />
              </div>
            </CardContent>
          </Card>

          {mode === "edit" && (
            <>
              {/* Additional Details */}
              <Card className="bg-card border-border">
                <CardHeader className="pb-3">
                  <CardTitle className="text-lg flex items-center gap-2">
                    <MapPin className="h-5 w-5 text-neon-green" />
                    Location
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div>
                    <label className="text-sm font-medium mb-2 block">
                      Address
                    </label>
                    <textarea
                      value={formData.address}
                      onChange={(e) => handleChange("address", e.target.value)}
                      placeholder="Enter your business address"
                      rows={2}
                      className="w-full px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 resize-none"
                    />
                  </div>
                </CardContent>
              </Card>

              {/* Specialties & Portfolio */}
              <Card className="bg-card border-border">
                <CardHeader className="pb-3">
                  <CardTitle className="text-lg flex items-center gap-2">
                    <Sparkles className="h-5 w-5 text-neon" />
                    Specialties & Portfolio
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div>
                    <label className="text-sm font-medium mb-2 block">
                      Specialties
                    </label>
                    <input
                      type="text"
                      value={formData.specialties}
                      onChange={(e) => handleChange("specialties", e.target.value)}
                      placeholder="Web Development, UI/UX Design, Mobile Apps"
                      className="w-full px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                    />
                    <p className="text-sm text-muted-foreground mt-1">
                      Enter your specialties separated by commas
                    </p>
                  </div>

                  <div>
                    <label className="text-sm font-medium mb-2 block">
                      Portfolio
                    </label>
                    <textarea
                      value={formData.portfolio}
                      onChange={(e) => handleChange("portfolio", e.target.value)}
                      placeholder="Describe your past work and achievements..."
                      rows={4}
                      className="w-full px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 resize-none"
                    />
                  </div>

                  <div>
                    <label className="text-sm font-medium mb-2 block">
                      Logo URL
                    </label>
                    <input
                      type="url"
                      value={formData.logo}
                      onChange={(e) => handleChange("logo", e.target.value)}
                      placeholder="https://example.com/logo.png"
                      className="w-full px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                    />
                  </div>
                </CardContent>
              </Card>
            </>
          )}

          {/* Submit Error */}
          {errors.submit && (
            <Card className="bg-destructive/10 border-destructive">
              <CardContent className="py-4">
                <p className="text-sm text-destructive">{errors.submit}</p>
              </CardContent>
            </Card>
          )}

          {/* Submit Button */}
          <div className="flex gap-4">
            <Button
              type="submit"
              className="flex-1 bg-neon hover:bg-neon/90 text-black font-semibold py-6"
              disabled={isLoading}
            >
              {isLoading ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  {mode === "create" ? "Creating Guild..." : "Saving Changes..."}
                </>
              ) : mode === "create" ? (
                "Create Guild"
              ) : (
                "Save Changes"
              )}
            </Button>
            <Button type="button" variant="outline" asChild className="py-6">
              <Link href="/guilds">Cancel</Link>
            </Button>
          </div>

          {/* Info Note */}
          {mode === "create" && (
            <div className="flex items-start gap-3 p-4 bg-muted/30 rounded-lg border border-border">
              <Info className="h-5 w-5 text-neon mt-0.5" />
              <div className="text-sm text-muted-foreground">
                <p>
                  After creating your guild, you can invite members, set up
                  specialties, and start bidding on project opportunities.
                </p>
              </div>
            </div>
          )}
        </form>
      </div>
    </div>
  );
}
