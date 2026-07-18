import { cn } from "~/lib/utils";

type WordmarkProps = {
  className?: string;
  accentClassName?: string;
};

export function ArdaNovaWordmark({
  className,
  accentClassName,
}: WordmarkProps) {
  return (
    <span
      aria-label="ArdaNova"
      className={cn(
        "inline-flex items-center gap-2 text-sm leading-none font-black tracking-[-0.04em] uppercase",
        className,
      )}
    >
      <span aria-hidden="true">ARDANOVA</span>
      <span
        aria-hidden="true"
        className={cn("size-2.5 bg-[#ef4638]", accentClassName)}
      />
    </span>
  );
}
