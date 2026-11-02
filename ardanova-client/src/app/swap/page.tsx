"use client";
import { Suspense } from "react";
import { SellUnitsScreen } from "~/lib/azoa-money/screens";

export default function Page() {
  return (
    <Suspense fallback={<p role="status">Loading your holdings.</p>}>
      <SellUnitsScreen />
    </Suspense>
  );
}
