/** Path/query building for .NET API URLs; see `src/lib/api/AGENTS.md` §Path safety. */

export type PathValue = string | number | boolean | URLSearchParams;

/** Thrown when a value cannot be placed in a URL path segment (empty, `.`, `..`). */
export class InvalidApiPathError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "InvalidApiPathError";
  }
}

/** Encodes one path segment; rejects values that would vanish or traverse. */
export function segment(value: string | number | boolean): string {
  const raw = String(value);
  if (raw === "" || raw === "." || raw === "..") {
    throw new InvalidApiPathError("Invalid path segment");
  }
  return encodeURIComponent(raw);
}

/** Builds a `URLSearchParams`, dropping `undefined`/`null`; arrays repeat the key. */
export function query(
  params: Record<string, string | number | boolean | null | undefined | ReadonlyArray<string | number | boolean>>,
): URLSearchParams {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null) continue;
    if (Array.isArray(value)) {
      for (const item of value as ReadonlyArray<string | number | boolean>) search.append(key, String(item));
    } else {
      search.append(key, String(value as string | number | boolean));
    }
  }
  return search;
}

/**
 * Tagged template for API paths: values before `?` become encoded segments, values after `?` become
 * encoded query values, and a `URLSearchParams` value is spliced in as a whole query string.
 */
export function apiPath(strings: TemplateStringsArray, ...values: PathValue[]): string {
  let out = strings[0] ?? "";
  values.forEach((value, index) => {
    const inQuery = out.includes("?");
    if (value instanceof URLSearchParams) {
      const search = value.toString();
      if (search) {
        if (!inQuery) out += "?";
        else if (!out.endsWith("?") && !out.endsWith("&")) out += "&";
        out += search;
      }
    } else if (inQuery) {
      out += encodeURIComponent(String(value));
    } else {
      out += segment(value);
    }
    out += strings[index + 1] ?? "";
  });
  return out;
}
