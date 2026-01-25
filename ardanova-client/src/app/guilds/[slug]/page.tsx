import { notFound } from "next/navigation";
import Link from "next/link";
import {
  ArrowLeft,
  Star,
  Users,
  Briefcase,
  CheckCircle,
  Globe,
  Mail,
  Phone,
  MapPin,
  Calendar,
  Edit,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "~/components/ui/tabs";
import { auth } from "~/server/auth";
import { api } from "~/trpc/server";

interface GuildPageProps {
  params: Promise<{
    slug: string;
  }>;
}

export default async function GuildPage({ params }: GuildPageProps) {
  const session = await auth();
  const { slug } = await params;

  let guild;
  let members;
  let reviews;

  try {
    guild = await api.guild.getBySlug({ slug });

    // Fetch additional data
    [members, reviews] = await Promise.all([
      api.guild.getMembers({ guildId: guild.id }),
      api.guild.getReviews({ guildId: guild.id }),
    ]);
  } catch (error) {
    console.error("Error fetching guild:", error);
    notFound();
  }

  if (!guild) {
    notFound();
  }

  const isOwner = session?.user?.id === guild.ownerId;

  const formatDate = (date: string) => {
    return new Date(date).toLocaleDateString("en-US", {
      month: "long",
      day: "numeric",
      year: "numeric",
    });
  };

  const renderRating = (rating: number | null) => {
    if (!rating) return <span className="text-slate-500">No ratings yet</span>;
    return (
      <div className="flex items-center gap-2">
        <div className="flex">
          {[1, 2, 3, 4, 5].map((star) => (
            <Star
              key={star}
              className={`h-5 w-5 ${
                star <= rating
                  ? "fill-yellow-400 text-yellow-400"
                  : "text-slate-300"
              }`}
            />
          ))}
        </div>
        <span className="font-semibold">{rating.toFixed(1)}</span>
        <span className="text-slate-500">({reviews.length} reviews)</span>
      </div>
    );
  };

  const getSpecialtiesList = (specialties: string | null) => {
    if (!specialties) return [];
    return specialties.split(",").map((s) => s.trim());
  };

  return (
    <div className="min-h-screen bg-slate-50">
      <div className="container mx-auto px-4 py-8">
        {/* Back Button */}
        <div className="mb-6">
          <Button variant="ghost" asChild>
            <Link href="/guilds">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Guilds
            </Link>
          </Button>
        </div>

        {/* Guild Header */}
        <div className="bg-white rounded-lg border p-6 mb-8">
          <div className="flex flex-col md:flex-row gap-6">
            {/* Logo */}
            <div className="flex-shrink-0">
              {guild.logo ? (
                <img
                  src={guild.logo}
                  alt={guild.name}
                  className="w-24 h-24 rounded-lg object-cover"
                />
              ) : (
                <div className="w-24 h-24 bg-slate-200 rounded-lg flex items-center justify-center">
                  <Briefcase className="h-12 w-12 text-slate-400" />
                </div>
              )}
            </div>

            {/* Guild Info */}
            <div className="flex-1">
              <div className="flex items-start justify-between">
                <div>
                  <h1 className="text-3xl font-bold text-slate-900 flex items-center gap-2">
                    {guild.name}
                    {guild.isVerified && (
                      <CheckCircle className="h-6 w-6 text-green-500" />
                    )}
                  </h1>
                  <div className="mt-2">{renderRating(guild.rating)}</div>
                </div>

                {isOwner && (
                  <Button variant="outline" asChild>
                    <Link href={`/guilds/${slug}/edit`}>
                      <Edit className="h-4 w-4 mr-2" />
                      Edit Guild
                    </Link>
                  </Button>
                )}
              </div>

              <p className="text-slate-600 mt-4">{guild.description}</p>

              {/* Specialties */}
              {guild.specialties && (
                <div className="flex flex-wrap gap-2 mt-4">
                  {getSpecialtiesList(guild.specialties).map((specialty, index) => (
                    <Badge key={index} variant="secondary">
                      {specialty}
                    </Badge>
                  ))}
                </div>
              )}

              {/* Stats */}
              <div className="flex flex-wrap gap-6 mt-6 text-sm text-slate-600">
                <div className="flex items-center gap-1">
                  <Briefcase className="h-4 w-4" />
                  <span className="font-semibold">{guild.projectsCount}</span> projects
                  completed
                </div>
                <div className="flex items-center gap-1">
                  <Users className="h-4 w-4" />
                  <span className="font-semibold">{members.length}</span> members
                </div>
                <div className="flex items-center gap-1">
                  <Calendar className="h-4 w-4" />
                  Member since {formatDate(guild.createdAt)}
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Content Tabs */}
        <Tabs defaultValue="about" className="space-y-6">
          <TabsList>
            <TabsTrigger value="about">About</TabsTrigger>
            <TabsTrigger value="portfolio">Portfolio</TabsTrigger>
            <TabsTrigger value="members">Members ({members.length})</TabsTrigger>
            <TabsTrigger value="reviews">Reviews ({reviews.length})</TabsTrigger>
          </TabsList>

          {/* About Tab */}
          <TabsContent value="about">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              {/* Contact Info */}
              <Card>
                <CardHeader>
                  <CardTitle className="text-lg">Contact Information</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="flex items-center gap-3">
                    <Mail className="h-4 w-4 text-slate-400" />
                    <a
                      href={`mailto:${guild.email}`}
                      className="text-blue-600 hover:underline"
                    >
                      {guild.email}
                    </a>
                  </div>

                  {guild.phone && (
                    <div className="flex items-center gap-3">
                      <Phone className="h-4 w-4 text-slate-400" />
                      <span>{guild.phone}</span>
                    </div>
                  )}

                  {guild.website && (
                    <div className="flex items-center gap-3">
                      <Globe className="h-4 w-4 text-slate-400" />
                      <a
                        href={guild.website}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="text-blue-600 hover:underline"
                      >
                        {guild.website}
                      </a>
                    </div>
                  )}

                  {guild.address && (
                    <div className="flex items-start gap-3">
                      <MapPin className="h-4 w-4 text-slate-400 mt-0.5" />
                      <span>{guild.address}</span>
                    </div>
                  )}
                </CardContent>
              </Card>

              {/* Quick Actions */}
              <Card className="md:col-span-2">
                <CardHeader>
                  <CardTitle className="text-lg">Work With Us</CardTitle>
                  <CardDescription>
                    Interested in working with {guild.name}? Get in touch!
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="flex flex-wrap gap-3">
                    <Button>
                      <Mail className="h-4 w-4 mr-2" />
                      Send Message
                    </Button>
                    <Button variant="outline">Request Quote</Button>
                    {session && !isOwner && (
                      <Button variant="outline">Submit a Review</Button>
                    )}
                  </div>
                </CardContent>
              </Card>
            </div>
          </TabsContent>

          {/* Portfolio Tab */}
          <TabsContent value="portfolio">
            <Card>
              <CardHeader>
                <CardTitle>Portfolio</CardTitle>
                <CardDescription>
                  View past work and completed projects
                </CardDescription>
              </CardHeader>
              <CardContent>
                {guild.portfolio ? (
                  <div className="prose max-w-none">
                    <p>{guild.portfolio}</p>
                  </div>
                ) : (
                  <p className="text-slate-500 text-center py-8">
                    No portfolio items added yet.
                  </p>
                )}
              </CardContent>
            </Card>
          </TabsContent>

          {/* Members Tab */}
          <TabsContent value="members">
            <Card>
              <CardHeader>
                <CardTitle>Team Members</CardTitle>
                <CardDescription>
                  Meet the professionals behind {guild.name}
                </CardDescription>
              </CardHeader>
              <CardContent>
                {members.length === 0 ? (
                  <p className="text-slate-500 text-center py-8">
                    No team members listed yet.
                  </p>
                ) : (
                  <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                    {members.map((member) => (
                      <div
                        key={member.id}
                        className="flex items-center gap-3 p-4 rounded-lg border"
                      >
                        <div className="w-10 h-10 bg-slate-200 rounded-full flex items-center justify-center">
                          <Users className="h-5 w-5 text-slate-400" />
                        </div>
                        <div>
                          <div className="font-medium">Member</div>
                          <div className="text-sm text-slate-500">{member.role}</div>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </CardContent>
            </Card>
          </TabsContent>

          {/* Reviews Tab */}
          <TabsContent value="reviews">
            <Card>
              <CardHeader>
                <CardTitle>Client Reviews</CardTitle>
                <CardDescription>
                  What clients say about working with {guild.name}
                </CardDescription>
              </CardHeader>
              <CardContent>
                {reviews.length === 0 ? (
                  <p className="text-slate-500 text-center py-8">
                    No reviews yet. Be the first to leave a review!
                  </p>
                ) : (
                  <div className="space-y-6">
                    {reviews.map((review) => (
                      <div key={review.id} className="border-b pb-6 last:border-b-0">
                        <div className="flex items-center gap-2 mb-2">
                          <div className="flex">
                            {[1, 2, 3, 4, 5].map((star) => (
                              <Star
                                key={star}
                                className={`h-4 w-4 ${
                                  star <= review.rating
                                    ? "fill-yellow-400 text-yellow-400"
                                    : "text-slate-300"
                                }`}
                              />
                            ))}
                          </div>
                          <span className="text-sm text-slate-500">
                            {formatDate(review.createdAt)}
                          </span>
                        </div>
                        {review.comment && (
                          <p className="text-slate-700">{review.comment}</p>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  );
}
