import { z } from 'zod';

export const UserScalarFieldEnumSchema = z.enum(['id','email','emailVerified','name','image','bio','location','phone','website','linkedIn','twitter','role','userType','isVerified','totalXP','level','tier','trustScore','verificationLevel','createdAt','updatedAt']);

export default UserScalarFieldEnumSchema;
