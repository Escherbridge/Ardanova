"use client";
import { Suspense } from "react";
import { WithdrawScreen } from "~/lib/azoa-money/screens";

export default function Page() {
  return (
    <Suspense fallback={<p role="status">Loading your account.</p>}>
      <WithdrawScreen />
    </Suspense>
  );
}
