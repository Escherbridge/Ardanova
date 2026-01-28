// Tab components
export { default as OverviewTab } from "./overview-tab";
export { default as ProductsTab } from "./products-tab";
export { default as ReviewsTab } from "./reviews-tab";
export { default as AboutTab } from "./about-tab";

// Shop type matching backend DTO
export interface Shop {
  id: string;
  name: string;
  slug: string;
  description?: string;
  category: string;
  industry?: string;
  address?: string;
  phone?: string;
  email?: string;
  website?: string;
  logo?: string;
  plan: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  ownerId: string;
}
