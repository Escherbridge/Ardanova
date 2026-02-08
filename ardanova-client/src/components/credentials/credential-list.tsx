"use client";

import { useState } from "react";
import { Badge } from "~/components/ui/badge";
import { Card, CardContent } from "~/components/ui/card";
import { CredentialCard } from "./credential-card";
import { Loader2, ShieldOff } from "lucide-react";
import { cn } from "~/lib/utils";
import type { MembershipCredential } from "~/lib/api/ardanova/endpoints/membership-credentials";

type FilterStatus = "ALL" | "ACTIVE" | "REVOKED";

interface CredentialListProps {
  credentials?: MembershipCredential[];
  isLoading?: boolean;
  scopeNames?: Record<string, string>;
  onCredentialClick?: (credential: MembershipCredential) => void;
  emptyMessage?: string;
  showFilter?: boolean;
  className?: string;
}

export function CredentialList({
  credentials,
  isLoading,
  scopeNames,
  onCredentialClick,
  emptyMessage = "No credentials yet",
  showFilter = true,
  className,
}: CredentialListProps) {
  const [filter, setFilter] = useState<FilterStatus>("ALL");

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="size-6 animate-spin text-muted-foreground" />
      </div>
    );
  }

  const filtered = credentials?.filter((c) => {
    if (filter === "ALL") return true;
    return c.status === filter;
  });

  if (!filtered || filtered.length === 0) {
    return (
      <div className={cn("space-y-4", className)}>
        {showFilter && credentials && credentials.length > 0 && (
          <FilterBar filter={filter} onFilterChange={setFilter} />
        )}
        <Card variant="ghost" padding="default">
          <CardContent className="flex flex-col items-center justify-center py-8 text-center">
            <ShieldOff className="size-10 text-muted-foreground mb-3" />
            <p className="text-muted-foreground">{emptyMessage}</p>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className={cn("space-y-4", className)}>
      {showFilter && credentials && credentials.length > 1 && (
        <FilterBar filter={filter} onFilterChange={setFilter} />
      )}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {filtered.map((credential) => {
          const scopeId = credential.projectId ?? credential.guildId ?? "";
          const scopeName = scopeNames?.[scopeId];

          return (
            <CredentialCard
              key={credential.id}
              credential={credential}
              scopeName={scopeName}
              onClick={
                onCredentialClick
                  ? () => onCredentialClick(credential)
                  : undefined
              }
            />
          );
        })}
      </div>
    </div>
  );
}

function FilterBar({
  filter,
  onFilterChange,
}: {
  filter: FilterStatus;
  onFilterChange: (f: FilterStatus) => void;
}) {
  const options: { value: FilterStatus; label: string }[] = [
    { value: "ALL", label: "All" },
    { value: "ACTIVE", label: "Active" },
    { value: "REVOKED", label: "Revoked" },
  ];

  return (
    <div className="flex gap-1">
      {options.map((opt) => (
        <Badge
          key={opt.value}
          variant={filter === opt.value ? "default" : "outline"}
          className="cursor-pointer"
          onClick={() => onFilterChange(opt.value)}
        >
          {opt.label}
        </Badge>
      ))}
    </div>
  );
}

export type { CredentialListProps, FilterStatus };
