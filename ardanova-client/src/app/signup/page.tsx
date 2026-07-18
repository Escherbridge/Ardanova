import { redirect } from "next/navigation";
import {
  buildSignInHref,
  normalizeInternalCallbackUrl,
} from "~/lib/auth-navigation";

export default async function SignUpAliasPage({
  searchParams,
}: {
  searchParams: Promise<{ callbackUrl?: string | string[] }>;
}) {
  const { callbackUrl } = await searchParams;
  redirect(
    buildSignInHref(normalizeInternalCallbackUrl(callbackUrl), {
      mode: "signup",
    }),
  );
}
