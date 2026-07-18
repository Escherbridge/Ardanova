import { cn } from "~/lib/utils";

type SignalCanvasProps = {
  className?: string;
};

export function SignalCanvas({ className }: SignalCanvasProps) {
  return (
    <figure
      className={cn(
        "relative isolate aspect-[4/5] w-full overflow-hidden border border-black bg-[#131313]",
        className,
      )}
    >
      <svg
        aria-label="An abstract map of a project moving from a draft through review to agreement"
        className="absolute inset-0 size-full"
        role="img"
        viewBox="0 0 640 800"
      >
        <rect fill="#131313" height="800" width="640" />
        <path
          d="M-18 99C75 24 178 29 221 101c41 69 14 177-70 218-80 39-153 6-192-54Z"
          fill="#ef4638"
        />
        <path
          d="M393 69c102-56 230 12 244 130 15 124-98 221-216 173-111-45-137-244-28-303Z"
          fill="#f2e8e4"
        />
        <path
          d="m68 643 180-286 94 129 127-205 112 407-213 99-112-151-92 127Z"
          fill="#ef4638"
        />
        <path
          d="M82 536 215 411l110 108 129-171 99 182"
          fill="none"
          stroke="#70d7e2"
          strokeWidth="5"
        />
        <g fill="#131313" stroke="#70d7e2" strokeWidth="5">
          <circle cx="82" cy="536" r="13" />
          <circle cx="215" cy="411" r="13" />
          <circle cx="325" cy="519" r="13" />
          <circle cx="454" cy="348" r="13" />
          <circle cx="553" cy="530" r="13" />
        </g>
        <path d="M0 713h640" stroke="#f2e8e4" strokeWidth="2" />
        <path d="M504 0v800" stroke="#f2e8e4" strokeWidth="2" />
        <text
          fill="#f2e8e4"
          fontFamily="monospace"
          fontSize="15"
          letterSpacing="2"
          x="24"
          y="755"
        >
          DRAFT / REVIEW / AGREE / RECORD
        </text>
        <text
          fill="#131313"
          fontFamily="sans-serif"
          fontSize="96"
          fontWeight="900"
          letterSpacing="-8"
          x="382"
          y="243"
        >
          AN
        </text>
      </svg>
      <figcaption className="absolute right-0 bottom-0 border-t border-l border-[#f2e8e4] bg-[#131313] px-3 py-2 font-mono text-[10px] tracking-[0.16em] text-[#f2e8e4] uppercase">
        Human review is the threshold
      </figcaption>
    </figure>
  );
}
