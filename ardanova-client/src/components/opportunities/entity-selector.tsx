"use client";

import { useState } from "react";
import { Building2, FolderKanban, Store, ArrowLeft, Loader2 } from "lucide-react";
import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Badge } from "~/components/ui/badge";
import { api } from "~/trpc/react";

interface EntitySelectorProps {
  onSelect: (entityType: "guild" | "project" | "shop", entityId: string, entitySlug: string) => void;
}

type EntityType = "guild" | "project" | "shop" | null;

const entityConfig = {
  guild: {
    icon: Building2,
    label: "Guild",
    variant: "neon-purple" as const,
    description: "Post opportunity on behalf of your guild"
  },
  project: {
    icon: FolderKanban,
    label: "Project",
    variant: "neon" as const,
    description: "Post opportunity for your project"
  },
  shop: {
    icon: Store,
    label: "Shop",
    variant: "neon-green" as const,
    description: "Post opportunity for your shop"
  },
};

export default function EntitySelector({ onSelect }: EntitySelectorProps) {
  const [selectedType, setSelectedType] = useState<EntityType>(null);

  // Fetch user's entities
  const { data: guilds, isLoading: guildsLoading } = api.guild.getMyGuilds.useQuery(undefined, {
    enabled: selectedType === "guild",
  });

  const { data: projects, isLoading: projectsLoading } = api.project.getMyProjects.useQuery(
    { limit: 100, page: 1 },
    { enabled: selectedType === "project" }
  );

  const { data: shops, isLoading: shopsLoading } = api.shop.getMyShops.useQuery(undefined, {
    enabled: selectedType === "shop",
  });

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
          <h3 className="text-lg font-bold mb-2">Select Entity Type</h3>
          <p className="text-sm text-muted-foreground">
            Choose which entity will be posting this opportunity
          </p>
        </div>

        <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
          {(Object.entries(entityConfig) as [keyof typeof entityConfig, typeof entityConfig[keyof typeof entityConfig]][]).map(([type, config]) => {
            const Icon = config.icon;
            return (
              <Card
                key={type}
                variant="interactive"
                className="cursor-pointer"
                onClick={() => handleTypeSelect(type)}
              >
                <CardHeader className="text-center">
                  <div className="mx-auto mb-3 flex size-12 items-center justify-center rounded-lg border-2 border-border bg-card">
                    <Icon className="size-6 text-primary" />
                  </div>
                  <CardTitle className="text-base">{config.label}</CardTitle>
                </CardHeader>
                <CardContent className="text-center">
                  <p className="text-sm text-muted-foreground">{config.description}</p>
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
  const Icon = config.icon;

  const isLoading = selectedType === "guild"
    ? guildsLoading
    : selectedType === "project"
    ? projectsLoading
    : shopsLoading;

  const entities = selectedType === "guild"
    ? guilds
    : selectedType === "project"
    ? projects?.items
    : shops;

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3">
        <Button
          variant="ghost"
          size="icon-sm"
          onClick={() => setSelectedType(null)}
        >
          <ArrowLeft className="size-4" />
        </Button>
        <div>
          <h3 className="text-lg font-bold">Select {config.label}</h3>
          <p className="text-sm text-muted-foreground">
            Choose from your {config.label.toLowerCase()}s
          </p>
        </div>
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="size-8 animate-spin text-primary" />
        </div>
      ) : !entities || entities.length === 0 ? (
        <Card variant="outlined" className="p-8 text-center">
          <Icon className="mx-auto mb-4 size-12 text-muted-foreground" />
          <h4 className="mb-2 text-lg font-semibold">No {config.label}s Found</h4>
          <p className="mb-4 text-sm text-muted-foreground">
            You don't have any {config.label.toLowerCase()}s yet. Create one to post opportunities.
          </p>
          <Button variant="neon" asChild>
            <a href={`/${selectedType}s/create`}>Create {config.label}</a>
          </Button>
        </Card>
      ) : (
        <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
          {entities.map((entity: any) => (
            <Card
              key={entity.id}
              variant="interactive"
              className="cursor-pointer p-4"
              onClick={() => handleEntitySelect(entity.id, entity.slug)}
            >
              <div className="flex items-center gap-3">
                <Avatar className="size-12">
                  <AvatarImage src={entity.logo ?? undefined} alt={entity.name} />
                  <AvatarFallback>
                    <Icon className="size-5" />
                  </AvatarFallback>
                </Avatar>
                <div className="flex-1 min-w-0">
                  <h4 className="font-semibold truncate">{entity.name ?? entity.title}</h4>
                  {entity.description && (
                    <p className="text-sm text-muted-foreground line-clamp-1">
                      {entity.description}
                    </p>
                  )}
                  {entity.category && (
                    <Badge variant="outline" size="sm" className="mt-1">
                      {entity.category}
                    </Badge>
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
