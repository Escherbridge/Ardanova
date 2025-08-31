import { notFound } from "next/navigation";
import { api } from "~/trpc/server";
import { ProjectDetail } from "~/components/project-detail";

interface ProjectPageProps {
  params: {
    slug: string;
  };
}

export default async function ProjectPage({ params }: ProjectPageProps) {
  try {
    const project = await api.project.getById({ id: params.slug });
    
    if (!project) {
      notFound();
    }

    return <ProjectDetail project={project} />;
  } catch (error) {
    console.error("Error fetching project:", error);
    notFound();
  }
}
