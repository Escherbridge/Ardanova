"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { ArrowLeft, Loader2, Plus, X, Info, Store, ImageIcon } from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "~/components/ui/select";

const categories = [
  { id: "Technology", label: "Technology" },
  { id: "Design", label: "Design" },
  { id: "Sustainability", label: "Sustainability" },
  { id: "Handmade", label: "Handmade" },
  { id: "Digital", label: "Digital Products" },
  { id: "Services", label: "Services" },
  { id: "Food", label: "Food & Beverages" },
  { id: "Fashion", label: "Fashion" },
];

export default function CreateShopPage() {
  const router = useRouter();
  const [formData, setFormData] = useState({
    name: "",
    description: "",
    category: "",
    email: "",
    website: "",
    tags: [] as string[],
  });
  const [newTag, setNewTag] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

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

  const addTag = () => {
    if (newTag.trim() && !formData.tags.includes(newTag.trim())) {
      setFormData((prev) => ({
        ...prev,
        tags: [...prev.tags, newTag.trim()],
      }));
      setNewTag("");
    }
  };

  const removeTag = (tag: string) => {
    setFormData((prev) => ({
      ...prev,
      tags: prev.tags.filter((t) => t !== tag),
    }));
  };

  const validateForm = () => {
    const newErrors: Record<string, string> = {};
    if (!formData.name.trim()) newErrors.name = "Shop name is required";
    if (!formData.description.trim())
      newErrors.description = "Description is required";
    if (formData.description.length < 10)
      newErrors.description = "Must be at least 10 characters";
    if (!formData.category) newErrors.category = "Category is required";
    if (!formData.email.trim()) newErrors.email = "Email is required";
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email))
      newErrors.email = "Invalid email address";
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return;

    setIsSubmitting(true);
    // TODO: Implement shop creation API
    // For now, simulate success and redirect
    setTimeout(() => {
      router.push("/shops");
    }, 1000);
  };

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-2xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <Button variant="ghost" asChild className="mb-4 -ml-2">
            <Link href="/shops">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Shops
            </Link>
          </Button>
          <h1 className="text-3xl font-bold flex items-center gap-3">
            <div className="w-10 h-10 bg-neon-green/20 rounded-lg flex items-center justify-center">
              <Store className="h-5 w-5 text-neon-green" />
            </div>
            Open Your Shop
          </h1>
          <p className="text-muted-foreground mt-2">
            Create a storefront to sell products and services to the community
          </p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Shop Info */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">Shop Information</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="text-sm font-medium mb-2 block">
                  Shop Name <span className="text-neon">*</span>
                </label>
                <input
                  type="text"
                  value={formData.name}
                  onChange={(e) => handleChange("name", e.target.value)}
                  placeholder="e.g., Eco Products Store"
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
                  placeholder="Describe what your shop offers..."
                  rows={4}
                  className={`w-full px-4 py-3 bg-muted/50 border-2 rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 resize-none ${
                    errors.description ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.description && (
                  <p className="text-sm text-destructive mt-1">
                    {errors.description}
                  </p>
                )}
              </div>

              <div>
                <label className="text-sm font-medium mb-2 block">
                  Category <span className="text-neon">*</span>
                </label>
                <Select
                  value={formData.category}
                  onValueChange={(value) => handleChange("category", value)}
                >
                  <SelectTrigger
                    className={
                      errors.category ? "border-destructive" : "border-border"
                    }
                  >
                    <SelectValue placeholder="Select category" />
                  </SelectTrigger>
                  <SelectContent>
                    {categories.map((cat) => (
                      <SelectItem key={cat.id} value={cat.id}>
                        {cat.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {errors.category && (
                  <p className="text-sm text-destructive mt-1">
                    {errors.category}
                  </p>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Contact Info */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">Contact Information</CardTitle>
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
                  placeholder="contact@yourshop.com"
                  className={`w-full px-4 py-3 bg-muted/50 border-2 rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 ${
                    errors.email ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.email && (
                  <p className="text-sm text-destructive mt-1">{errors.email}</p>
                )}
              </div>

              <div>
                <label className="text-sm font-medium mb-2 block">Website</label>
                <input
                  type="url"
                  value={formData.website}
                  onChange={(e) => handleChange("website", e.target.value)}
                  placeholder="https://yourshop.com"
                  className="w-full px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                />
              </div>
            </CardContent>
          </Card>

          {/* Shop Logo */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">Shop Branding</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="text-sm font-medium mb-2 block">Shop Logo</label>
                <div className="border-2 border-dashed border-border rounded-lg p-8 text-center hover:border-neon/50 transition-colors cursor-pointer">
                  <ImageIcon className="h-10 w-10 mx-auto text-muted-foreground mb-3" />
                  <p className="text-sm text-muted-foreground">
                    Click to upload or drag and drop
                  </p>
                  <p className="text-xs text-muted-foreground mt-1">
                    PNG, JPG up to 2MB
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Tags */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">Tags</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex gap-2">
                <input
                  type="text"
                  value={newTag}
                  onChange={(e) => setNewTag(e.target.value)}
                  placeholder="Add a tag..."
                  onKeyDown={(e) => {
                    if (e.key === "Enter") {
                      e.preventDefault();
                      addTag();
                    }
                  }}
                  className="flex-1 px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                />
                <Button
                  type="button"
                  variant="outline"
                  onClick={addTag}
                  className="px-4"
                >
                  <Plus className="h-4 w-4" />
                </Button>
              </div>
              {formData.tags.length > 0 && (
                <div className="flex flex-wrap gap-2">
                  {formData.tags.map((tag) => (
                    <Badge
                      key={tag}
                      variant="secondary"
                      className="gap-1 cursor-pointer hover:bg-destructive/20"
                      onClick={() => removeTag(tag)}
                    >
                      {tag}
                      <X className="h-3 w-3" />
                    </Badge>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>

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
              disabled={isSubmitting}
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Opening Shop...
                </>
              ) : (
                "Open Shop"
              )}
            </Button>
            <Button type="button" variant="outline" asChild className="py-6">
              <Link href="/shops">Cancel</Link>
            </Button>
          </div>

          {/* Info Note */}
          <div className="flex items-start gap-3 p-4 bg-muted/30 rounded-lg border border-border">
            <Info className="h-5 w-5 text-neon mt-0.5" />
            <div className="text-sm text-muted-foreground">
              <p>
                After creating your shop, you can add products, set up payment
                methods, and start selling to the community.
              </p>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}
