"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { ArrowLeft, Loader2, Info, Store, ImageIcon } from "lucide-react";
import { toast } from "sonner";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "~/components/ui/select";
import { api } from "~/trpc/react";

const categories = [
  { id: "RETAIL", label: "Retail" },
  { id: "SERVICES", label: "Services" },
  { id: "DIGITAL_PRODUCTS", label: "Digital Products" },
  { id: "FOOD_BEVERAGE", label: "Food & Beverage" },
  { id: "HEALTH_WELLNESS", label: "Health & Wellness" },
  { id: "TECHNOLOGY", label: "Technology" },
  { id: "FASHION", label: "Fashion" },
  { id: "HOME_GARDEN", label: "Home & Garden" },
  { id: "ARTS_CRAFTS", label: "Arts & Crafts" },
  { id: "EDUCATION", label: "Education" },
  { id: "OTHER", label: "Other" },
];

export default function CreateShopPage() {
  const router = useRouter();
  const [formData, setFormData] = useState({
    name: "",
    description: "",
    category: "",
    industry: "",
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const createShopMutation = api.shop.create.useMutation({
    onSuccess: (data) => {
      toast.success("Shop created successfully!");
      router.push(`/shops/${data.slug}`);
    },
    onError: (error) => {
      toast.error(error.message || "Failed to create shop");
    },
  });

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
    if (!formData.name.trim()) newErrors.name = "Shop name is required";
    if (!formData.category) newErrors.category = "Category is required";
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return;

    createShopMutation.mutate({
      name: formData.name,
      description: formData.description || undefined,
      category: formData.category as any,
      industry: formData.industry || undefined,
    });
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
                  Description
                </label>
                <textarea
                  value={formData.description}
                  onChange={(e) => handleChange("description", e.target.value)}
                  placeholder="Describe what your shop offers..."
                  rows={4}
                  className="w-full px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 resize-none"
                />
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

              <div>
                <label className="text-sm font-medium mb-2 block">
                  Industry
                </label>
                <input
                  type="text"
                  value={formData.industry}
                  onChange={(e) => handleChange("industry", e.target.value)}
                  placeholder="e.g., Sustainable Goods, Artisan Crafts"
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
              disabled={createShopMutation.isPending}
            >
              {createShopMutation.isPending ? (
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
