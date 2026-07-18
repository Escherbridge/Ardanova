"use client";

import Link from "next/link";
import { ArrowLeft, Info, Vote, Folder } from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";

export default function CreateProposalPage() {
  return (
    <div className="bg-background min-h-screen">
      <div className="mx-auto max-w-2xl px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <Button variant="ghost" asChild className="mb-4 -ml-2">
            <Link href="/governance">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Governance
            </Link>
          </Button>
          <h1 className="flex items-center gap-3 text-3xl font-bold">
            <div className="bg-system/20 flex h-10 w-10 items-center justify-center rounded-none">
              <Vote className="text-system h-5 w-5" />
            </div>
            Create Proposal
          </h1>
          <p className="text-muted-foreground mt-2">
            Proposals are created within projects
          </p>
        </div>

        {/* Info Card */}
        <Card className="bg-card border-border">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <Info className="text-system h-5 w-5" />
              How to Create a Proposal
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-muted-foreground">
              To submit a proposal, you need to go to a specific project first.
              Proposals are tied to projects and can only be created within the
              project context.
            </p>

            <div className="mt-4 space-y-3">
              <div className="flex items-start gap-3">
                <div className="bg-system/20 flex h-8 w-8 shrink-0 items-center justify-center rounded-none">
                  <span className="text-system text-sm font-bold">1</span>
                </div>
                <div>
                  <p className="text-foreground font-medium">Browse Projects</p>
                  <p className="text-muted-foreground text-sm">
                    Find a project you want to create a proposal for
                  </p>
                </div>
              </div>

              <div className="flex items-start gap-3">
                <div className="bg-system/20 flex h-8 w-8 shrink-0 items-center justify-center rounded-none">
                  <span className="text-system text-sm font-bold">2</span>
                </div>
                <div>
                  <p className="text-foreground font-medium">
                    Go to Proposals Tab
                  </p>
                  <p className="text-muted-foreground text-sm">
                    Navigate to the Proposals tab within the project
                  </p>
                </div>
              </div>

              <div className="flex items-start gap-3">
                <div className="bg-system/20 flex h-8 w-8 shrink-0 items-center justify-center rounded-none">
                  <span className="text-system text-sm font-bold">3</span>
                </div>
                <div>
                  <p className="text-foreground font-medium">Create Proposal</p>
                  <p className="text-muted-foreground text-sm">
                    Click the &quot;New Proposal&quot; button to create your
                    proposal
                  </p>
                </div>
              </div>
            </div>

            <div className="bg-system/10 border-system/30 mt-6 flex items-start gap-3 rounded-none border p-4">
              <Info className="text-system mt-0.5 h-5 w-5 shrink-0" />
              <div className="text-sm">
                <p className="text-system font-medium">
                  Why project-level proposals?
                </p>
                <p className="text-muted-foreground mt-1">
                  This ensures that proposals are always contextual and relevant
                  to specific projects, making governance more organized and
                  meaningful.
                </p>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Action Buttons */}
        <div className="mt-6 flex gap-4">
          <Button variant="neon" className="flex-1" asChild>
            <Link href="/projects">
              <Folder className="mr-2 h-4 w-4" />
              Browse Projects
            </Link>
          </Button>
          <Button variant="outline" asChild>
            <Link href="/governance">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back
            </Link>
          </Button>
        </div>
      </div>
    </div>
  );
}
