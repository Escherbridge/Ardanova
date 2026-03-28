import { z } from 'zod';

export const ProjectDurationSchema = z.enum(['ONE_TWO_WEEKS','ONE_THREE_MONTHS','THREE_SIX_MONTHS','SIX_TWELVE_MONTHS','ONE_TWO_YEARS','TWO_PLUS_YEARS','ONGOING']);

export type ProjectDurationType = `${z.infer<typeof ProjectDurationSchema>}`

export default ProjectDurationSchema;
