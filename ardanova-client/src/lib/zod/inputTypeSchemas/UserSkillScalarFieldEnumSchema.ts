import { z } from 'zod';

export const UserSkillScalarFieldEnumSchema = z.enum(['id','userId','skill','level']);

export default UserSkillScalarFieldEnumSchema;
