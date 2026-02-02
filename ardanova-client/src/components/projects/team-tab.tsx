"use client";

import { useState } from "react";
import { api } from "~/trpc/react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Loader2, Users, Plus, Check, X } from "lucide-react";

interface TeamTabProps {
  projectId: string;
  isOwner: boolean;
}

type MemberRole = "FOUNDER" | "LEADER" | "CORE_CONTRIBUTOR" | "CONTRIBUTOR" | "OBSERVER";
type ApplicationStatus = "PENDING" | "ACCEPTED" | "REJECTED" | "WITHDRAWN";

interface Member {
  id: string;
  userId: string;
  projectId: string;
  role: MemberRole;
  joinedAt: string;
  user?: {
    id: string;
    name?: string | null;
    email?: string | null;
    image?: string | null;
  };
}

interface Application {
  id: string;
  userId: string;
  projectId: string;
  roleTitle: string;
  message: string;
  skills?: string;
  experience?: string;
  availability?: string;
  status: ApplicationStatus;
  submittedAt: string;
  reviewedAt?: string;
  reviewMessage?: string;
  user?: {
    id: string;
    name?: string | null;
    email?: string | null;
    image?: string | null;
  };
}

const getRoleBadgeVariant = (role: MemberRole) => {
  switch (role) {
    case "FOUNDER":
      return "neon-pink-solid" as const;
    case "LEADER":
      return "neon-purple" as const;
    case "CORE_CONTRIBUTOR":
      return "neon-green" as const;
    case "CONTRIBUTOR":
      return "info" as const;
    case "OBSERVER":
      return "outline" as const;
    default:
      return "default" as const;
  }
};

const formatRoleName = (role: MemberRole) => {
  return role
    .split("_")
    .map((word) => word.charAt(0) + word.slice(1).toLowerCase())
    .join(" ");
};

// Fallback roles if no opportunities have been created for this project
const DEFAULT_ROLES = [
  { id: "FOUNDER", name: "Founder", description: "Project creator with full administrative access" },
  { id: "LEADER", name: "Leader", description: "Team lead with management responsibilities" },
  { id: "CORE_CONTRIBUTOR", name: "Core Contributor", description: "Key contributor with significant ongoing involvement" },
  { id: "CONTRIBUTOR", name: "Contributor", description: "Active contributor to the project" },
  { id: "OBSERVER", name: "Observer", description: "Following the project with limited participation" },
] as const;

export default function TeamTab({ projectId, isOwner }: TeamTabProps) {
  const [showApplicationForm, setShowApplicationForm] = useState(false);
  const [roleTitle, setRoleTitle] = useState("");
  const [message, setMessage] = useState("");
  const [skills, setSkills] = useState("");
  const [experience, setExperience] = useState("");
  const [availability, setAvailability] = useState("");

  const utils = api.useUtils();

  // Queries
  const { data: members, isLoading: membersLoading } = api.project.getMembers.useQuery({
    projectId,
  });

  const { data: opportunities } = api.opportunity.getByProjectId.useQuery({
    projectId,
  });

  const { data: applications, isLoading: applicationsLoading } = api.project.getApplications.useQuery(
    { projectId },
    { enabled: isOwner }
  );

  // Mutations
  const applyMutation = api.project.applyToProject.useMutation({
    onSuccess: () => {
      setShowApplicationForm(false);
      setRoleTitle("");
      setMessage("");
      setSkills("");
      setExperience("");
      setAvailability("");
    },
    onError: (error) => {
      alert(error.message);
    },
  });

  const reviewMutation = api.project.reviewApplication.useMutation({
    onMutate: async (variables) => {
      if (!variables) return;
      const { applicationId, status } = variables;
      await utils.project.getApplications.cancel({ projectId });
      const previous = utils.project.getApplications.getData({ projectId });
      utils.project.getApplications.setData({ projectId }, (old) =>
        old?.map((app) => (app.id === applicationId ? { ...app, status } : app))
      );
      return { previous };
    },
    onError: (err, _vars, context) => {
      if (context?.previous) {
        utils.project.getApplications.setData({ projectId }, context.previous);
      }
      alert(err.message);
    },
    onSettled: () => {
      void utils.project.getApplications.invalidate({ projectId });
      void utils.project.getMembers.invalidate({ projectId });
    },
  });

  const handleApply = (e: React.FormEvent) => {
    e.preventDefault();
    applyMutation.mutate({
      projectId,
      roleTitle,
      message,
      skills: skills || undefined,
      experience: experience || undefined,
      availability: availability || undefined,
    });
  };

  const handleReview = (applicationId: string, status: ApplicationStatus) => {
    reviewMutation.mutate({
      applicationId,
      status,
    });
  };

  if (membersLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="size-6 animate-spin text-muted-foreground" />
      </div>
    );
  }

  const pendingApplications = applications?.filter((app) => app.status === "PENDING") ?? [];

  return (
    <div className="space-y-6">
      {/* Team Members */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <Users className="size-5" />
                Team Members
              </CardTitle>
              <CardDescription>Current members working on this project</CardDescription>
            </div>
            {!isOwner && (
              <Button
                onClick={() => setShowApplicationForm(!showApplicationForm)}
                variant="default"
                size="sm"
              >
                <Plus className="size-4 mr-2" />
                Apply to Join
              </Button>
            )}
          </div>
        </CardHeader>
        <CardContent>
          {!members || members.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              No team members yet. Be the first to join!
            </div>
          ) : (
            <div className="space-y-3">
              {members.map((member: any) => (
                <div
                  key={member.id}
                  className="flex items-center justify-between p-4 border border-border rounded hover:bg-muted/50 transition-colors"
                >
                  <div className="flex items-center gap-3">
                    <Avatar>
                      <AvatarImage src={member.user?.image ?? undefined} />
                      <AvatarFallback>
                        {member.user?.name?.charAt(0).toUpperCase() ?? "U"}
                      </AvatarFallback>
                    </Avatar>
                    <div>
                      <div className="font-medium">
                        {member.user?.name ?? member.user?.email ?? "Unknown User"}
                      </div>
                      <div className="text-xs text-muted-foreground">
                        Joined {new Date(member.joinedAt).toLocaleDateString()}
                      </div>
                    </div>
                  </div>
                  <Badge variant={getRoleBadgeVariant(member.role)}>
                    {formatRoleName(member.role)}
                  </Badge>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Available Roles */}
      <Card>
        <CardHeader>
          <CardTitle>Available Roles</CardTitle>
          <CardDescription>Team positions and their current status</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-3">
            {opportunities && opportunities.length > 0 ? (
              // Show roles from project creation (stored as opportunities)
              opportunities.map((opp: any) => {
                const isFilled = opp.status === "FILLED" || opp.status === "CLOSED";

                return (
                  <div
                    key={opp.id}
                    className="flex items-center justify-between p-3 border border-border rounded hover:bg-muted/30 transition-colors"
                  >
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <Badge variant="neon">
                          {opp.title}
                        </Badge>
                        {opp.type && (
                          <Badge variant="secondary" className="text-xs">
                            {opp.type}
                          </Badge>
                        )}
                        {opp.applicationsCount > 0 && (
                          <span className="text-xs text-muted-foreground">
                            ({opp.applicationsCount} {opp.applicationsCount === 1 ? 'applicant' : 'applicants'})
                          </span>
                        )}
                      </div>
                      <p className="text-sm text-muted-foreground mt-1">{opp.description}</p>
                    </div>
                    <div className="ml-4">
                      {isFilled ? (
                        <Badge variant="secondary" className="text-xs">Filled</Badge>
                      ) : (
                        <Badge variant="outline" className="text-xs">Open</Badge>
                      )}
                    </div>
                  </div>
                );
              })
            ) : (
              // Fallback: show default roles when no opportunities defined
              DEFAULT_ROLES.map((role) => {
                const filledMembers = members?.filter((m: any) => m.role === role.id) ?? [];
                const isFilled = filledMembers.length > 0;

                return (
                  <div
                    key={role.id}
                    className="flex items-center justify-between p-3 border border-border rounded hover:bg-muted/30 transition-colors"
                  >
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <Badge variant={getRoleBadgeVariant(role.id as MemberRole)}>
                          {role.name}
                        </Badge>
                        {isFilled && (
                          <span className="text-xs text-muted-foreground">
                            ({filledMembers.length} {filledMembers.length === 1 ? 'member' : 'members'})
                          </span>
                        )}
                      </div>
                      <p className="text-sm text-muted-foreground mt-1">{role.description}</p>
                    </div>
                    <div className="ml-4">
                      {isFilled ? (
                        <div className="flex -space-x-2">
                          {filledMembers.slice(0, 3).map((m: any) => (
                            <Avatar key={m.id} className="size-8 border-2 border-background">
                              <AvatarImage src={m.user?.image ?? undefined} />
                              <AvatarFallback className="text-xs">
                                {m.user?.name?.charAt(0).toUpperCase() ?? "U"}
                              </AvatarFallback>
                            </Avatar>
                          ))}
                          {filledMembers.length > 3 && (
                            <div className="size-8 rounded-full bg-muted border-2 border-background flex items-center justify-center text-xs font-medium">
                              +{filledMembers.length - 3}
                            </div>
                          )}
                        </div>
                      ) : (
                        <Badge variant="outline" className="text-xs">Open</Badge>
                      )}
                    </div>
                  </div>
                );
              })
            )}
          </div>
        </CardContent>
      </Card>

      {/* Application Form */}
      {showApplicationForm && !isOwner && (
        <Card>
          <CardHeader>
            <CardTitle>Apply to Join Project</CardTitle>
            <CardDescription>
              Tell the project owner why you'd like to join and what you can contribute
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleApply} className="space-y-4">
              <div>
                <label htmlFor="roleTitle" className="text-sm font-medium block mb-1.5">
                  Desired Role Title *
                </label>
                <input
                  id="roleTitle"
                  type="text"
                  value={roleTitle}
                  onChange={(e) => setRoleTitle(e.target.value)}
                  placeholder="e.g., Frontend Developer, Designer, Marketing Lead"
                  className="w-full px-3 py-2 border border-border rounded bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                  required
                />
              </div>

              <div>
                <label htmlFor="message" className="text-sm font-medium block mb-1.5">
                  Application Message * (min 20 characters)
                </label>
                <textarea
                  id="message"
                  value={message}
                  onChange={(e) => setMessage(e.target.value)}
                  placeholder="Explain why you want to join this project and what you can contribute..."
                  className="w-full px-3 py-2 border border-border rounded bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary min-h-[100px] resize-y"
                  required
                  minLength={20}
                />
              </div>

              <div>
                <label htmlFor="skills" className="text-sm font-medium block mb-1.5">
                  Skills (Optional)
                </label>
                <input
                  id="skills"
                  type="text"
                  value={skills}
                  onChange={(e) => setSkills(e.target.value)}
                  placeholder="e.g., React, TypeScript, UX Design"
                  className="w-full px-3 py-2 border border-border rounded bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                />
              </div>

              <div>
                <label htmlFor="experience" className="text-sm font-medium block mb-1.5">
                  Experience (Optional)
                </label>
                <textarea
                  id="experience"
                  value={experience}
                  onChange={(e) => setExperience(e.target.value)}
                  placeholder="Briefly describe your relevant experience..."
                  className="w-full px-3 py-2 border border-border rounded bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary min-h-[80px] resize-y"
                />
              </div>

              <div>
                <label htmlFor="availability" className="text-sm font-medium block mb-1.5">
                  Availability (Optional)
                </label>
                <input
                  id="availability"
                  type="text"
                  value={availability}
                  onChange={(e) => setAvailability(e.target.value)}
                  placeholder="e.g., 10 hours/week, Weekends, Full-time"
                  className="w-full px-3 py-2 border border-border rounded bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                />
              </div>

              <div className="flex gap-2 pt-2">
                <Button type="submit" disabled={applyMutation.isPending}>
                  {applyMutation.isPending ? (
                    <>
                      <Loader2 className="size-4 mr-2 animate-spin" />
                      Submitting...
                    </>
                  ) : (
                    "Submit Application"
                  )}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setShowApplicationForm(false)}
                >
                  Cancel
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      )}

      {/* Pending Applications (Owner Only) */}
      {isOwner && (
        <Card>
          <CardHeader>
            <CardTitle>Pending Applications</CardTitle>
            <CardDescription>Review applications from people who want to join your project</CardDescription>
          </CardHeader>
          <CardContent>
            {applicationsLoading ? (
              <div className="flex items-center justify-center py-8">
                <Loader2 className="size-6 animate-spin text-muted-foreground" />
              </div>
            ) : pendingApplications.length === 0 ? (
              <div className="text-center py-8 text-muted-foreground">
                No pending applications
              </div>
            ) : (
              <div className="space-y-4">
                {pendingApplications.map((application: any) => (
                  <div
                    key={application.id}
                    className="p-4 border border-border rounded space-y-3"
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex items-center gap-3">
                        <Avatar>
                          <AvatarImage src={application.user?.image ?? undefined} />
                          <AvatarFallback>
                            {application.user?.name?.charAt(0).toUpperCase() ?? "U"}
                          </AvatarFallback>
                        </Avatar>
                        <div>
                          <div className="font-medium">
                            {application.user?.name ?? application.user?.email ?? "Unknown User"}
                          </div>
                          <div className="text-sm text-muted-foreground">
                            Applied for: {application.roleTitle}
                          </div>
                        </div>
                      </div>
                      <Badge variant="warning">Pending</Badge>
                    </div>

                    <div className="space-y-2">
                      <div>
                        <div className="text-xs font-medium text-muted-foreground mb-1">
                          Message:
                        </div>
                        <div className="text-sm">{application.message}</div>
                      </div>

                      {application.skills && (
                        <div>
                          <div className="text-xs font-medium text-muted-foreground mb-1">
                            Skills:
                          </div>
                          <div className="text-sm">{application.skills}</div>
                        </div>
                      )}

                      {application.experience && (
                        <div>
                          <div className="text-xs font-medium text-muted-foreground mb-1">
                            Experience:
                          </div>
                          <div className="text-sm">{application.experience}</div>
                        </div>
                      )}

                      {application.availability && (
                        <div>
                          <div className="text-xs font-medium text-muted-foreground mb-1">
                            Availability:
                          </div>
                          <div className="text-sm">{application.availability}</div>
                        </div>
                      )}
                    </div>

                    <div className="flex gap-2 pt-2">
                      <Button
                        size="sm"
                        variant="default"
                        onClick={() => handleReview(application.id, "ACCEPTED")}
                        disabled={reviewMutation.isPending}
                      >
                        <Check className="size-4 mr-2" />
                        Accept
                      </Button>
                      <Button
                        size="sm"
                        variant="destructive"
                        onClick={() => handleReview(application.id, "REJECTED")}
                        disabled={reviewMutation.isPending}
                      >
                        <X className="size-4 mr-2" />
                        Reject
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
