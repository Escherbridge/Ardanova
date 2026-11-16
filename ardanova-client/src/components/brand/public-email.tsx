/** Public operator mailbox. A constant, not caller input. */
export const PUBLIC_OPERATOR_EMAIL = "contact@escherbridge.com";

/**
 * Operator mailto.
 *
 * JSX comments never reach the response, so the markers are raw HTML and
 * they surround the whole anchor: Cloudflare rewrites a mailto href when it
 * reads the start tag. `email_on` is the closer this change emits;
 * `/email_off` is the closer in Cloudflare's email obfuscation docs.
 */
export function publicEmailAnchorHtml(className = "underline"): string {
  return `<!--email_off--><a class="${className}" href="mailto:${PUBLIC_OPERATOR_EMAIL}">${PUBLIC_OPERATOR_EMAIL}</a><!--email_on--><!--/email_off-->`;
}

const PUBLIC_EMAIL_HTML = publicEmailAnchorHtml();

export function PublicEmailLine() {
  return <p dangerouslySetInnerHTML={{ __html: PUBLIC_EMAIL_HTML }} />;
}

/** Inline mailto. The Cloudflare markers stay on the anchor, as in `PublicEmailLine`. */
export function PublicEmailAnchor({ className }: { className?: string }) {
  return (
    <span
      dangerouslySetInnerHTML={{ __html: publicEmailAnchorHtml(className) }}
    />
  );
}
