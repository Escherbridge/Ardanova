import { type BaseApiClient, type ApiResponse } from "../../base-client";

// ============ Type Definitions ============

export type KycStatus = 'PENDING' | 'IN_REVIEW' | 'APPROVED' | 'REJECTED' | 'EXPIRED';

export type KycDocumentType = 'GOVERNMENT_ID' | 'PASSPORT' | 'DRIVERS_LICENSE' | 'SELFIE' | 'PROOF_OF_ADDRESS';

export type KycProvider = 'MANUAL' | 'VERIFF';

export interface KycDocument {
  id: string;
  submissionId: string;
  type: KycDocumentType;
  fileUrl: string;
  fileName: string;
  mimeType?: string;
  fileSizeBytes?: number;
  metadata?: string;
  createdAt: string;
}

export interface KycSubmission {
  id: string;
  userId: string;
  userName?: string;
  userEmail?: string;
  userImage?: string;
  provider: KycProvider;
  status: KycStatus;
  reviewerId?: string;
  reviewNotes?: string;
  rejectionReason?: string;
  providerSessionId?: string;
  submittedAt: string;
  reviewedAt?: string;
  expiresAt?: string;
  createdAt: string;
  updatedAt: string;
  documents: KycDocument[];
}

export interface SubmitKycDocumentDto {
  type: KycDocumentType;
  fileUrl: string;
  fileName: string;
  mimeType?: string;
  fileSizeBytes?: number;
  metadata?: string;
}

export interface SubmitKycDto {
  userId: string;
  documents: SubmitKycDocumentDto[];
}

export interface ReviewKycDto {
  reviewerId: string;
  reviewNotes?: string;
  rejectionReason?: string;
}

// ============ KYC Endpoint ============

export class KycEndpoint {
  constructor(private client: BaseApiClient) {}

  submit(data: SubmitKycDto): Promise<ApiResponse<KycSubmission>> {
    return this.client.post<KycSubmission>('/api/Kyc/submit', data);
  }

  getStatus(userId: string): Promise<ApiResponse<KycSubmission>> {
    return this.client.get<KycSubmission>(`/api/Kyc/status/${encodeURIComponent(userId)}`);
  }

  getById(id: string): Promise<ApiResponse<KycSubmission>> {
    return this.client.get<KycSubmission>(`/api/Kyc/${encodeURIComponent(id)}`);
  }

  getPending(): Promise<ApiResponse<KycSubmission[]>> {
    return this.client.get<KycSubmission[]>('/api/Kyc/pending');
  }

  approve(id: string, data: ReviewKycDto): Promise<ApiResponse<KycSubmission>> {
    return this.client.post<KycSubmission>(`/api/Kyc/${encodeURIComponent(id)}/approve`, data);
  }

  reject(id: string, data: ReviewKycDto): Promise<ApiResponse<KycSubmission>> {
    return this.client.post<KycSubmission>(`/api/Kyc/${encodeURIComponent(id)}/reject`, data);
  }
}
