"use client";

import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { MessageCircle } from "lucide-react";

interface DiscussionTabProps {
  proposalId: string;
}

export default function DiscussionTab({ proposalId }: DiscussionTabProps) {
  return (
    <div className="space-y-6">
      {/* Discussion Card */}
      <Card className="bg-card border-2 border-border">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <div className="w-8 h-8 bg-primary/20 rounded-lg flex items-center justify-center border border-primary/30">
              <MessageCircle className="size-4 text-primary" />
            </div>
            Discussion
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="text-center py-8 space-y-4">
            <div className="flex justify-center">
              <div className="w-16 h-16 bg-muted rounded-full flex items-center justify-center">
                <MessageCircle className="size-8 text-muted-foreground" />
              </div>
            </div>
            <div className="space-y-2">
              <h3 className="text-lg font-semibold text-foreground">
                Discussion Coming Soon
              </h3>
              <p className="text-muted-foreground max-w-md mx-auto">
                This feature will allow proposal discussions and debates.
                Community members will be able to share their thoughts, ask questions,
                and engage in meaningful dialogue about proposals.
              </p>
            </div>
          </div>

          {/* Future Discussion Form (Disabled) */}
          <div className="mt-6 space-y-3 opacity-50 pointer-events-none">
            <div>
              <label htmlFor="comment" className="text-sm font-medium block mb-1.5">
                Add Your Comment
              </label>
              <textarea
                id="comment"
                disabled
                placeholder="Share your thoughts on this proposal..."
                className="w-full px-3 py-2 border border-border rounded bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary min-h-[100px] resize-y"
              />
            </div>
            <Button type="button" variant="neon" className="w-full" disabled>
              <MessageCircle className="size-4 mr-2" />
              Post Comment
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
