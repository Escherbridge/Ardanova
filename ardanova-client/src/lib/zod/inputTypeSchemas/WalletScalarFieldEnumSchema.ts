import { z } from 'zod';

export const WalletScalarFieldEnumSchema = z.enum(['id','userId','address','provider','label','isVerified','isPrimary','createdAt','updatedAt']);

export default WalletScalarFieldEnumSchema;
