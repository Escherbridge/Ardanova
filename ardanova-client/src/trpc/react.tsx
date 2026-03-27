"use client";

import { QueryClientProvider, type QueryClient } from "@tanstack/react-query";
import { httpBatchStreamLink, loggerLink } from "@trpc/client";
import { createTRPCReact } from "@trpc/react-query";
import { type inferRouterInputs, type inferRouterOutputs } from "@trpc/server";
import { useState } from "react";
import SuperJSON from "superjson";

import { type AppRouter } from "~/server/api/root";
import { createQueryClient } from "./query-client";

let clientQueryClientSingleton: QueryClient | undefined = undefined;
const getQueryClient = () => {
  if (typeof window === "undefined") {
    // Server: always make a new query client
    return createQueryClient();
  }
  // Browser: use singleton pattern to keep the same query client
  clientQueryClientSingleton ??= createQueryClient();

  return clientQueryClientSingleton;
};

export const api = createTRPCReact<AppRouter>();

/**
 * Inference helper for inputs.
 *
 * @example type HelloInput = RouterInputs['example']['hello']
 */
export type RouterInputs = inferRouterInputs<AppRouter>;

/**
 * Inference helper for outputs.
 *
 * @example type HelloOutput = RouterOutputs['example']['hello']
 */
export type RouterOutputs = inferRouterOutputs<AppRouter>;

export function TRPCReactProvider(props: { children: React.ReactNode }) {
  const queryClient = getQueryClient();

  const [trpcClient] = useState(() =>
    api.createClient({
      links: [
        loggerLink({
          /**
           * In dev, logging every query/mutation spams the console and is often
           * shown as "errors" by React DevTools (`hook.js` / overrideMethod).
           * Set `NEXT_PUBLIC_TRPC_DEBUG=true` when you need tRPC traffic logs.
           */
          enabled: (op) => {
            if (op.direction === "down" && op.result instanceof Error) {
              return true;
            }
            return process.env.NEXT_PUBLIC_TRPC_DEBUG === "true";
          },
        }),
        httpBatchStreamLink({
          // eslint-disable-next-line @typescript-eslint/no-explicit-any
          transformer: SuperJSON as any,
          url: getBaseUrl() + "/api/trpc",
          headers: () => {
            const headers = new Headers();
            headers.set("x-trpc-source", "nextjs-react");
            return headers;
          },
        }),
      ],
    }),
  );

  return (
    <QueryClientProvider client={queryClient}>
      <api.Provider client={trpcClient} queryClient={queryClient}>
        {props.children}
      </api.Provider>
    </QueryClientProvider>
  );
}

function getBaseUrl() {
  if (typeof window !== "undefined") return window.location.origin;
  // Production
  if (process.env.NODE_ENV === "production") return "https://www.ardanova.com";
  // Use AUTH_URL if set (for staging/preview)
  if (process.env.AUTH_URL) return process.env.AUTH_URL;
  // Local development
  return `http://localhost:${process.env.PORT ?? 3000}`;
}
