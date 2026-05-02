import { z } from 'zod';

export const OpportunityApplicationScalarFieldEnumSchema = z.enum(['id','opportunityId','applicantId','coverLetter','portfolio','additionalInfo','status','reviewNotes','appliedAt','reviewedAt']);

export default OpportunityApplicationScalarFieldEnumSchema;
