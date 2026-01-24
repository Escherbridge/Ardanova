import { notFound } from "next/navigation";
import { api } from "~/trpc/server";
import { ProjectDetail } from "~/components/project-detail";

interface ProjectPageProps {
  params: Promise<{
    slug: string;
  }>;
}

export default async function ProjectPage({ params }: ProjectPageProps) {
  try {
    const { slug } = await params;
    const project = await api.project.getById({ id: slug });

    if (!project) {
      notFound();
    }

    return <ProjectDetail project={project as any} />;
  } catch (error) {
    console.error("Error fetching project:", error);
    notFound();
  }
}
