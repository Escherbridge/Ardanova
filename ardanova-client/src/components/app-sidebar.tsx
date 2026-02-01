"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard,
  FolderKanban,
  Users,
  Briefcase,
  Settings,
  HelpCircle,
  LogOut,
  Plus,
  ChevronLeft,
  ChevronRight,
  Vote,
  Bell,
  MessageCircle,
  CheckSquare,
  Store,
  Calendar,
} from "lucide-react";
import { useState } from "react";

import { Button } from "~/components/ui/button";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Separator } from "~/components/ui/separator";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "~/components/ui/tooltip";
import { cn } from "~/lib/utils";

interface AppSidebarProps {
  user?: {
    name?: string | null;
    email?: string | null;
    image?: string | null;
  } | null;
}

const mainNavItems = [
  {
    href: "/dashboard",
    label: "Dashboard",
    icon: LayoutDashboard,
  },
  {
    href: "/projects",
    label: "Projects",
    icon: FolderKanban,
  },
  {
    href: "/guilds",
    label: "Guilds",
    icon: Users,
  },
  {
    href: "/shops",
    label: "Shops",
    icon: Store,
  },
  {
    href: "/opportunities",
    label: "Opportunities",
    icon: Briefcase,
  },
  {
    href: "/governance",
    label: "Governance",
    icon: Vote,
  },
  {
    href: "/events",
    label: "Events",
    icon: Calendar,
  },
  {
    href: "/tasks",
    label: "Tasks",
    icon: CheckSquare,
  },
  {
    href: "/chats",
    label: "Chats",
    icon: MessageCircle,
  },
];

const secondaryNavItems = [
  {
    href: "/settings",
    label: "Settings",
    icon: Settings,
  },
  {
    href: "/help",
    label: "Help & Support",
    icon: HelpCircle,
  },
];

export function AppSidebar({ user }: AppSidebarProps) {
  const pathname = usePathname();
  const [isCollapsed, setIsCollapsed] = useState(false);

  const isActive = (href: string) => {
    if (href === "/dashboard") {
      return pathname === "/dashboard";
    }
    return pathname.startsWith(href);
  };

  return (
    <TooltipProvider delayDuration={0}>
      <aside
        className={cn(
          "sticky top-0 self-start z-40 flex h-screen shrink-0 flex-col border-r-2 border-sidebar-border bg-sidebar transition-all duration-300",
          isCollapsed ? "w-16" : "w-64"
        )}
      >
        {/* Logo */}
        <div className="flex h-16 items-center justify-between border-b-2 border-sidebar-border px-4">
          {!isCollapsed && (
            <Link href="/" className="flex items-center gap-2">
              <span className="text-xl font-bold tracking-tight">
                <span className="text-sidebar-primary">ARDA</span>
                <span className="text-sidebar-foreground">NOVA</span>
              </span>
            </Link>
          )}
          <Button
            variant="ghost"
            size="icon-sm"
            onClick={() => setIsCollapsed(!isCollapsed)}
            className="text-sidebar-muted hover:text-sidebar-foreground hover:bg-sidebar-accent"
          >
            {isCollapsed ? (
              <ChevronRight className="size-4" />
            ) : (
              <ChevronLeft className="size-4" />
            )}
          </Button>
        </div>

        {/* Create Button */}
        <div className="p-3">
          {isCollapsed ? (
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="neon"
                  size="icon"
                  className="w-full"
                  asChild
                >
                  <Link href="/dashboard/create">
                    <Plus className="size-5" />
                  </Link>
                </Button>
              </TooltipTrigger>
              <TooltipContent side="right">Create Project</TooltipContent>
            </Tooltip>
          ) : (
            <Button variant="neon" className="w-full" asChild>
              <Link href="/dashboard/create">
                <Plus className="size-4 mr-2" />
                Create Project
              </Link>
            </Button>
          )}
        </div>

        {/* Main Navigation */}
        <nav className="flex-1 space-y-1 px-3 py-2">
          {mainNavItems.map((item) => {
            const Icon = item.icon;
            const active = isActive(item.href);

            if (isCollapsed) {
              return (
                <Tooltip key={item.href}>
                  <TooltipTrigger asChild>
                    <Link
                      href={item.href}
                      className={cn(
                        "flex h-10 w-full items-center justify-center transition-colors",
                        active
                          ? "bg-sidebar-accent text-sidebar-primary border-l-2 border-sidebar-primary"
                          : "text-sidebar-muted hover:bg-sidebar-accent hover:text-sidebar-foreground"
                      )}
                    >
                      <Icon className="size-5" />
                    </Link>
                  </TooltipTrigger>
                  <TooltipContent side="right">{item.label}</TooltipContent>
                </Tooltip>
              );
            }

            return (
              <Link
                key={item.href}
                href={item.href}
                className={cn(
                  "flex h-10 items-center gap-3 px-3 text-sm font-medium transition-colors",
                  active
                    ? "bg-sidebar-accent text-sidebar-primary border-l-2 border-sidebar-primary -ml-px"
                    : "text-sidebar-muted hover:bg-sidebar-accent hover:text-sidebar-foreground"
                )}
              >
                <Icon className="size-5" />
                {item.label}
              </Link>
            );
          })}
        </nav>

        <Separator className="mx-3 bg-sidebar-border" />

        {/* Secondary Navigation */}
        <nav className="space-y-1 px-3 py-2">
          {secondaryNavItems.map((item) => {
            const Icon = item.icon;
            const active = isActive(item.href);

            if (isCollapsed) {
              return (
                <Tooltip key={item.href}>
                  <TooltipTrigger asChild>
                    <Link
                      href={item.href}
                      className={cn(
                        "flex h-10 w-full items-center justify-center transition-colors",
                        active
                          ? "bg-sidebar-accent text-sidebar-primary"
                          : "text-sidebar-muted hover:bg-sidebar-accent hover:text-sidebar-foreground"
                      )}
                    >
                      <Icon className="size-5" />
                    </Link>
                  </TooltipTrigger>
                  <TooltipContent side="right">{item.label}</TooltipContent>
                </Tooltip>
              );
            }

            return (
              <Link
                key={item.href}
                href={item.href}
                className={cn(
                  "flex h-10 items-center gap-3 px-3 text-sm font-medium transition-colors",
                  active
                    ? "bg-sidebar-accent text-sidebar-primary"
                    : "text-sidebar-muted hover:bg-sidebar-accent hover:text-sidebar-foreground"
                )}
              >
                <Icon className="size-5" />
                {item.label}
              </Link>
            );
          })}
        </nav>

        {/* User Section */}
        <div className="mt-auto p-3">
          {user && (
            <div
              className={cn(
                "flex items-center gap-3",
                isCollapsed && "justify-center"
              )}
            >
              <Link href="/dashboard/profile" className="shrink-0">
                <Avatar className="size-9 border-2 border-sidebar-border hover:border-sidebar-primary transition-colors">
                  <AvatarImage src={user.image ?? ""} alt={user.name ?? ""} />
                  <AvatarFallback className="bg-sidebar-accent text-sidebar-foreground">
                    {user.name?.charAt(0) ?? "U"}
                  </AvatarFallback>
                </Avatar>
              </Link>
              {!isCollapsed && (
                <Link href="/dashboard/profile" className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-sidebar-foreground truncate hover:text-sidebar-primary transition-colors">
                    {user.name}
                  </p>
                  <p className="text-xs text-sidebar-muted truncate">
                    {user.email}
                  </p>
                </Link>
              )}
              {!isCollapsed && (
                <div className="flex gap-1">
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <Button
                        variant="ghost"
                        size="icon-sm"
                        className="text-sidebar-muted hover:text-sidebar-foreground hover:bg-sidebar-accent"
                      >
                        <Bell className="size-4" />
                      </Button>
                    </TooltipTrigger>
                    <TooltipContent>Notifications</TooltipContent>
                  </Tooltip>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <Button
                        variant="ghost"
                        size="icon-sm"
                        className="text-sidebar-muted hover:text-sidebar-foreground hover:bg-sidebar-accent"
                        asChild
                      >
                        <Link href="/api/auth/signout">
                          <LogOut className="size-4" />
                        </Link>
                      </Button>
                    </TooltipTrigger>
                    <TooltipContent>Sign Out</TooltipContent>
                  </Tooltip>
                </div>
              )}
            </div>
          )}
          {isCollapsed && user && (
            <div className="flex flex-col gap-1 mt-2">
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button
                    variant="ghost"
                    size="icon-sm"
                    className="w-full text-sidebar-muted hover:text-sidebar-foreground hover:bg-sidebar-accent"
                  >
                    <Bell className="size-4" />
                  </Button>
                </TooltipTrigger>
                <TooltipContent side="right">Notifications</TooltipContent>
              </Tooltip>
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button
                    variant="ghost"
                    size="icon-sm"
                    className="w-full text-sidebar-muted hover:text-sidebar-foreground hover:bg-sidebar-accent"
                    asChild
                  >
                    <Link href="/api/auth/signout">
                      <LogOut className="size-4" />
                    </Link>
                  </Button>
                </TooltipTrigger>
                <TooltipContent side="right">Sign Out</TooltipContent>
              </Tooltip>
            </div>
          )}
        </div>
      </aside>
    </TooltipProvider>
  );
}

export function SidebarInset({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex-1 min-w-0 transition-all duration-300">
      {children}
    </div>
  );
}
