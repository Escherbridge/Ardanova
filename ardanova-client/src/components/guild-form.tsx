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
import { buildSignInHref } from "~/lib/auth-navigation";

interface GuildFormProps {
  mode: "create" | "edit";
  guild?: Guild;
}

interface GuildFormData {
  name: string;
  description: string;
  email: string;
  website: string;
  phone: string;
  address: string;
  logo: string;
  portfolio: string;
  specialties: string;
}

export function GuildForm({ mode, guild }: GuildFormProps) {
  const router = useRouter();
  const { data: session } = useSession();

  const [formData, setFormData] = useState<GuildFormData>({
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
    onSuccess: (data) => {
      router.push(`/guilds/${data.slug}`);
    },
    onError: (error) => {
      setErrors({ submit: error.message });
    },
  });

  const updateMutation = api.guild.update.useMutation({
    onSuccess: (data) => {
      router.push(`/guilds/${data.slug}`);
    },
    onError: (error) => {
      setErrors({ submit: error.message });
    },
  });

  const isLoading = createMutation.isPending || updateMutation.isPending;

  const handleChange = <Key extends keyof GuildFormData>(
    field: Key,
    value: GuildFormData[Key],
  ) => {
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
          website: formData.website || undefined,
          phone: formData.phone || undefined,
          address: formData.address || undefined,
          logo: formData.logo || undefined,
          portfolio: formData.portfolio || undefined,
          specialties: formData.specialties || undefined,
        },
      });
    }
  };

  if (!session) {
    const callbackUrl =
      mode === "edit" && guild?.slug
        ? `/guilds/${guild.slug}/edit`
        : "/guilds/create";
    return (
      <div className="bg-background flex min-h-screen items-center justify-center">
        <Card className="bg-card border-border w-full max-w-md">
          <CardHeader>
            <CardTitle>Authentication Required</CardTitle>
            <p className="text-muted-foreground text-sm">
              You need to be signed in to{" "}
              {mode === "create" ? "create" : "edit"} a guild.
            </p>
          </CardHeader>
          <CardContent>
            <Button
              asChild
              className="bg-neon hover:bg-neon/90 w-full font-semibold text-black"
            >
              <Link href={buildSignInHref(callbackUrl)}>Sign In</Link>
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="bg-background min-h-screen">
      <div className="mx-auto max-w-2xl px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <Button variant="ghost" asChild className="mb-4 -ml-2">
            <Link href="/guilds">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Guilds
            </Link>
          </Button>
          <h1 className="flex items-center gap-3 text-3xl font-bold">
            <div className="bg-neon/20 flex h-10 w-10 items-center justify-center rounded-lg">
              <Shield className="text-neon h-5 w-5" />
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
                <label
                  htmlFor="guild-name"
                  className="mb-2 block text-sm font-medium"
                >
                  Guild Name <span className="text-neon">*</span>
                </label>
                <input
                  type="text"
                  id="guild-name"
                  required
                  aria-invalid={Boolean(errors.name)}
                  aria-describedby={
                    errors.name ? "guild-name-error" : undefined
                  }
                  value={formData.name}
                  onChange={(e) => handleChange("name", e.target.value)}
                  placeholder="Enter your guild name"
                  className={`bg-muted/50 focus:ring-neon/50 w-full rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none ${
                    errors.name ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.name && (
                  <p
                    id="guild-name-error"
                    role="alert"
                    className="text-destructive mt-1 text-sm"
                  >
                    {errors.name}
                  </p>
                )}
              </div>

              <div>
                <label
                  htmlFor="guild-description"
                  className="mb-2 block text-sm font-medium"
                >
                  Description <span className="text-neon">*</span>
                </label>
                <textarea
                  id="guild-description"
                  required
                  aria-invalid={Boolean(errors.description)}
                  aria-describedby={
                    errors.description ? "guild-description-error" : undefined
                  }
                  value={formData.description}
                  onChange={(e) => handleChange("description", e.target.value)}
                  placeholder="Describe what your guild does, your expertise, and what makes you unique..."
                  rows={4}
                  className={`bg-muted/50 focus:ring-neon/50 w-full resize-none rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none ${
                    errors.description ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.description && (
                  <p
                    id="guild-description-error"
                    role="alert"
                    className="text-destructive mt-1 text-sm"
                  >
                    {errors.description}
                  </p>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Contact Information */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center gap-2 text-lg">
                <Mail className="text-neon-purple h-5 w-5" />
                Contact Information
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label
                  htmlFor="guild-email"
                  className="mb-2 block text-sm font-medium"
                >
                  Contact Email <span className="text-neon">*</span>
                </label>
                <input
                  type="email"
                  id="guild-email"
                  required
                  aria-invalid={Boolean(errors.email)}
                  aria-describedby={
                    errors.email ? "guild-email-error" : undefined
                  }
                  value={formData.email}
                  onChange={(e) => handleChange("email", e.target.value)}
                  placeholder="contact@yourguild.com"
                  className={`bg-muted/50 focus:ring-neon/50 w-full rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none ${
                    errors.email ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.email && (
                  <p
                    id="guild-email-error"
                    role="alert"
                    className="text-destructive mt-1 text-sm"
                  >
                    {errors.email}
                  </p>
                )}
              </div>

              <div>
                <label
                  htmlFor="guild-website"
                  className="mb-2 block text-sm font-medium"
                >
                  Website
                </label>
                <input
                  type="url"
                  id="guild-website"
                  aria-invalid={Boolean(errors.website)}
                  aria-describedby={
                    errors.website ? "guild-website-error" : undefined
                  }
                  value={formData.website}
                  onChange={(e) => handleChange("website", e.target.value)}
                  placeholder="https://yourguild.com"
                  className={`bg-muted/50 focus:ring-neon/50 w-full rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none ${
                    errors.website ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.website && (
                  <p
                    id="guild-website-error"
                    role="alert"
                    className="text-destructive mt-1 text-sm"
                  >
                    {errors.website}
                  </p>
                )}
              </div>

              <div>
                <label
                  htmlFor="guild-phone"
                  className="mb-2 block text-sm font-medium"
                >
                  Phone Number
                </label>
                <input
                  type="tel"
                  id="guild-phone"
                  value={formData.phone}
                  onChange={(e) => handleChange("phone", e.target.value)}
                  placeholder="+1 (555) 000-0000"
                  className="bg-muted/50 border-border focus:ring-neon/50 w-full rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none"
                />
              </div>
            </CardContent>
          </Card>

          {mode === "edit" && (
            <>
              {/* Additional Details */}
              <Card className="bg-card border-border">
                <CardHeader className="pb-3">
                  <CardTitle className="flex items-center gap-2 text-lg">
                    <MapPin className="text-neon-green h-5 w-5" />
                    Location
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div>
                    <label
                      htmlFor="guild-address"
                      className="mb-2 block text-sm font-medium"
                    >
                      Address
                    </label>
                    <textarea
                      id="guild-address"
                      value={formData.address}
                      onChange={(e) => handleChange("address", e.target.value)}
                      placeholder="Enter your business address"
                      rows={2}
                      className="bg-muted/50 border-border focus:ring-neon/50 w-full resize-none rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none"
                    />
                  </div>
                </CardContent>
              </Card>

              {/* Specialties & Portfolio */}
              <Card className="bg-card border-border">
                <CardHeader className="pb-3">
                  <CardTitle className="flex items-center gap-2 text-lg">
                    <Sparkles className="text-neon h-5 w-5" />
                    Specialties & Portfolio
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div>
                    <label
                      htmlFor="guild-specialties"
                      className="mb-2 block text-sm font-medium"
                    >
                      Specialties
                    </label>
                    <input
                      type="text"
                      id="guild-specialties"
                      aria-describedby="guild-specialties-help"
                      value={formData.specialties}
                      onChange={(e) =>
                        handleChange("specialties", e.target.value)
                      }
                      placeholder="Web Development, UI/UX Design, Mobile Apps"
                      className="bg-muted/50 border-border focus:ring-neon/50 w-full rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none"
                    />
                    <p
                      id="guild-specialties-help"
                      className="text-muted-foreground mt-1 text-sm"
                    >
                      Enter your specialties separated by commas
                    </p>
                  </div>

                  <div>
                    <label
                      htmlFor="guild-portfolio"
                      className="mb-2 block text-sm font-medium"
                    >
                      Portfolio
                    </label>
                    <textarea
                      id="guild-portfolio"
                      value={formData.portfolio}
                      onChange={(e) =>
                        handleChange("portfolio", e.target.value)
                      }
                      placeholder="Describe your past work and achievements..."
                      rows={4}
                      className="bg-muted/50 border-border focus:ring-neon/50 w-full resize-none rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none"
                    />
                  </div>

                  <div>
                    <label
                      htmlFor="guild-logo"
                      className="mb-2 block text-sm font-medium"
                    >
                      Logo URL
                    </label>
                    <input
                      type="url"
                      id="guild-logo"
                      value={formData.logo}
                      onChange={(e) => handleChange("logo", e.target.value)}
                      placeholder="https://example.com/logo.png"
                      className="bg-muted/50 border-border focus:ring-neon/50 w-full rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none"
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
                <p role="alert" className="text-destructive text-sm">
                  {errors.submit}
                </p>
              </CardContent>
            </Card>
          )}

          {/* Submit Button */}
          <div className="flex flex-col gap-3 sm:flex-row sm:gap-4">
            <Button
              type="submit"
              className="bg-neon hover:bg-neon/90 flex-1 py-6 font-semibold text-black"
              disabled={isLoading}
            >
              {isLoading ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  {mode === "create"
                    ? "Creating Guild..."
                    : "Saving Changes..."}
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
            <div className="bg-muted/30 border-border flex items-start gap-3 rounded-lg border p-4">
              <Info className="text-neon mt-0.5 h-5 w-5" />
              <div className="text-muted-foreground text-sm">
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
