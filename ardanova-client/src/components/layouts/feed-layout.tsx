import { cn } from "~/lib/utils";

const SIDEBAR_WIDTH = "w-[300px]";

interface FeedLayoutProps {
  children: React.ReactNode;
  sidebar?: React.ReactNode;
  className?: string;
}

export function FeedLayout({ children, sidebar, className }: FeedLayoutProps) {
  return (
    <div className={cn("h-screen bg-background", className)}>
      <div className="flex h-full">
        <div className="flex-1 min-w-0 scrollbar-hide border-x-2 border-border h-full overflow-y-auto">
          {children}
        </div>
        {sidebar && (
          <div className={cn("hidden xl:block shrink-0 ml-[.5vw]", SIDEBAR_WIDTH, "p-4 space-y-4 h-full overflow-y-auto scrollbar-hide border-l-2 border-border bg-background")}>
            {sidebar}
          </div>
        )}
      </div>
    </div>
  );
}
