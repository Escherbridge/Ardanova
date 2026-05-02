import { z } from 'zod';

export const ShareHolderScalarFieldEnumSchema = z.enum(['id','shareId','userId','balance','stakedAmount','lockedAmount','createdAt','updatedAt']);

export default ShareHolderScalarFieldEnumSchema;
