import {
  defaultShouldDehydrateQuery,
  QueryClient,
} from "@tanstack/react-query";
import SuperJSON from "superjson";

import { shouldRetryQuery } from "~/components/ui/query-error-copy";

export const createQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: {
        // With SSR, we usually want to set some default staleTime
        // above 0 to avoid refetching immediately on the client
        staleTime: 30 * 1000,
        // Terminal codes (auth/permission/not-found/validation) fail on request 1 instead of
        // after 3 backoff retries (~10.8s of "Loading…"). See `query-error-copy.ts` §shouldRetryQuery
        // and `src/components/ui/AGENTS.md` §query-error-state.
        retry: shouldRetryQuery,
      },
      dehydrate: {
        serializeData: SuperJSON.serialize,
        shouldDehydrateQuery: (query) =>
          defaultShouldDehydrateQuery(query) ||
          query.state.status === "pending",
      },
      hydrate: {
        deserializeData: SuperJSON.deserialize,
      },
    },
  });
