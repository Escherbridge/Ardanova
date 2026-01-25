import { z } from 'zod';

export const GuildScalarFieldEnumSchema = z.enum(['id','name','slug','description','website','email','phone','address','logo','portfolio','specialties','isVerified','rating','reviewsCount','projectsCount','createdAt','updatedAt','ownerId']);

export default GuildScalarFieldEnumSchema;
