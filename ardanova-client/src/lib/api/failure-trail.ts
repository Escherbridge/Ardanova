import "server-only";

import { AsyncLocalStorage } from "node:async_hooks";

/** A non-2xx answer from the .NET API, as the BFF saw it. */
export type ApiFailure = { status: number; message: string };

const trailStorage = new AsyncLocalStorage<ApiFailure[]>();

/** Runs a BFF procedure while remembering every API failure it receives. See lib/api/AGENTS.md §Failure trail. */
export function runWithApiFailureTrail<T>(callback: () => T): T {
  return trailStorage.run([], callback);
}

/** Called by the base client for every non-2xx response; a no-op outside a trail. */
export function recordApiFailure(failure: ApiFailure): void {
  trailStorage.getStore()?.push(failure);
}

/** The most recent API failure whose message the procedure rethrew word for word. */
export function findApiFailure(message: string): ApiFailure | undefined {
  const failures = trailStorage.getStore() ?? [];
  for (let index = failures.length - 1; index >= 0; index -= 1) {
    if (failures[index]?.message === message) return failures[index];
  }
  return undefined;
}
