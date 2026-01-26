"use client";

import { useParams } from "next/navigation";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "~/components/ui/tabs";
import { Share2, Calendar, FolderKanban, Users, UserPlus } from "lucide-react";

// Sample user data - would come from API in production
const sampleUsers: Record<string, { name: string; avatar: string; bio: string; projects: number; guilds: number; joinedDate: string }> = {
  "u1": { name: "Sarah Chen", avatar: "https://i.pravatar.cc/150?u=sarah", bio: "Founder of EcoWaste Solutions. Passionate about sustainability.", projects: 5, guilds: 3, joinedDate: "Nov 2023" },
  "u2": { name: "Marcus Rodriguez", avatar: "https://i.pravatar.cc/150?u=marcus", bio: "Mobile developer. Building accessible apps for everyone.", projects: 2, guilds: 1, joinedDate: "Dec 2023" },
  "u3": { name: "Design Guild", avatar: "https://i.pravatar.cc/150?u=guild", bio: "Official Design Guild account.", projects: 0, guilds: 1, joinedDate: "Jan 2024" },
  "u4": { name: "Alex Kim", avatar: "https://i.pravatar.cc/150?u=alex", bio: "Strategy & Growth. Always thinking about the next big thing.", projects: 3, guilds: 2, joinedDate: "Oct 2023" },
  "u5": { name: "Jordan Lee", avatar: "https://i.pravatar.cc/150?u=jordan", bio: "EdTech enthusiast. Connecting students with mentors.", projects: 4, guilds: 2, joinedDate: "Sep 2023" },
  "u6": { name: "Emma Watson", avatar: "https://i.pravatar.cc/150?u=emma", bio: "UX Designer with a passion for accessible design.", projects: 12, guilds: 4, joinedDate: "Aug 2023" },
  "u7": { name: "David Park", avatar: "https://i.pravatar.cc/150?u=david", bio: "Full-stack developer. Building the future one commit at a time.", projects: 8, guilds: 3, joinedDate: "Jul 2023" },
  "u8": { name: "Lisa Chen", avatar: "https://i.pravatar.cc/150?u=lisa", bio: "Product Manager. Turning ideas into reality.", projects: 8, guilds: 5, joinedDate: "Jun 2023" },
};

export default function UserProfilePage() {
  const params = useParams();
  const userId = params.id as string;

  const userData = sampleUsers[userId] || {
    name: "Unknown User",
    avatar: "",
    bio: "This user hasn't set up their profile yet.",
    projects: 0,
    guilds: 0,
    joinedDate: "Unknown",
  };

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-4xl mx-auto p-6">
        {/* Profile Header */}
        <Card className="mb-6">
          <CardContent className="pt-6">
            <div className="flex items-start gap-6">
              <Avatar className="size-24 border-4 border-primary">
                <AvatarImage src={userData.avatar} alt={userData.name} />
                <AvatarFallback className="text-2xl">
                  {userData.name.charAt(0)}
                </AvatarFallback>
              </Avatar>

              <div className="flex-1">
                <div className="flex items-center gap-3 mb-2">
                  <h1 className="text-2xl font-bold text-foreground">
                    {userData.name}
                  </h1>
                  <Badge variant="neon">Member</Badge>
                </div>
                <p className="text-muted-foreground mb-4">
                  {userData.bio}
                </p>
                <div className="flex gap-6 text-sm">
                  <div className="flex items-center gap-2">
                    <FolderKanban className="size-4 text-primary" />
                    <span><strong>{userData.projects}</strong> Projects</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Users className="size-4 text-neon-pink" />
                    <span><strong>{userData.guilds}</strong> Guilds</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Calendar className="size-4 text-neon-green" />
                    <span>Joined {userData.joinedDate}</span>
                  </div>
                </div>
              </div>

              <div className="flex gap-2">
                <Button variant="outline" size="sm">
                  <Share2 className="size-4 mr-2" />
                  Share
                </Button>
                <Button variant="default" size="sm">
                  <UserPlus className="size-4 mr-2" />
                  Follow
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Profile Tabs */}
        <Tabs defaultValue="activity" className="w-full">
          <TabsList className="w-full justify-start">
            <TabsTrigger value="activity">Activity</TabsTrigger>
            <TabsTrigger value="projects">Projects</TabsTrigger>
            <TabsTrigger value="guilds">Guilds</TabsTrigger>
            <TabsTrigger value="contributions">Contributions</TabsTrigger>
          </TabsList>

          <TabsContent value="activity" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle>Recent Activity</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-muted-foreground">No recent activity to show.</p>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="projects" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle>Projects</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-muted-foreground">This user hasn't joined any projects yet.</p>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="guilds" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle>Guilds</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-muted-foreground">This user hasn't joined any guilds yet.</p>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="contributions" className="mt-6">
            <Card>
              <CardHeader>
                <CardTitle>Contributions</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-muted-foreground">No contributions to show.</p>
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  );
}
