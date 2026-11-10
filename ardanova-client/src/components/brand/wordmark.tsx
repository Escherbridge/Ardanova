import { cn } from "~/lib/utils";

type WordmarkProps = {
  className?: string;
};

/** Square ArdaNova mark used in headers. The file includes its parchment ground. */
export function ArdaNovaWordmark({ className }: WordmarkProps) {
  return (
    // The mark is a static SVG in /public. next/image does not optimize SVG.
    // eslint-disable-next-line @next/next/no-img-element
    <img
      src="/brand/ardanova-logo.svg"
      alt="ArdaNova"
      width={40}
      height={40}
      className={cn("h-10 w-10", className)}
    />
  );
}
