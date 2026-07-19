import { AlertTriangle, ArrowLeft, RotateCcw } from "lucide-react";
import Link from "next/link";

import { ArdaNovaWordmark } from "~/components/brand/wordmark";
import { Button } from "~/components/ui/button";

import { authErrorSecondaryButtonClass } from "./styles";

interface ErrorPageProps {
  searchParams: Promise<{ error?: string }>;
}

const errorMessages = {
  Configuration: {
    title: "Sign-in is not configured here.",
    description:
      "This environment cannot reach its authentication provider. No account or workspace action was completed.",
  },
  AccessDenied: {
    title: "This account cannot enter.",
    description:
      "The provider returned an access denial. Check the account you selected or ask a workspace steward for help.",
  },
  Verification: {
    title: "That verification is no longer valid.",
    description:
      "The sign-in verification expired or was already used. Start a new sign-in request.",
  },
  Default: {
    title: "Sign-in did not complete.",
    description:
      "Your workspace remains unchanged. Try again, or return to the public site while the connection is checked.",
  },
} as const;

export default async function ErrorPage({ searchParams }: ErrorPageProps) {
  const { error } = await searchParams;
  const reference =
    error && Object.hasOwn(errorMessages, error)
      ? (error as keyof typeof errorMessages)
      : "Default";
  const errorInfo = errorMessages[reference];

  return (
    <main className="grid min-h-screen bg-[#151513] text-[#f6f0eb] lg:grid-cols-[minmax(0,1fr)_minmax(26rem,0.7fr)]">
      <section className="flex min-h-[40vh] flex-col justify-between border-b-2 border-[#f6f0eb]/35 p-6 lg:min-h-screen lg:border-r-2 lg:border-b-0 lg:p-12">
        <Link
          href="/"
          aria-label="ArdaNova home"
          className="inline-flex min-h-11 w-fit items-center"
        >
          <ArdaNovaWordmark className="text-lg" />
        </Link>

        <div className="my-16 max-w-4xl">
          <p className="mb-5 font-mono text-xs font-bold tracking-[0.16em] text-[#70d7e2] uppercase">
            Authentication / no state changed
          </p>
          <h1 className="max-w-3xl text-[clamp(3.5rem,9vw,8rem)] leading-[0.82] font-black tracking-[-0.075em] uppercase">
            Pause.
            <br />
            Keep the work safe.
          </h1>
        </div>

        <p className="max-w-xl border-t border-[#f6f0eb]/35 pt-5 text-sm leading-relaxed text-[#c9c3bd]">
          Authentication proves who is entering. Publishing, approvals, funds,
          settlement, and rights remain separate actions inside the workspace.
        </p>
      </section>

      <section className="flex items-center bg-[#20201d] px-6 py-14 sm:px-10 lg:px-14">
        <div className="w-full max-w-lg">
          <div className="mb-8 flex size-16 items-center justify-center border-2 border-[#ef4638] text-[#ef4638]">
            <AlertTriangle className="size-8" aria-hidden="true" />
          </div>
          <p className="font-mono text-xs font-bold tracking-[0.16em] text-[#70d7e2] uppercase">
            Reference / {reference}
          </p>
          <h2 className="mt-4 text-4xl leading-[0.92] font-black tracking-[-0.05em] uppercase sm:text-5xl">
            {errorInfo.title}
          </h2>
          <p className="mt-6 text-base leading-relaxed text-[#c9c3bd]">
            {errorInfo.description}
          </p>

          <div className="mt-10 grid gap-3 border-t border-[#f6f0eb]/35 pt-8 sm:grid-cols-2">
            <Button
              asChild
              className="border-[#f6f0eb] bg-[#f6f0eb] text-[#151513] hover:bg-[#dcd6d0]"
            >
              <Link href="/auth/signin">
                <RotateCcw className="size-4" aria-hidden="true" />
                Try sign-in again
              </Link>
            </Button>
            <Button
              asChild
              variant="outline"
              className={authErrorSecondaryButtonClass}
            >
              <Link href="/">
                <ArrowLeft className="size-4" aria-hidden="true" />
                Public site
              </Link>
            </Button>
          </div>
        </div>
      </section>
    </main>
  );
}
