"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

import { cn } from "~/lib/utils";

const sections = [
  { href: "/settings/profile", label: "Profile" },
  { href: "/settings/verification", label: "Verification" },
  { href: "/settings/payouts", label: "Payouts" },
] as const;

/** Tabs across the settings sections. */
export function SettingsNav() {
  const pathname = usePathname();
  return (
    <nav aria-label="Settings sections" className="container mx-auto max-w-3xl pt-6">
      <ul className="flex gap-2 overflow-x-auto border-b-2 border-white/10">
        {sections.map((section) => {
          const current = pathname === section.href || pathname.startsWith(`${section.href}/`);
          return (
            <li key={section.href}>
              <Link
                href={section.href}
                aria-current={current ? "page" : undefined}
                className={cn(
                  "inline-flex min-h-11 items-center border-b-2 px-3 font-mono text-sm uppercase -mb-[2px]",
                  current
                    ? "border-primary text-foreground"
                    : "text-muted-foreground hover:text-foreground border-transparent",
                )}
              >
                {section.label}
              </Link>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
