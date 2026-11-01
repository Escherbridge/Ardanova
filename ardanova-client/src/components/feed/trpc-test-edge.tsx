import type { ReactNode } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render } from "@testing-library/react";
import { TRPCClientError, type TRPCLink } from "@trpc/client";
import { observable } from "@trpc/server/observable";

import type { AppRouter } from "~/server/api/root";
import { api } from "~/trpc/react";

export type EdgeHandler = (input: unknown) => unknown;
export interface EdgeCall {
  path: string;
  input: unknown;
}

/**
 * Test-only: renders `ui` under a real tRPC client whose single terminating link answers from `handlers`.
 * The hooks, React Query and the tRPC client all run; only the HTTP edge is fake. A handler that throws
 * becomes a TRPCClientError, exactly as a failed request would. `latencyMs` > 0 lets React commit the
 * in-flight state between request and answer, as a real network does; 0 answers within the same tick.
 */
export function renderWithTrpcEdge(
  ui: ReactNode,
  handlers: Record<string, EdgeHandler>,
  { latencyMs = 0 }: { latencyMs?: number } = {},
) {
  const calls: EdgeCall[] = [];
  const edge: TRPCLink<AppRouter> =
    () =>
    ({ op }) =>
      observable((observer) => {
        calls.push({ path: op.path, input: op.input });
        const answered =
          latencyMs > 0
            ? new Promise<void>((resolve) => setTimeout(resolve, latencyMs))
            : Promise.resolve();
        answered
          .then(() => {
            const handler = handlers[op.path];
            if (!handler) throw new Error(`No fake for ${op.path}`);
            return handler(op.input);
          })
          .then((data) => {
            observer.next({ result: { type: "data", data } });
            observer.complete();
          })
          .catch((cause: unknown) => {
            observer.error(
              TRPCClientError.from<AppRouter>(
                cause instanceof Error ? cause : new Error(String(cause)),
              ),
            );
          });
      });

  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const client = api.createClient({ links: [edge] });
  const view = render(
    <QueryClientProvider client={queryClient}>
      <api.Provider client={client} queryClient={queryClient}>
        {ui}
      </api.Provider>
    </QueryClientProvider>,
  );
  return { ...view, calls };
}
