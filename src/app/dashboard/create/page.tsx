import { ProjectForm } from "~/components/project-form";

export default function CreateProjectPage() {
  return (
    <div className="min-h-screen bg-gray-50">
      <ProjectForm mode="create" />
    </div>
  );
}
