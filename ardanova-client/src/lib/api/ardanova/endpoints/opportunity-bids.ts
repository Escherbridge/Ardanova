import { type BaseApiClient, type ApiResponse } from "../../base-client";

export type OpportunityBidStatus = "SUBMITTED" | "UNDER_REVIEW" | "ACCEPTED" | "REJECTED" | "WITHDRAWN" | "COMPLETED";

export interface OpportunityBid {
  id: string;
  opportunityId: string;
  bidderId: string;
  guildId?: string | null;
  proposedAmount?: number | null;
  proposal: string;
  estimatedHours?: number | null;
  timeline?: string | null;
  deliverables?: string | null;
  status: OpportunityBidStatus;
  createdAt: string;
  updatedAt: string;
  [key: string]: unknown;
}

export interface CreateOpportunityBid {
  opportunityId: string;
  guildId?: string | null;
  proposedAmount?: number;
  proposal: string;
  estimatedHours?: number;
  timeline?: string;
  deliverables?: string;
  [key: string]: unknown;
}

export interface UpdateOpportunityBid {
  proposedAmount?: number;
  proposal?: string;
  estimatedHours?: number;
  timeline?: string;
  deliverables?: string;
  [key: string]: unknown;
}

export class OpportunityBidsEndpoint {
  constructor(private client: BaseApiClient) {}

  getByOpportunityId(opportunityId: string): Promise<ApiResponse<OpportunityBid[]>> {
    return this.client.get<OpportunityBid[]>(`/api/opportunity-bids?opportunityId=${encodeURIComponent(opportunityId)}`);
  }

  getById(id: string): Promise<ApiResponse<OpportunityBid>> {
    return this.client.get<OpportunityBid>(`/api/opportunity-bids/${id}`);
  }

  getByBidderId(bidderId: string): Promise<ApiResponse<OpportunityBid[]>> {
    return this.client.get<OpportunityBid[]>(`/api/opportunity-bids/bidder/${bidderId}`);
  }

  getByGuildId(guildId: string): Promise<ApiResponse<OpportunityBid[]>> {
    return this.client.get<OpportunityBid[]>(`/api/opportunity-bids/guild/${guildId}`);
  }

  create(data: CreateOpportunityBid): Promise<ApiResponse<OpportunityBid>> {
    return this.client.post<OpportunityBid>("/api/opportunity-bids", data);
  }

  update(id: string, data: UpdateOpportunityBid): Promise<ApiResponse<OpportunityBid>> {
    return this.client.put<OpportunityBid>(`/api/opportunity-bids/${id}`, data);
  }

  accept(id: string): Promise<ApiResponse<OpportunityBid>> {
    return this.client.post<OpportunityBid>(`/api/opportunity-bids/${id}/accept`, {});
  }

  reject(id: string): Promise<ApiResponse<OpportunityBid>> {
    return this.client.post<OpportunityBid>(`/api/opportunity-bids/${id}/reject`, {});
  }

  withdraw(id: string): Promise<ApiResponse<OpportunityBid>> {
    return this.client.post<OpportunityBid>(`/api/opportunity-bids/${id}/withdraw`, {});
  }

  complete(id: string): Promise<ApiResponse<OpportunityBid>> {
    return this.client.post<OpportunityBid>(`/api/opportunity-bids/${id}/complete`, {});
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/opportunity-bids/${id}`);
  }
}
