import { cn } from "~/lib/utils";

/** Sister sites, omitting ArdaNova. Anchors are the public footer copy. */
export const ESCHERBRIDGE_PROJECTS = [
  {
    href: "https://primusneo.com",
    label: "PrimusNeo, the technology innovation lab",
  },
  {
    href: "https://neos.primusneo.com",
    label: "NEOS, a workspace for agreements and decisions",
  },
  {
    href: "https://azoa.ardanova.com",
    label: "AZOA, an engine for structured workflows",
  },
  {
    href: "https://aevani.com",
    label: "Aevani, a marketplace for growing tools",
  },
  {
    href: "https://plantgeo.aevani.com",
    label: "PlantGeo, a 3D map for planning a site",
  },
  {
    href: "https://overseasgenai.com",
    label: "OverseasGenAI, a venture shipyard",
  },
  {
    href: "https://glitxz.com",
    label: "Glitxz, an artist-led market for work in progress",
  },
] as const;

/** Compact sister-site list. Links are follow links on each site's own domain. */
export function EscherbridgeProjects({
  className,
  linkClassName,
}: {
  className?: string;
  linkClassName?: string;
}) {
  return (
    <nav aria-label="An Escherbridge project" className={className}>
      <p className="font-mono text-[10px] font-semibold tracking-[0.14em] uppercase">
        An Escherbridge project
      </p>
      <ul className="mt-3 flex flex-col gap-1 text-sm">
        {ESCHERBRIDGE_PROJECTS.map((site) => (
          <li key={site.href}>
            <a
              className={cn(
                "inline-flex min-h-11 items-center underline-offset-4 hover:underline",
                linkClassName,
              )}
              href={site.href}
            >
              {site.label}
            </a>
          </li>
        ))}
      </ul>
    </nav>
  );
}
