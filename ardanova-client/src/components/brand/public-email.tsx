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
const PUBLIC_EMAIL_HTML = `<!--email_off--><a class="underline" href="mailto:${PUBLIC_OPERATOR_EMAIL}">${PUBLIC_OPERATOR_EMAIL}</a><!--email_on--><!--/email_off-->`;

export function PublicEmailLine() {
  return <p dangerouslySetInnerHTML={{ __html: PUBLIC_EMAIL_HTML }} />;
}
