import { z } from 'zod';

export const UserExperienceScalarFieldEnumSchema = z.enum(['id','userId','title','company','description','startDate','endDate','isCurrent']);

export default UserExperienceScalarFieldEnumSchema;
