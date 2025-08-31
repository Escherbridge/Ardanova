import { redirect } from "next/navigation";
import { auth } from "~/server/auth";
import { api } from "~/trpc/server";
import { ProjectForm } from "~/components/project-form";

interface EditProjectPageProps {
  params: {
    id: string;
  };
}

export default async function EditProjectPage({ params }: EditProjectPageProps) {
  const session = await auth();

  if (!session) {
    redirect("/api/auth/signin");
  }

  // Fetch the project to edit
  const project = await api.project.getById({ id: params.id });

  // Check if user owns the project
  if (project.createdById !== session.user.id) {
    redirect("/dashboard");
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <ProjectForm 
        mode="edit" 
        project={project} 
      />
    </div>
  );
}
