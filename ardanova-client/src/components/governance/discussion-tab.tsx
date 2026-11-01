"use client";

import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { MessageCircle } from "lucide-react";

interface DiscussionTabProps {
  proposalId: string;
}

export default function DiscussionTab({
  proposalId: _proposalId,
}: DiscussionTabProps) {
  return (
    <div className="space-y-6">
      {/* Discussion Card */}
      <Card className="bg-card border-border border-2">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-lg">
            <div className="bg-primary/20 border-primary/30 flex h-8 w-8 items-center justify-center rounded-lg border">
              <MessageCircle className="text-primary size-4" />
            </div>
            Discussion
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-4 py-8 text-center">
            <div className="flex justify-center">
              <div className="bg-muted flex h-16 w-16 items-center justify-center rounded-none">
                <MessageCircle className="text-muted-foreground size-8" />
              </div>
            </div>
            <div className="space-y-2">
              <h3 className="text-foreground text-lg font-semibold">
                Discussion is coming soon.
              </h3>
            </div>
          </div>

          {/* Future Discussion Form (Disabled) */}
          <div className="pointer-events-none mt-6 space-y-3 opacity-50">
            <div>
              <label
                htmlFor="comment"
                className="mb-1.5 block text-sm font-medium"
              >
                Add a comment
              </label>
              <textarea
                id="comment"
                disabled
                placeholder="What do you think?"
                className="border-border bg-background text-foreground focus:ring-primary min-h-[100px] w-full resize-y rounded border px-3 py-2 focus:ring-2 focus:outline-none"
              />
            </div>
            <Button type="button" variant="neon" className="w-full" disabled>
              <MessageCircle className="mr-2 size-4" />
              Post Comment
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
