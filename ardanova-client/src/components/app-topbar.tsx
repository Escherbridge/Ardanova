"use client";

import { Menu, Radio } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState } from "react";

import {
  WorkspaceNavigation,
  type WorkspaceUser,
} from "~/components/app-sidebar";
import { NovaAssistant } from "~/components/ai/nova-assistant";
import { Button } from "~/components/ui/button";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from "~/components/ui/sheet";
import { getNovaContext } from "~/lib/nova-context";

export function AppTopbar({
  user,
  authPreview = false,
}: {
  user?: WorkspaceUser | null;
  authPreview?: boolean;
}) {
  const pathname = usePathname();
  const context = getNovaContext(pathname);
  const [mobileOpen, setMobileOpen] = useState(false);

  return (
    <header className="bg-background sticky top-0 z-30 border-b-2">
      {authPreview && (
        <div
          role="status"
          className="bg-primary text-primary-foreground flex min-h-8 items-center justify-center gap-2 border-b-2 px-4 text-center font-mono text-[0.65rem] font-bold tracking-[0.1em] uppercase"
        >
          <Radio className="size-3" aria-hidden="true" />
          Local preview session · provider sign-in bypassed; backend permissions
          still apply
        </div>
      )}

      <div className="flex min-h-16 items-center gap-3 px-3 sm:px-5">
        <Sheet open={mobileOpen} onOpenChange={setMobileOpen}>
          <SheetTrigger asChild>
            <Button
              variant="outline"
              size="icon"
              className="lg:hidden"
              aria-label="Open workspace navigation"
            >
              <Menu className="size-5" aria-hidden="true" />
            </Button>
          </SheetTrigger>
          <SheetContent
            side="left"
            className="bg-sidebar text-sidebar-foreground w-[min(90vw,20rem)] gap-0 border-r-2 p-0 shadow-none sm:max-w-none"
          >
            <SheetHeader className="border-sidebar-border min-h-16 justify-center border-b-2 px-5 py-3 text-left">
              <SheetTitle asChild>
                <Link
                  href="/dashboard"
                  onClick={() => setMobileOpen(false)}
                  className="font-mono text-xl font-black tracking-[-0.06em] uppercase"
                >
                  ARDA<span className="text-sidebar-primary">NOVA</span>
                </Link>
              </SheetTitle>
              <SheetDescription className="sr-only">
                ArdaNova workspace navigation
              </SheetDescription>
            </SheetHeader>
            <WorkspaceNavigation
              user={user}
              authPreview={authPreview}
              onNavigate={() => setMobileOpen(false)}
            />
          </SheetContent>
        </Sheet>

        <Link
          href="/dashboard"
          className="inline-flex min-h-11 items-center font-mono text-lg font-black tracking-[-0.06em] uppercase lg:hidden"
          aria-label="ArdaNova home"
        >
          ARDA<span className="text-primary">NOVA</span>
        </Link>

        <div className="hidden min-w-0 flex-1 sm:block">
          <p className="text-muted-foreground font-mono text-[0.62rem] font-bold tracking-[0.14em] uppercase">
            {context.artifact}
          </p>
          <p className="truncate text-sm font-semibold">{context.title}</p>
        </div>

        <div className="ml-auto flex items-center gap-3">
          <div className="hidden text-right xl:block">
            <p className="text-sm font-semibold">
              {user?.name ?? "ArdaNova member"}
            </p>
            <p className="text-muted-foreground font-mono text-[0.62rem] tracking-[0.1em] uppercase">
              Human review required
            </p>
          </div>
          <NovaAssistant pathname={pathname} />
        </div>
      </div>
    </header>
  );
}
