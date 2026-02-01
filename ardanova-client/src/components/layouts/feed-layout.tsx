import { cn } from "~/lib/utils";

interface FeedLayoutProps {
  children: React.ReactNode;
  sidebar?: React.ReactNode;
  className?: string;
}

export function FeedLayout({ children, sidebar, className }: FeedLayoutProps) {
  return (
    <div className={cn("h-screen bg-background", className)}>
      <div className="flex h-full">
        <div className="min-w-[33vw] max-w-2xl border-x-2 border-border h-full overflow-y-auto">
          {children}
        </div>
        {sidebar && (
          <div className="hidden xl:block shrink-0 w-80 p-4 space-y-4 h-full overflow-y-auto border-l-2 border-border bg-background">
            {sidebar}
          </div>
        )}
      </div>
    </div>
  );
}
