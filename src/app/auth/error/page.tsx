import { Button } from "~/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "~/components/ui/card";
import Link from "next/link";

interface ErrorPageProps {
  searchParams: Promise<{
    error?: string;
  }>;
}

const errorMessages: Record<string, { title: string; description: string }> = {
  Configuration: {
    title: "Server Configuration Error",
    description: "There is a problem with the server configuration. Please contact support.",
  },
  AccessDenied: {
    title: "Access Denied",
    description: "You do not have permission to sign in.",
  },
  Verification: {
    title: "Verification Required",
    description: "The sign in link is no longer valid. Please try again.",
  },
  Default: {
    title: "Authentication Error",
    description: "An error occurred during authentication. Please try again.",
  },
};

export default async function ErrorPage({ searchParams }: ErrorPageProps) {
  const params = await searchParams;
  const error = params.error ?? "Default";
  const errorInfo = errorMessages[error] ?? errorMessages.Default;

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 flex items-center justify-center p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-red-100">
            <svg
              className="h-8 w-8 text-red-600"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z"
              />
            </svg>
          </div>
          <CardTitle className="text-xl">{errorInfo?.title ?? "Authentication Error"}</CardTitle>
          <CardDescription>{errorInfo?.description ?? "An error occurred during authentication. Please try again."}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="text-center text-sm text-muted-foreground">
            Error: {error}
          </div>
          <div className="flex flex-col space-y-2">
            <Button asChild className="w-full">
              <Link href="/">Return Home</Link>
            </Button>
            <Button variant="outline" asChild className="w-full">
              <Link href="/auth/signin">Try Again</Link>
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
