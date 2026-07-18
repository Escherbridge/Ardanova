"use client";

import {
  BriefcaseBusiness,
  CalendarDays,
  CheckSquare2,
  FolderKanban,
  Home,
  Landmark,
  LogOut,
  MessageCircle,
  Settings,
  Shield,
  ShieldCheck,
  Sparkles,
  UsersRound,
  Vote,
  WalletCards,
} from "lucide-react";
import Link from "next/link";
import { signOut } from "next-auth/react";
import { usePathname } from "next/navigation";

import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { cn } from "~/lib/utils";

export interface WorkspaceUser {
  name?: string | null;
  email?: string | null;
  image?: string | null;
  role?: string | null;
}

interface AppSidebarProps {
  user?: WorkspaceUser | null;
  authPreview?: boolean;
}

interface NavigationItem {
  href: string;
  label: string;
  icon: NavigationIconName;
}

type NavigationIconName =
  | "home"
  | "projects"
  | "tasks"
  | "opportunities"
  | "guilds"
  | "people"
  | "governance"
  | "events"
  | "portfolio"
  | "chats"
  | "wallets"
  | "settings"
  | "kyc";

function NavigationIcon({
  name,
  className,
}: {
  name: NavigationIconName;
  className: string;
}) {
  switch (name) {
    case "home":
      return <Home className={className} aria-hidden="true" />;
    case "projects":
      return <FolderKanban className={className} aria-hidden="true" />;
    case "tasks":
      return <CheckSquare2 className={className} aria-hidden="true" />;
    case "opportunities":
      return <BriefcaseBusiness className={className} aria-hidden="true" />;
    case "guilds":
      return <Shield className={className} aria-hidden="true" />;
    case "people":
      return <UsersRound className={className} aria-hidden="true" />;
    case "governance":
      return <Vote className={className} aria-hidden="true" />;
    case "events":
      return <CalendarDays className={className} aria-hidden="true" />;
    case "portfolio":
      return <Landmark className={className} aria-hidden="true" />;
    case "chats":
      return <MessageCircle className={className} aria-hidden="true" />;
    case "wallets":
      return <WalletCards className={className} aria-hidden="true" />;
    case "settings":
      return <Settings className={className} aria-hidden="true" />;
    case "kyc":
      return <ShieldCheck className={className} aria-hidden="true" />;
  }
}

interface NavigationGroup {
  label: string;
  items: NavigationItem[];
}

const navigationGroups: NavigationGroup[] = [
  {
    label: "Workspace",
    items: [
      { href: "/dashboard", label: "Home", icon: "home" },
      { href: "/projects", label: "Projects", icon: "projects" },
    ],
  },
  {
    label: "Work",
    items: [
      { href: "/tasks", label: "Tasks", icon: "tasks" },
      {
        href: "/opportunities",
        label: "Opportunities",
        icon: "opportunities",
      },
    ],
  },
  {
    label: "Community",
    items: [
      { href: "/guilds", label: "Guilds", icon: "guilds" },
      { href: "/people", label: "People", icon: "people" },
      { href: "/governance", label: "Governance", icon: "governance" },
      { href: "/events", label: "Events", icon: "events" },
    ],
  },
  {
    label: "Records",
    items: [
      { href: "/portfolio", label: "Portfolio", icon: "portfolio" },
      { href: "/chats", label: "Chats", icon: "chats" },
    ],
  },
];

const utilityItems: NavigationItem[] = [
  { href: "/settings/wallets", label: "Wallets", icon: "wallets" },
  { href: "/settings", label: "Settings", icon: "settings" },
];

function isCurrentPath(pathname: string, href: string) {
  if (href === "/dashboard") return pathname === href;
  if (href === "/settings") {
    return (
      pathname === href ||
      (pathname.startsWith("/settings/") &&
        !pathname.startsWith("/settings/wallets"))
    );
  }
  return pathname === href || pathname.startsWith(`${href}/`);
}

function NavigationLink({
  item,
  pathname,
  onNavigate,
}: {
  item: NavigationItem;
  pathname: string;
  onNavigate?: () => void;
}) {
  const active = isCurrentPath(pathname, item.href);
  return (
    <Link
      href={item.href}
      onClick={onNavigate}
      aria-current={active ? "page" : undefined}
      className={cn(
        "flex min-h-11 items-center gap-3 border-l-4 border-transparent px-3 text-sm font-semibold transition-colors",
        active
          ? "border-sidebar-primary bg-sidebar-foreground text-sidebar"
          : "text-sidebar-foreground hover:border-sidebar-primary hover:bg-sidebar-accent",
      )}
    >
      <NavigationIcon name={item.icon} className="size-[1.125rem]" />
      <span>{item.label}</span>
    </Link>
  );
}

export function WorkspaceNavigation({
  user,
  onNavigate,
  authPreview = false,
}: {
  user?: WorkspaceUser | null;
  onNavigate?: () => void;
  authPreview?: boolean;
}) {
  const pathname = usePathname();

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <div className="border-sidebar-border border-b-2 p-3">
        <Link
          href="/studio"
          onClick={onNavigate}
          aria-current={isCurrentPath(pathname, "/studio") ? "page" : undefined}
          className={cn(
            "border-sidebar-primary flex min-h-12 items-center justify-between gap-3 border-2 px-3 font-mono text-sm font-bold tracking-[0.08em] uppercase transition-colors",
            isCurrentPath(pathname, "/studio")
              ? "bg-sidebar-primary text-sidebar-primary-foreground"
              : "bg-sidebar text-sidebar-primary hover:bg-sidebar-primary hover:text-sidebar-primary-foreground",
          )}
        >
          <span className="flex items-center gap-2">
            <Sparkles className="size-4" aria-hidden="true" />
            Nova Studio
          </span>
          <span aria-hidden="true">↗</span>
        </Link>
      </div>

      <nav
        aria-label="Primary workspace navigation"
        className="min-h-0 flex-1 overflow-y-auto py-2"
      >
        {navigationGroups.map((group) => (
          <div key={group.label} role="group" aria-label={group.label}>
            <p className="text-sidebar-muted px-4 pt-4 pb-1 font-mono text-[0.65rem] font-bold tracking-[0.16em] uppercase">
              {group.label}
            </p>
            <div className="px-2">
              {group.items.map((item) => (
                <NavigationLink
                  key={item.href}
                  item={item}
                  pathname={pathname}
                  onNavigate={onNavigate}
                />
              ))}
            </div>
          </div>
        ))}

        {user?.role === "ADMIN" && (
          <div role="group" aria-label="Administration">
            <p className="text-sidebar-muted px-4 pt-4 pb-1 font-mono text-[0.65rem] font-bold tracking-[0.16em] uppercase">
              Administration
            </p>
            <div className="px-2">
              <NavigationLink
                item={{
                  href: "/admin/kyc",
                  label: "Identity operations",
                  icon: "kyc",
                }}
                pathname={pathname}
                onNavigate={onNavigate}
              />
            </div>
          </div>
        )}
      </nav>

      <nav
        aria-label="Workspace utilities"
        className="border-sidebar-border border-t-2 px-2 py-2"
      >
        {utilityItems.map((item) => (
          <NavigationLink
            key={item.href}
            item={item}
            pathname={pathname}
            onNavigate={onNavigate}
          />
        ))}
      </nav>

      {user && (
        <div className="border-sidebar-border flex items-center gap-3 border-t-2 p-3">
          <Link
            href="/dashboard/profile"
            onClick={onNavigate}
            className="inline-flex size-11 shrink-0 items-center justify-center"
            aria-label={`View ${user.name ?? "your"} profile`}
          >
            <Avatar className="border-sidebar-border size-10 border-2">
              <AvatarImage src={user.image ?? ""} alt="" />
              <AvatarFallback className="bg-sidebar-accent text-sidebar-foreground font-mono font-bold">
                {user.name?.charAt(0)?.toUpperCase() ?? "A"}
              </AvatarFallback>
            </Avatar>
          </Link>
          <Link
            href="/dashboard/profile"
            onClick={onNavigate}
            className="flex min-h-11 min-w-0 flex-1 flex-col justify-center"
          >
            <p className="truncate text-sm font-semibold">
              {user.name ?? "ArdaNova member"}
            </p>
            <p className="text-sidebar-muted truncate text-xs">{user.email}</p>
          </Link>
          {authPreview ? (
            <Link
              href="/"
              onClick={onNavigate}
              className="hover:bg-sidebar-accent inline-flex size-11 items-center justify-center border-2 border-transparent transition-colors"
              aria-label="Return to public site"
            >
              <LogOut className="size-4" aria-hidden="true" />
            </Link>
          ) : (
            <button
              type="button"
              onClick={() => {
                onNavigate?.();
                void signOut({ callbackUrl: "/" });
              }}
              className="hover:bg-sidebar-accent inline-flex size-11 items-center justify-center border-2 border-transparent transition-colors"
              aria-label="Sign out"
            >
              <LogOut className="size-4" aria-hidden="true" />
            </button>
          )}
        </div>
      )}
    </div>
  );
}

export function AppSidebar({ user, authPreview = false }: AppSidebarProps) {
  return (
    <aside className="border-sidebar-border bg-sidebar text-sidebar-foreground sticky top-0 hidden h-dvh w-[17rem] shrink-0 flex-col border-r-2 lg:flex">
      <div className="border-sidebar-border flex min-h-16 items-center border-b-2 px-5">
        <Link
          href="/dashboard"
          className="inline-flex min-h-11 items-center font-mono text-xl font-black tracking-[-0.06em] uppercase"
          aria-label="ArdaNova home"
        >
          ARDA<span className="text-sidebar-primary">NOVA</span>
        </Link>
      </div>

      <WorkspaceNavigation user={user} authPreview={authPreview} />
    </aside>
  );
}

export function SidebarInset({ children }: { children: React.ReactNode }) {
  return <div className="min-w-0 flex-1">{children}</div>;
}
