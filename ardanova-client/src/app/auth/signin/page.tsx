import { ArrowLeft, ArrowRight, Check } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";

import { ArdaNovaWordmark } from "~/components/brand/wordmark";
import { Button } from "~/components/ui/button";
import {
  buildSignInHref,
  normalizeInternalCallbackUrl,
} from "~/lib/auth-navigation";
import { auth, signIn } from "~/server/auth";

export const metadata: Metadata = {
  title: "Sign in",
  description: "Sign in to your ArdaNova project workspace.",
};

interface SignInPageProps {
  searchParams: Promise<{
    mode?: string | string[];
    callbackUrl?: string | string[];
  }>;
}

export default async function SignInPage({ searchParams }: SignInPageProps) {
  const session = await auth();
  const { mode, callbackUrl: requestedCallbackUrl } = await searchParams;
  const isSignUp = mode === "signup";
  const callbackUrl = normalizeInternalCallbackUrl(requestedCallbackUrl);

  if (session) {
    redirect(callbackUrl);
  }

  return (
    <main
      className="min-h-screen bg-[#151513] text-[#f6f0eb] lg:grid lg:grid-cols-[minmax(0,1.1fr)_minmax(420px,0.9fr)]"
      id="signin-content"
    >
      <a className="skip-link" href="#signin-form">
        Skip to sign in
      </a>

      <section className="relative isolate hidden min-h-screen overflow-hidden border-r border-[#f6f0eb]/35 p-10 lg:flex lg:flex-col lg:justify-between xl:p-14">
        <div
          aria-hidden="true"
          className="absolute -top-20 -right-16 -z-10 size-[360px] rounded-full bg-[#ef4638] xl:size-[460px]"
        />
        <div
          aria-hidden="true"
          className="absolute top-[37%] right-[12%] -z-10 h-px w-[58%] rotate-[-17deg] bg-[#70d7e2]"
        />
        <div
          aria-hidden="true"
          className="absolute top-[40%] right-[18%] -z-10 size-5 border-4 border-[#70d7e2] bg-[#151513]"
        />

        <Link
          aria-label="ArdaNova home"
          className="inline-flex min-h-11 w-fit items-center"
          href="/"
        >
          <ArdaNovaWordmark className="text-lg" />
        </Link>

        <div className="max-w-3xl">
          <p className="mb-6 font-mono text-xs font-semibold tracking-[0.18em] text-[#70d7e2] uppercase">
            Your shared operating view
          </p>
          <h1 className="text-7xl leading-[0.84] font-black tracking-[-0.07em] uppercase xl:text-8xl">
            Bring the project back into focus.
          </h1>
          <p className="mt-8 max-w-xl text-lg leading-relaxed text-[#c9c3bd]">
            Return to the plans, decisions, contributions, and review states
            that keep shared work understandable.
          </p>
        </div>

        <div className="grid grid-cols-4 border border-[#f6f0eb]/35 font-mono text-[10px] font-semibold tracking-[0.12em] uppercase">
          {["Draft", "Review", "Agree", "Record"].map((step, index) => (
            <div
              className="flex min-h-20 flex-col justify-between border-r border-[#f6f0eb]/35 p-3 last:border-r-0"
              key={step}
            >
              <span
                className={index === 1 ? "text-[#70d7e2]" : "text-[#aaa59f]"}
              >
                0{index + 1}
              </span>
              <span>{step}</span>
            </div>
          ))}
        </div>
      </section>

      <section className="flex min-h-screen items-center bg-[#20201d] px-5 py-12 sm:px-10 lg:px-14">
        <div className="mx-auto w-full max-w-md" id="signin-form" tabIndex={-1}>
          <Link
            aria-label="ArdaNova home"
            className="mb-12 inline-flex min-h-11 w-fit items-center lg:hidden"
            href="/"
          >
            <ArdaNovaWordmark className="text-lg" />
          </Link>

          <nav
            aria-label="Account access"
            className="mb-8 grid grid-cols-2 border border-[#f6f0eb]/35 text-sm font-bold"
          >
            <Link
              aria-current={!isSignUp ? "page" : undefined}
              className={`inline-flex min-h-11 items-center justify-center px-3 ${
                !isSignUp
                  ? "bg-[#f6f0eb] text-[#151513]"
                  : "text-[#d6d0ca] hover:bg-[#2b2b27]"
              }`}
              href={buildSignInHref(callbackUrl)}
            >
              Sign in
            </Link>
            <Link
              aria-current={isSignUp ? "page" : undefined}
              className={`inline-flex min-h-11 items-center justify-center border-l border-[#f6f0eb]/35 px-3 ${
                isSignUp
                  ? "bg-[#f6f0eb] text-[#151513]"
                  : "text-[#d6d0ca] hover:bg-[#2b2b27]"
              }`}
              href={buildSignInHref(callbackUrl, { mode: "signup" })}
            >
              Create account
            </Link>
          </nav>

          <p className="mb-4 font-mono text-xs font-semibold tracking-[0.18em] text-[#70d7e2] uppercase">
            {isSignUp ? "New member entry" : "Secure workspace entry"}
          </p>
          <h2 className="text-5xl leading-[0.9] font-black tracking-[-0.055em] uppercase sm:text-6xl">
            {isSignUp ? "Start your workspace." : "Welcome back."}
          </h2>
          <p className="mt-5 max-w-sm leading-relaxed text-[#bdb8b1]">
            {isSignUp
              ? "Continue with Google to create your ArdaNova profile. You will review project and rights-changing actions separately inside the workspace."
              : "Continue with the Google account connected to your ArdaNova workspace."}
          </p>

          <form
            action={async () => {
              "use server";
              await signIn("google", { redirectTo: callbackUrl });
            }}
            className="mt-10 border-t border-[#f6f0eb]/35 pt-8"
          >
            <Button
              className="w-full border-[#f6f0eb] bg-[#f6f0eb] text-[#151513] hover:bg-[#dcd6d0]"
              size="lg"
              type="submit"
            >
              <svg
                aria-hidden="true"
                className="size-5"
                fill="none"
                viewBox="0 0 24 24"
              >
                <path
                  d="M21.8 12.23c0-.71-.06-1.4-.18-2.06H12v3.9h5.5a4.7 4.7 0 0 1-2.04 3.08v2.53h3.3c1.93-1.78 3.04-4.4 3.04-7.45Z"
                  fill="#4285F4"
                />
                <path
                  d="M12 22.2c2.75 0 5.06-.91 6.76-2.52l-3.3-2.53c-.92.61-2.08.98-3.46.98-2.65 0-4.9-1.79-5.7-4.2H2.9v2.61A10.2 10.2 0 0 0 12 22.2Z"
                  fill="#34A853"
                />
                <path
                  d="M6.3 13.93A6.1 6.1 0 0 1 5.98 12c0-.67.12-1.32.32-1.93V7.46H2.9A10.2 10.2 0 0 0 1.8 12c0 1.64.39 3.2 1.1 4.54l3.4-2.61Z"
                  fill="#FBBC05"
                />
                <path
                  d="M12 5.87c1.5 0 2.84.51 3.9 1.51l2.92-2.91A9.82 9.82 0 0 0 12 1.8a10.2 10.2 0 0 0-9.1 5.66l3.4 2.61c.8-2.41 3.05-4.2 5.7-4.2Z"
                  fill="#EA4335"
                />
              </svg>
              {isSignUp ? "Create account with Google" : "Continue with Google"}
              <ArrowRight aria-hidden="true" className="ml-auto" />
            </Button>
          </form>
          <p className="mt-3 text-xs leading-relaxed text-[#aaa59f]">
            You’ll continue to Google in a new authentication step, then return
            to this ArdaNova workspace. Creating an account and signing in use
            the same secure provider flow.
          </p>

          <div className="mt-6 border border-[#f6f0eb]/25 p-4">
            <div className="flex gap-3">
              <Check
                aria-hidden="true"
                className="mt-0.5 size-4 shrink-0 text-[#70d7e2]"
              />
              <p className="text-sm leading-relaxed text-[#bdb8b1]">
                Signing in authenticates your account. Publishing, approving,
                and settlement actions remain separate, deliberate steps in the
                workspace.
              </p>
            </div>
          </div>

          <Link
            className="mt-10 inline-flex min-h-11 items-center gap-2 text-sm font-semibold text-[#d6d0ca] hover:text-[#70d7e2]"
            href="/"
          >
            <ArrowLeft aria-hidden="true" className="size-4" />
            Return to the public site
          </Link>
        </div>
      </section>
    </main>
  );
}
