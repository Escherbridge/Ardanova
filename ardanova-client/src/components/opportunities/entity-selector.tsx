"use client";

import { useState } from "react";
import { Building2, FolderKanban, ArrowLeft, Loader2 } from "lucide-react";
import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Badge } from "~/components/ui/badge";
import { api } from "~/trpc/react";
import Link from "next/link";

interface EntitySelectorProps {
  onSelect: (
    entityType: "guild" | "project",
    entityId: string,
    entitySlug: string,
  ) => void;
}

type EntityType = "guild" | "project" | null;

interface SelectableEntity {
  id: string;
  slug: string;
  name: string;
  description?: string | null;
  image?: string;
}

const entityConfig: Record<
  Exclude<EntityType, null>,
  {
    label: string;
    variant: "neon-purple" | "neon";
    description: string;
  }
> = {
  guild: {
    label: "Guild",
    variant: "neon-purple" as const,
    description: "Post opportunity on behalf of your guild",
  },
  project: {
    label: "Project",
    variant: "neon" as const,
    description: "Post opportunity for your project",
  },
};

export default function EntitySelector({ onSelect }: EntitySelectorProps) {
  const [selectedType, setSelectedType] = useState<EntityType>(null);

  // Fetch user's entities
  const {
    data: guilds,
    isLoading: guildsLoading,
    error: guildsError,
  } = api.guild.getMyGuilds.useQuery(undefined, {
    enabled: selectedType === "guild",
  });

  const {
    data: projects,
    isLoading: projectsLoading,
    error: projectsError,
  } = api.project.getMyProjects.useQuery(
    { limit: 100, page: 1 },
    { enabled: selectedType === "project" },
  );

  // Handle entity type selection
  const handleTypeSelect = (type: EntityType) => {
    setSelectedType(type);
  };

  // Handle specific entity selection
  const handleEntitySelect = (entityId: string, entitySlug: string) => {
    if (selectedType) {
      onSelect(selectedType, entityId, entitySlug);
    }
  };

  // Render entity type selection cards
  if (!selectedType) {
    return (
      <div className="space-y-4">
        <div>
          <h3 className="mb-2 text-lg font-bold">Select Entity Type</h3>
          <p className="text-muted-foreground text-sm">
            Choose which entity will be posting this opportunity
          </p>
        </div>

        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          {(
            Object.entries(entityConfig) as [
              keyof typeof entityConfig,
              (typeof entityConfig)[keyof typeof entityConfig],
            ][]
          ).map(([type, config]) => {
            return (
              <Card
                key={type}
                variant="interactive"
                role="button"
                tabIndex={0}
                aria-label={`Choose ${config.label} as the opportunity owner`}
                className="focus-visible:ring-ring cursor-pointer focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none"
                onClick={() => handleTypeSelect(type)}
                onKeyDown={(event) => {
                  if (event.key === "Enter" || event.key === " ") {
                    event.preventDefault();
                    handleTypeSelect(type);
                  }
                }}
              >
                <CardHeader className="text-center">
                  <div className="border-border bg-card mx-auto mb-3 flex size-12 items-center justify-center rounded-none border-2">
                    {type === "guild" ? (
                      <Building2 className="text-primary size-6" />
                    ) : (
                      <FolderKanban className="text-primary size-6" />
                    )}
                  </div>
                  <CardTitle className="text-base">{config.label}</CardTitle>
                </CardHeader>
                <CardContent className="text-center">
                  <p className="text-muted-foreground text-sm">
                    {config.description}
                  </p>
                </CardContent>
              </Card>
            );
          })}
        </div>
      </div>
    );
  }

  // Render entity list based on selected type
  const config = entityConfig[selectedType];

  const isLoading = selectedType === "guild" ? guildsLoading : projectsLoading;
  const queryError = selectedType === "guild" ? guildsError : projectsError;

  const entities: SelectableEntity[] =
    selectedType === "guild"
      ? (guilds ?? []).map((guild) => ({
          id: guild.id,
          slug: guild.slug ?? guild.id,
          name: guild.name,
          description: guild.description,
          image: guild.logo ?? undefined,
        }))
      : (projects?.items ?? []).map((project) => ({
          id: project.id,
          slug: project.slug ?? project.id,
          name: project.title,
          description: project.description,
          image: project.images?.split(",")[0] || undefined,
        }));

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3">
        <Button
          type="button"
          variant="ghost"
          size="icon-lg"
          aria-label="Back to entity types"
          onClick={() => setSelectedType(null)}
        >
          <ArrowLeft className="size-4" />
        </Button>
        <div>
          <h3 className="text-lg font-bold">Select {config.label}</h3>
          <p className="text-muted-foreground text-sm">
            Choose from your {config.label.toLowerCase()}s
          </p>
        </div>
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="text-primary size-8 animate-spin" />
          <span className="sr-only">Loading {config.label.toLowerCase()}s</span>
        </div>
      ) : queryError ? (
        <Card
          variant="outlined"
          className="border-destructive bg-destructive/10 p-8 text-center"
          role="alert"
        >
          <h4 className="text-destructive mb-2 text-lg font-semibold">
            {config.label}s could not be loaded
          </h4>
          <p className="text-muted-foreground text-sm">
            Try again before choosing who will own this opportunity.
          </p>
        </Card>
      ) : entities.length === 0 ? (
        <Card variant="outlined" className="p-8 text-center">
          {selectedType === "guild" ? (
            <Building2 className="text-muted-foreground mx-auto mb-4 size-12" />
          ) : (
            <FolderKanban className="text-muted-foreground mx-auto mb-4 size-12" />
          )}
          <h4 className="mb-2 text-lg font-semibold">
            No {config.label}s Found
          </h4>
          <p className="text-muted-foreground mb-4 text-sm">
            You don&apos;t have any {config.label.toLowerCase()}s yet. Create
            one to post opportunities.
          </p>
          <Button variant="neon" asChild className="min-h-11">
            <Link href={`/${selectedType}s/create`}>Create {config.label}</Link>
          </Button>
        </Card>
      ) : (
        <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
          {entities.map((entity) => (
            <Card
              key={entity.id}
              variant="interactive"
              role="button"
              tabIndex={0}
              aria-label={`Choose ${entity.name}`}
              className="focus-visible:ring-ring min-h-11 cursor-pointer p-4 focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none"
              onClick={() => handleEntitySelect(entity.id, entity.slug)}
              onKeyDown={(event) => {
                if (event.key === "Enter" || event.key === " ") {
                  event.preventDefault();
                  handleEntitySelect(entity.id, entity.slug);
                }
              }}
            >
              <div className="flex items-center gap-3">
                <Avatar className="size-12">
                  <AvatarImage src={entity.image} alt="" />
                  <AvatarFallback>
                    {selectedType === "guild" ? (
                      <Building2 className="size-5" />
                    ) : (
                      <FolderKanban className="size-5" />
                    )}
                  </AvatarFallback>
                </Avatar>
                <div className="min-w-0 flex-1">
                  <h4 className="truncate font-semibold">{entity.name}</h4>
                  {entity.description && (
                    <p className="text-muted-foreground line-clamp-1 text-sm">
                      {entity.description}
                    </p>
                  )}
                </div>
                <div className="flex-shrink-0">
                  <Badge variant={config.variant}>{config.label}</Badge>
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
