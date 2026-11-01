import { fetchRequestHandler } from "@trpc/server/adapters/fetch";
import { type NextRequest } from "next/server";

import { env } from "~/env";
import { appRouter } from "~/server/api/root";
import { createTRPCContext } from "~/server/api/trpc";

/**
 * This wraps the `createTRPCContext` helper and provides the required context for the tRPC API when
 * handling a HTTP request (e.g. when you make requests from Client Components).
 */
const createContext = async (req: NextRequest) => {
  return createTRPCContext({
    headers: req.headers,
  });
};

/**
 * Caps the body a mutation request may spend before tRPC/zod ever look at it. See
 * SECURITY-DEPENDENCIES.md "unbounded array" entry: an oversized body is how a caller turns a
 * zod parse into a multi-second, multi-hundred-MB event-loop block. The largest legitimate
 * payload in this app (a project create/update with every long-text field near its cap) is well
 * under 250 KB; this leaves headroom for `httpBatchStreamLink` batching several calls into one
 * request while still rejecting the finding's 2 MB attack body before it is read into memory.
 */
const MAX_BODY_BYTES = 1 * 1024 * 1024; // 1 MiB

function requestTooLarge(): Response {
  return Response.json(
    { error: { message: "Request body too large" } },
    { status: 413 },
  );
}

/**
 * Reads a body stream up to `limitBytes`, aborting as soon as that many bytes have arrived.
 * Guards both an honest `Content-Length` over the cap (checked by the caller before this runs)
 * and a chunked/absent `Content-Length` that understates the real size — the stream itself, not
 * the header, is the source of truth here.
 */
async function readBodyWithinLimit(
  stream: ReadableStream<Uint8Array>,
  limitBytes: number,
): Promise<Uint8Array | null> {
  const reader = stream.getReader();
  const chunks: Uint8Array[] = [];
  let total = 0;

  for (;;) {
    const { done, value } = await reader.read();
    if (done) break;
    total += value.byteLength;
    if (total > limitBytes) {
      await reader.cancel();
      return null;
    }
    chunks.push(value);
  }

  const combined = new Uint8Array(total);
  let offset = 0;
  for (const chunk of chunks) {
    combined.set(chunk, offset);
    offset += chunk.byteLength;
  }
  return combined;
}

const runTrpc = (req: NextRequest, parsedReq: Request) =>
  fetchRequestHandler({
    endpoint: "/api/trpc",
    req: parsedReq,
    router: appRouter,
    createContext: () => createContext(req),
    onError:
      env.NODE_ENV === "development"
        ? ({ path, error }) => {
            console.error(
              `❌ tRPC failed on ${path ?? "<no-path>"}: ${error.message}`,
            );
          }
        : undefined,
  });

/**
 * GET carries its input in the query string (tRPC's batch-GET encoding), so there is no request
 * body to bound here; it is left to the platform's own URL-length limit.
 */
const handleGet = (req: NextRequest) => runTrpc(req, req);

async function handlePost(req: NextRequest): Promise<Response> {
  const declaredLength = req.headers.get("content-length");
  if (declaredLength !== null) {
    const parsed = Number(declaredLength);
    if (!Number.isFinite(parsed) || parsed > MAX_BODY_BYTES) {
      return requestTooLarge();
    }
  }

  if (!req.body) {
    // No body to drain; let tRPC's own parsing report whatever is actually wrong.
    return runTrpc(req, req);
  }

  // Re-checks the real byte count even when Content-Length was missing, chunked, or understated.
  const cappedBody = await readBodyWithinLimit(req.body, MAX_BODY_BYTES);
  if (cappedBody === null) {
    return requestTooLarge();
  }

  const reconstructed = new Request(req.url, {
    method: req.method,
    headers: req.headers,
    // `combined.buffer` is sized to exactly `combined.byteLength` (freshly allocated in
    // readBodyWithinLimit), so handing over the raw ArrayBuffer is safe here.
    body: cappedBody.buffer as ArrayBuffer,
  });
  return runTrpc(req, reconstructed);
}

export { handleGet as GET, handlePost as POST };
