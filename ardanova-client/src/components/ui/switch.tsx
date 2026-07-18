"use client";

import * as React from "react";
import * as SwitchPrimitive from "@radix-ui/react-switch";

import { cn } from "~/lib/utils";

function Switch({
  className,
  ...props
}: React.ComponentProps<typeof SwitchPrimitive.Root>) {
  return (
    <SwitchPrimitive.Root
      data-slot="switch"
      className={cn(
        "peer before:border-border relative inline-flex size-11 shrink-0 cursor-pointer items-center bg-transparent before:pointer-events-none before:absolute before:top-2.5 before:left-0 before:h-6 before:w-11 before:border-2 before:transition-colors before:content-['']",
        "focus-visible:ring-ring focus-visible:ring-offset-background focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none",
        "disabled:cursor-not-allowed disabled:opacity-50",
        "data-[state=checked]:before:border-primary data-[state=checked]:before:bg-primary",
        "data-[state=unchecked]:before:bg-secondary",
        className,
      )}
      {...props}
    >
      <SwitchPrimitive.Thumb
        data-slot="switch-thumb"
        className={cn(
          "bg-foreground pointer-events-none absolute top-3 left-0.5 block size-5 transition-transform",
          "data-[state=checked]:bg-primary-foreground data-[state=checked]:translate-x-5",
          "data-[state=unchecked]:translate-x-0",
        )}
      />
    </SwitchPrimitive.Root>
  );
}

export { Switch };
