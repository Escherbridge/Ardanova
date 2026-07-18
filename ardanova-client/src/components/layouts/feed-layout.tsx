import { cn } from "~/lib/utils";

const SIDEBAR_WIDTH = "w-[300px]";

interface FeedLayoutProps {
  children: React.ReactNode;
  sidebar?: React.ReactNode;
  className?: string;
}

export function FeedLayout({ children, sidebar, className }: FeedLayoutProps) {
  return (
    <div className={cn("bg-background min-h-0", className)}>
      <div className="flex min-h-0 items-start">
        <div className="border-border min-w-0 flex-1 border-x-2">
          {children}
        </div>
        {sidebar && (
          <aside
            className={cn(
              "border-border bg-background ml-[.5vw] hidden shrink-0 space-y-4 border-l-2 p-4 xl:block",
              SIDEBAR_WIDTH,
            )}
          >
            {sidebar}
          </aside>
        )}
      </div>
    </div>
  );
}
