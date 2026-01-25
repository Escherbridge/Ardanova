import Link from "next/link";
import { Search, Filter, Star, Users, Briefcase, CheckCircle, Plus } from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Input } from "~/components/ui/input";
import { auth } from "~/server/auth";
import { api } from "~/trpc/server";

export default async function GuildsPage() {
  const session = await auth();

  // Fetch guilds from API
  const guildsResult = await api.guild.getAll({
    limit: 50,
    page: 1,
  });

  const guilds = guildsResult.items;

  // Calculate stats
  const verifiedCount = guilds.filter((g) => g.isVerified).length;
  const totalMembers = guilds.reduce((sum, g) => sum + (g.reviewsCount || 0), 0); // Placeholder
  const totalProjects = guilds.reduce((sum, g) => sum + (g.projectsCount || 0), 0);

  const renderRating = (rating: number | null) => {
    if (!rating) return null;
    return (
      <div className="flex items-center gap-1">
        <Star className="h-4 w-4 fill-yellow-400 text-yellow-400" />
        <span className="font-medium">{rating.toFixed(1)}</span>
        <span className="text-slate-500">/ 5</span>
      </div>
    );
  };

  const getSpecialtiesBadges = (specialties: string | null) => {
    if (!specialties) return [];
    return specialties.split(",").slice(0, 3).map((s) => s.trim());
  };

  return (
    <div className="min-h-screen bg-slate-50">
      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <h1 className="text-3xl font-bold text-slate-900 mb-2">Professional Guilds</h1>
            <p className="text-slate-600">
              Discover professional guilds ready to bring your projects to life
            </p>
          </div>
          {session && (
            <Button asChild>
              <Link href="/guilds/create">
                <Plus className="h-4 w-4 mr-2" />
                Create Guild
              </Link>
            </Button>
          )}
        </div>

        {/* Filters and Search */}
        <div className="mb-8">
          <div className="flex flex-col sm:flex-row gap-4 mb-6">
            <div className="flex-1">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-slate-400 h-4 w-4" />
                <Input placeholder="Search guilds..." className="pl-10" />
              </div>
            </div>
            <Button variant="outline">
              <CheckCircle className="h-4 w-4 mr-2" />
              Verified Only
            </Button>
            <Button variant="outline">
              <Filter className="h-4 w-4 mr-2" />
              More Filters
            </Button>
          </div>

          {/* Quick Stats */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
            <div className="text-center p-4 bg-white rounded-lg border">
              <div className="text-2xl font-bold text-slate-900">{guilds.length}</div>
              <div className="text-sm text-slate-600">Total Guilds</div>
            </div>
            <div className="text-center p-4 bg-white rounded-lg border">
              <div className="text-2xl font-bold text-green-600">{verifiedCount}</div>
              <div className="text-sm text-slate-600">Verified</div>
            </div>
            <div className="text-center p-4 bg-white rounded-lg border">
              <div className="text-2xl font-bold text-slate-900">{totalProjects}</div>
              <div className="text-sm text-slate-600">Projects Completed</div>
            </div>
            <div className="text-center p-4 bg-white rounded-lg border">
              <div className="text-2xl font-bold text-slate-900">{totalMembers}</div>
              <div className="text-sm text-slate-600">Total Reviews</div>
            </div>
          </div>
        </div>

        {/* Guilds Grid */}
        {guilds.length === 0 ? (
          <div className="text-center py-12">
            <Briefcase className="h-12 w-12 text-slate-400 mx-auto mb-4" />
            <h3 className="text-lg font-medium text-slate-900 mb-2">No guilds yet</h3>
            <p className="text-slate-600 mb-4">
              Be the first to create a professional guild on ArdaNova!
            </p>
            {session && (
              <Button asChild>
                <Link href="/guilds/create">
                  <Plus className="h-4 w-4 mr-2" />
                  Create Guild
                </Link>
              </Button>
            )}
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {guilds.map((guild) => (
              <Card
                key={guild.id}
                className="hover:shadow-lg transition-shadow cursor-pointer"
              >
                <CardHeader>
                  <div className="flex items-start justify-between mb-2">
                    <div className="flex items-center gap-2">
                      {guild.logo ? (
                        <img
                          src={guild.logo}
                          alt={guild.name}
                          className="w-12 h-12 rounded-full object-cover"
                        />
                      ) : (
                        <div className="w-12 h-12 bg-slate-200 rounded-full flex items-center justify-center">
                          <Briefcase className="h-6 w-6 text-slate-400" />
                        </div>
                      )}
                      <div>
                        <CardTitle className="text-lg flex items-center gap-2">
                          <Link
                            href={`/guilds/${guild.slug}`}
                            className="hover:text-blue-600 transition-colors"
                          >
                            {guild.name}
                          </Link>
                          {guild.isVerified && (
                            <CheckCircle className="h-4 w-4 text-green-500" />
                          )}
                        </CardTitle>
                        {guild.rating && renderRating(guild.rating)}
                      </div>
                    </div>
                  </div>
                  <CardDescription className="line-clamp-2">
                    {guild.description}
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    {/* Specialties */}
                    {guild.specialties && (
                      <div className="flex flex-wrap gap-1">
                        {getSpecialtiesBadges(guild.specialties).map(
                          (specialty, index) => (
                            <Badge key={index} variant="secondary" className="text-xs">
                              {specialty}
                            </Badge>
                          )
                        )}
                      </div>
                    )}

                    {/* Stats */}
                    <div className="flex items-center justify-between text-sm text-slate-600">
                      <div className="flex items-center gap-1">
                        <Briefcase className="h-4 w-4" />
                        {guild.projectsCount} projects
                      </div>
                      <div className="flex items-center gap-1">
                        <Users className="h-4 w-4" />
                        {guild.reviewsCount} reviews
                      </div>
                    </div>

                    {/* Contact Info */}
                    {guild.website && (
                      <div className="text-sm text-slate-500 truncate">
                        {guild.website}
                      </div>
                    )}

                    {/* Action Buttons */}
                    <div className="flex gap-2 pt-2">
                      <Button className="flex-1" size="sm" asChild>
                        <Link href={`/guilds/${guild.slug}`}>View Guild</Link>
                      </Button>
                      <Button variant="outline" size="sm">
                        Contact
                      </Button>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}

        {/* Load More */}
        {guilds.length > 0 && guildsResult.nextCursor && (
          <div className="text-center mt-12">
            <Button variant="outline" size="lg">
              Load More Guilds
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}
