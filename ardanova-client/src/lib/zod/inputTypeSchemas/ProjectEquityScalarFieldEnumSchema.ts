import { z } from 'zod';

export const ProjectEquityScalarFieldEnumSchema = z.enum(['id','projectId','userId','sharePercent','investmentAmount','grantedAt']);

export default ProjectEquityScalarFieldEnumSchema;
