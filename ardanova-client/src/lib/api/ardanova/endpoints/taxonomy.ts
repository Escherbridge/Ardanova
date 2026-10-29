import { apiPath, query } from "../../path";
import { type BaseApiClient, type ApiResponse } from "../../base-client";
import type {
  TaxonomyKind,
  TaxonomyKindCode,
  TaxonomyTermDto as TaxonomyTermContract,
} from "~/lib/contracts/taxonomy-contract";

export type TaxonomyTerm = TaxonomyTermContract;
export type { TaxonomyKind, TaxonomyKindCode };

export class TaxonomyEndpoint {
  constructor(private client: BaseApiClient) {}

  /** All active taxonomy terms, optionally filtered to one kind. */
  getAll(kind?: TaxonomyKindCode): Promise<ApiResponse<TaxonomyTerm[]>> {
    const q = query({ kind });
    return this.client.get<TaxonomyTerm[]>(apiPath`/api/taxonomy${q}`);
  }
}
