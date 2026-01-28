"use client";

import Link from "next/link";
import { ArrowLeft, Info, Vote, Folder } from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";

export default function CreateProposalPage() {

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-2xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <Button variant="ghost" asChild className="mb-4 -ml-2">
            <Link href="/governance">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Governance
            </Link>
          </Button>
          <h1 className="text-3xl font-bold flex items-center gap-3">
            <div className="w-10 h-10 bg-neon-purple/20 rounded-lg flex items-center justify-center">
              <Vote className="h-5 w-5 text-neon-purple" />
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
            <CardTitle className="text-lg flex items-center gap-2">
              <Info className="h-5 w-5 text-neon" />
              How to Create a Proposal
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-muted-foreground">
              To submit a proposal, you need to go to a specific project first. Proposals
              are tied to projects and can only be created within the project context.
            </p>

            <div className="space-y-3 mt-4">
              <div className="flex items-start gap-3">
                <div className="w-8 h-8 rounded-full bg-neon/20 flex items-center justify-center shrink-0">
                  <span className="text-sm font-bold text-neon">1</span>
                </div>
                <div>
                  <p className="font-medium text-foreground">Browse Projects</p>
                  <p className="text-sm text-muted-foreground">
                    Find a project you want to create a proposal for
                  </p>
                </div>
              </div>

              <div className="flex items-start gap-3">
                <div className="w-8 h-8 rounded-full bg-neon/20 flex items-center justify-center shrink-0">
                  <span className="text-sm font-bold text-neon">2</span>
                </div>
                <div>
                  <p className="font-medium text-foreground">Go to Proposals Tab</p>
                  <p className="text-sm text-muted-foreground">
                    Navigate to the Proposals tab within the project
                  </p>
                </div>
              </div>

              <div className="flex items-start gap-3">
                <div className="w-8 h-8 rounded-full bg-neon/20 flex items-center justify-center shrink-0">
                  <span className="text-sm font-bold text-neon">3</span>
                </div>
                <div>
                  <p className="font-medium text-foreground">Create Proposal</p>
                  <p className="text-sm text-muted-foreground">
                    Click the "New Proposal" button to create your proposal
                  </p>
                </div>
              </div>
            </div>

            <div className="flex items-start gap-3 p-4 bg-neon/10 rounded-lg border border-neon/30 mt-6">
              <Info className="h-5 w-5 text-neon mt-0.5 shrink-0" />
              <div className="text-sm">
                <p className="font-medium text-neon">Why project-level proposals?</p>
                <p className="text-muted-foreground mt-1">
                  This ensures that proposals are always contextual and relevant to specific
                  projects, making governance more organized and meaningful.
                </p>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Action Buttons */}
        <div className="mt-6 flex gap-4">
          <Button variant="neon" className="flex-1" asChild>
            <Link href="/projects">
              <Folder className="h-4 w-4 mr-2" />
              Browse Projects
            </Link>
          </Button>
          <Button variant="outline" asChild>
            <Link href="/governance">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back
            </Link>
          </Button>
        </div>
      </div>
    </div>
  );
}
