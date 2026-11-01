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
              Back to governance
            </Link>
          </Button>
          <h1 className="flex items-center gap-3 text-3xl font-bold">
            <div className="bg-system/20 flex h-10 w-10 items-center justify-center rounded-none">
              <Vote className="text-system h-5 w-5" />
            </div>
            New proposal
          </h1>
          <p className="text-muted-foreground mt-2">
            Proposals start in projects.
          </p>
        </div>

        {/* Info Card */}
        <Card className="bg-card border-border">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <Info className="text-system h-5 w-5" />
              How to propose
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-3">
              <div className="flex items-start gap-3">
                <div className="bg-system/20 flex h-8 w-8 shrink-0 items-center justify-center rounded-none">
                  <span className="text-system text-sm font-bold">1</span>
                </div>
                <p className="text-foreground font-medium">
                  Open a project you belong to
                </p>
              </div>

              <div className="flex items-start gap-3">
                <div className="bg-system/20 flex h-8 w-8 shrink-0 items-center justify-center rounded-none">
                  <span className="text-system text-sm font-bold">2</span>
                </div>
                <p className="text-foreground font-medium">
                  Open its Proposals tab
                </p>
              </div>

              <div className="flex items-start gap-3">
                <div className="bg-system/20 flex h-8 w-8 shrink-0 items-center justify-center rounded-none">
                  <span className="text-system text-sm font-bold">3</span>
                </div>
                <p className="text-foreground font-medium">
                  Start a new proposal
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
              Browse projects
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
