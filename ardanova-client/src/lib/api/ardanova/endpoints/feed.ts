import { apiPath, query } from "../../path";
import { type BaseApiClient, type ApiResponse } from "../../base-client";
import type {
  FeedCard,
  FeedPage,
  FeedPostTypeCode,
  FeedScope,
} from "~/lib/contracts/feed-contract";

export type { FeedCard, FeedPage, FeedPostTypeCode, FeedScope };

export interface FeedPageParams {
  /** The previous page's `nextCursor`; omit for the first page. Opaque: never build or edit one. */
  cursor?: string | null;
  limit?: number;
  scope?: FeedScope;
}

/**
 * The ranked feed. The viewer is the signed actor the BFF attaches (or nobody: PUBLIC only), never a
 * parameter; every page is re-filtered server-side, so a cursor can only skip, never reveal.
 */
export class FeedEndpoint {
  constructor(private client: BaseApiClient) {}

  getPage(params: FeedPageParams = {}): Promise<ApiResponse<FeedPage>> {
    const q = query({
      cursor: params.cursor ?? undefined,
      limit: params.limit,
      scope: params.scope,
    });
    return this.client.get<FeedPage>(apiPath`/api/feed${q}`);
  }
}
