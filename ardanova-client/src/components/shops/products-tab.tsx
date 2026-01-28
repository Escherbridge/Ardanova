"use client";

import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Package, Plus, DollarSign, ShoppingBag } from "lucide-react";

interface ProductsTabProps {
  shopId: string;
  isOwner: boolean;
}

// TODO: API Integration Point - Replace with actual Product entity from backend
// Placeholder product data structure for UI demonstration
interface Product {
  id: string;
  name: string;
  price: number;
  description: string;
  image?: string;
  category?: string;
  inStock: boolean;
}

// Placeholder products for demonstration
const PLACEHOLDER_PRODUCTS: Product[] = [
  {
    id: "1",
    name: "Handcrafted Ceramic Mug",
    price: 24.99,
    description: "Beautiful handmade ceramic mug with unique glaze patterns.",
    category: "Home & Kitchen",
    inStock: true,
  },
  {
    id: "2",
    name: "Organic Cotton Tote Bag",
    price: 18.50,
    description: "Eco-friendly tote bag made from 100% organic cotton.",
    category: "Accessories",
    inStock: true,
  },
  {
    id: "3",
    name: "Artisan Soap Set",
    price: 32.00,
    description: "Set of 4 handmade soaps with natural ingredients.",
    category: "Beauty & Personal Care",
    inStock: false,
  },
];

export default function ProductsTab({ shopId, isOwner }: ProductsTabProps) {
  const [showAddForm, setShowAddForm] = useState(false);

  // TODO: API Integration Point - Replace with actual API call
  // Example: const { data: products } = api.shop.getProducts.useQuery({ shopId });
  const products = PLACEHOLDER_PRODUCTS;
  const hasProducts = products.length > 0;

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat("en-US", {
      style: "currency",
      currency: "USD",
    }).format(price);
  };

  return (
    <div className="space-y-6">
      {/* Header with Add Product Button */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <ShoppingBag className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-xl font-semibold">Products</h2>
        </div>
        {isOwner && (
          <Button onClick={() => setShowAddForm(!showAddForm)} size="sm">
            <Plus className="mr-2 h-4 w-4" />
            Add Product
          </Button>
        )}
      </div>

      {/* Add Product Form Placeholder */}
      {isOwner && showAddForm && (
        <Card className="border-2 border-neon-green/30">
          <CardHeader>
            <CardTitle>Add New Product</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-sm text-muted-foreground">
              Product creation form will be implemented with backend integration.
            </div>
            <Button
              variant="outline"
              onClick={() => setShowAddForm(false)}
              className="mt-4"
            >
              Close
            </Button>
          </CardContent>
        </Card>
      )}

      {/* Empty State */}
      {!hasProducts && (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <Package className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-semibold mb-2">No products yet</h3>
            <p className="text-sm text-muted-foreground text-center max-w-sm">
              {isOwner
                ? "Start building your shop by adding your first product."
                : "This shop hasn't listed any products yet. Check back soon!"}
            </p>
            {isOwner && (
              <Button onClick={() => setShowAddForm(true)} className="mt-4">
                <Plus className="mr-2 h-4 w-4" />
                Add Your First Product
              </Button>
            )}
          </CardContent>
        </Card>
      )}

      {/* Products Grid */}
      {hasProducts && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {products.map((product) => (
            <Card
              key={product.id}
              className="bg-card border-2 border-border hover:border-neon-pink/50 transition-all group"
            >
              <CardContent className="p-0">
                {/* Product Image Placeholder */}
                <div className="aspect-square bg-muted flex items-center justify-center border-b-2 border-border group-hover:bg-muted/50 transition-colors">
                  <Package className="size-12 text-muted-foreground/50" />
                </div>

                {/* Product Details */}
                <div className="p-4 space-y-3">
                  <div>
                    <div className="flex items-start justify-between gap-2 mb-1">
                      <h3 className="font-semibold text-foreground line-clamp-1">
                        {product.name}
                      </h3>
                      {!product.inStock && (
                        <Badge variant="outline" className="text-xs shrink-0">
                          Out of Stock
                        </Badge>
                      )}
                    </div>
                    {product.category && (
                      <p className="text-xs text-muted-foreground">
                        {product.category}
                      </p>
                    )}
                  </div>

                  <p className="text-sm text-foreground/80 line-clamp-2">
                    {product.description}
                  </p>

                  <div className="flex items-center justify-between pt-2">
                    <div className="flex items-center gap-1">
                      <DollarSign className="size-4 text-neon-green" />
                      <span className="text-lg font-bold text-foreground">
                        {formatPrice(product.price)}
                      </span>
                    </div>
                    <Button
                      size="sm"
                      variant={product.inStock ? "default" : "outline"}
                      disabled={!product.inStock}
                    >
                      {product.inStock ? "View Details" : "Unavailable"}
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Placeholder Notice */}
      {hasProducts && (
        <Card className="border-dashed border-2 border-border">
          <CardContent className="py-4">
            <p className="text-sm text-muted-foreground text-center">
              Currently showing placeholder products. Real product data will be loaded from the backend.
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
