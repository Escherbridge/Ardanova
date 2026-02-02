// Member types
export type MemberRole = "OWNER" | "ADMIN" | "MEMBER";

export interface GuildMember {
  id: string;
  userId: string;
  guildId: string;
  role: MemberRole;
  joinedAt: string;
  user?: {
    id: string;
    name?: string | null;
    email?: string | null;
    image?: string | null;
  };
}

// Review types
export interface GuildReview {
  id: string;
  guildId: string;
  userId: string;
  rating: number;
  comment: string | null;
  createdAt: string;
  user?: {
    id: string;
    name?: string | null;
    email?: string | null;
    image?: string | null;
  };
}

// Update types
export interface GuildUpdate {
  id: string;
  guildId: string;
  title: string;
  content: string;
  createdAt: string;
  createdById: string;
  user?: {
    id: string;
    name?: string | null;
    image?: string | null;
  };
}

// Guild overview type
export interface GuildOverview {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  email?: string | null;
  phone?: string | null;
  website?: string | null;
  address?: string | null;
  logo?: string | null;
  specialties?: string | null;
  portfolio?: string | null;
  isVerified: boolean;
  rating?: number | null;
  reviewsCount: number;
  projectsCount: number;
  ownerId: string;
}

// Utility functions
export const getRoleBadgeVariant = (role: MemberRole) => {
  switch (role) {
    case "OWNER": return "neon-pink-solid" as const;
    case "ADMIN": return "neon-purple" as const;
    case "MEMBER": return "info" as const;
    default: return "default" as const;
  }
};

export const formatRelativeDate = (dateString: string) => {
  const date = new Date(dateString);
  const now = new Date();
  const diffInMs = now.getTime() - date.getTime();
  const diffInHours = diffInMs / (1000 * 60 * 60);

  if (diffInHours < 24) {
    const hours = Math.floor(diffInHours);
    if (hours === 0) {
      const minutes = Math.floor(diffInMs / (1000 * 60));
      return minutes === 0 ? "Just now" : `${minutes}m ago`;
    }
    return `${hours}h ago`;
  } else if (diffInHours < 48) {
    return "Yesterday";
  }
  return date.toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: date.getFullYear() !== now.getFullYear() ? "numeric" : undefined,
  });
};

export const getInitials = (name?: string | null) => {
  if (!name) return "?";
  return name
    .split(" ")
    .map((n) => n[0])
    .join("")
    .toUpperCase()
    .slice(0, 2);
};
