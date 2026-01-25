import { z } from 'zod';

export const ProjectScalarFieldEnumSchema = z.enum(['id','title','slug','description','problemStatement','solution','category','status','fundingGoal','currentFunding','supportersCount','votesCount','viewsCount','featured','tags','images','videos','documents','targetAudience','expectedImpact','timeline','createdAt','updatedAt','publishedAt','fundedAt','completedAt','createdById','assignedGuildId']);

export default ProjectScalarFieldEnumSchema;
