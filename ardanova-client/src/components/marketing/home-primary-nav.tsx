"use client";

import { Menu, X } from "lucide-react";
import Link from "next/link";
import { useEffect, useId, useRef, useState } from "react";

/** The home page's primary destinations; mirrors the md+ nav in src/app/page.tsx. */
function homePrimaryLinks(projectsHref: string) {
  return [
    { label: "Method", href: "#method" },
    { label: "Rights", href: "#rights" },
    { label: "Nova", href: "#nova" },
    { label: "Projects", href: projectsHref },
  ];
}

/** Disclosure menu (button + panel) that carries the primary nav below the md breakpoint. */
export function HomePrimaryNavMenu({ projectsHref }: { projectsHref: string }) {
  const [open, setOpen] = useState(false);
  const panelId = useId();
  const toggleRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (!open) return;
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key !== "Escape") return;
      setOpen(false);
      toggleRef.current?.focus();
    };
    document.addEventListener("keydown", closeOnEscape);
    return () => document.removeEventListener("keydown", closeOnEscape);
  }, [open]);

  return (
    <div className="md:hidden">
      <button
        aria-controls={panelId}
        aria-expanded={open}
        className="border-border bg-background hover:bg-secondary inline-flex min-h-11 min-w-11 items-center justify-center gap-1.5 border px-3 text-xs font-semibold"
        onClick={() => setOpen((current) => !current)}
        ref={toggleRef}
        type="button"
      >
        {open ? (
          <X aria-hidden="true" className="size-4" />
        ) : (
          <Menu aria-hidden="true" className="size-4" />
        )}
        Menu
      </button>
      <nav
        aria-label="Primary navigation"
        className="bg-brand-paper absolute inset-x-0 top-full border-b border-black/70 px-4 py-2"
        hidden={!open}
        id={panelId}
      >
        <ul className="flex flex-col text-sm font-semibold">
          {homePrimaryLinks(projectsHref).map((link) => (
            <li key={link.label}>
              <Link
                className="hover:text-primary flex min-h-11 items-center"
                href={link.href}
                onClick={() => setOpen(false)}
              >
                {link.label}
              </Link>
            </li>
          ))}
        </ul>
      </nav>
    </div>
  );
}
