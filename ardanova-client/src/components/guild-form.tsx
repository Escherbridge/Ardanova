"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useSession } from "next-auth/react";
import { ArrowLeft, Loader2 } from "lucide-react";
import Link from "next/link";

import { Button } from "~/components/ui/button";
import { Input } from "~/components/ui/input";
import { Textarea } from "~/components/ui/textarea";
import { Label } from "~/components/ui/label";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "~/components/ui/card";
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

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    // Clear error when user starts typing
    if (errors[name]) {
      setErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[name];
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
      <div className="min-h-screen flex items-center justify-center bg-slate-50">
        <Card className="w-full max-w-md">
          <CardHeader>
            <CardTitle>Authentication Required</CardTitle>
            <CardDescription>
              You need to be signed in to {mode === "create" ? "create" : "edit"} a guild.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Button asChild className="w-full">
              <Link href="/api/auth/signin">Sign In</Link>
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-50">
      <div className="container mx-auto px-4 py-8 max-w-2xl">
        {/* Back Button */}
        <div className="mb-6">
          <Button variant="ghost" asChild>
            <Link href="/guilds">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Guilds
            </Link>
          </Button>
        </div>

        <Card>
          <CardHeader>
            <CardTitle>
              {mode === "create" ? "Create a New Guild" : "Edit Guild"}
            </CardTitle>
            <CardDescription>
              {mode === "create"
                ? "Set up your professional guild to start bidding on projects"
                : "Update your guild information"}
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit} className="space-y-6">
              {/* Guild Name */}
              <div className="space-y-2">
                <Label htmlFor="name">Guild Name *</Label>
                <Input
                  id="name"
                  name="name"
                  value={formData.name}
                  onChange={handleChange}
                  placeholder="Enter your guild name"
                  className={errors.name ? "border-red-500" : ""}
                />
                {errors.name && (
                  <p className="text-sm text-red-500">{errors.name}</p>
                )}
              </div>

              {/* Description */}
              <div className="space-y-2">
                <Label htmlFor="description">Description *</Label>
                <Textarea
                  id="description"
                  name="description"
                  value={formData.description}
                  onChange={handleChange}
                  placeholder="Describe what your guild does..."
                  rows={4}
                  className={errors.description ? "border-red-500" : ""}
                />
                {errors.description && (
                  <p className="text-sm text-red-500">{errors.description}</p>
                )}
              </div>

              {/* Email */}
              <div className="space-y-2">
                <Label htmlFor="email">Contact Email *</Label>
                <Input
                  id="email"
                  name="email"
                  type="email"
                  value={formData.email}
                  onChange={handleChange}
                  placeholder="contact@yourguild.com"
                  className={errors.email ? "border-red-500" : ""}
                />
                {errors.email && (
                  <p className="text-sm text-red-500">{errors.email}</p>
                )}
              </div>

              {/* Website */}
              <div className="space-y-2">
                <Label htmlFor="website">Website</Label>
                <Input
                  id="website"
                  name="website"
                  type="url"
                  value={formData.website}
                  onChange={handleChange}
                  placeholder="https://yourguild.com"
                  className={errors.website ? "border-red-500" : ""}
                />
                {errors.website && (
                  <p className="text-sm text-red-500">{errors.website}</p>
                )}
              </div>

              {/* Phone */}
              <div className="space-y-2">
                <Label htmlFor="phone">Phone Number</Label>
                <Input
                  id="phone"
                  name="phone"
                  type="tel"
                  value={formData.phone}
                  onChange={handleChange}
                  placeholder="+1 (555) 000-0000"
                />
              </div>

              {mode === "edit" && (
                <>
                  {/* Address */}
                  <div className="space-y-2">
                    <Label htmlFor="address">Address</Label>
                    <Textarea
                      id="address"
                      name="address"
                      value={formData.address}
                      onChange={handleChange}
                      placeholder="Enter your business address"
                      rows={2}
                    />
                  </div>

                  {/* Specialties */}
                  <div className="space-y-2">
                    <Label htmlFor="specialties">Specialties</Label>
                    <Input
                      id="specialties"
                      name="specialties"
                      value={formData.specialties}
                      onChange={handleChange}
                      placeholder="Web Development, UI/UX Design, Mobile Apps (comma-separated)"
                    />
                    <p className="text-sm text-slate-500">
                      Enter your specialties separated by commas
                    </p>
                  </div>

                  {/* Portfolio */}
                  <div className="space-y-2">
                    <Label htmlFor="portfolio">Portfolio</Label>
                    <Textarea
                      id="portfolio"
                      name="portfolio"
                      value={formData.portfolio}
                      onChange={handleChange}
                      placeholder="Describe your past work and achievements..."
                      rows={4}
                    />
                  </div>

                  {/* Logo URL */}
                  <div className="space-y-2">
                    <Label htmlFor="logo">Logo URL</Label>
                    <Input
                      id="logo"
                      name="logo"
                      type="url"
                      value={formData.logo}
                      onChange={handleChange}
                      placeholder="https://example.com/logo.png"
                    />
                  </div>
                </>
              )}

              {/* Submit Error */}
              {errors.submit && (
                <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
                  <p className="text-sm text-red-600">{errors.submit}</p>
                </div>
              )}

              {/* Submit Button */}
              <div className="flex gap-4">
                <Button
                  type="submit"
                  className="flex-1"
                  disabled={isLoading}
                >
                  {isLoading ? (
                    <>
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                      {mode === "create" ? "Creating..." : "Saving..."}
                    </>
                  ) : mode === "create" ? (
                    "Create Guild"
                  ) : (
                    "Save Changes"
                  )}
                </Button>
                <Button type="button" variant="outline" asChild>
                  <Link href="/guilds">Cancel</Link>
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
