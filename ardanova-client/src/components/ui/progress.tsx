"use client"

import * as React from "react"
import * as ProgressPrimitive from "@radix-ui/react-progress"
import { cva, type VariantProps } from "class-variance-authority"

import { cn } from "~/lib/utils"

const progressVariants = cva(
  "relative h-3 w-full overflow-hidden bg-secondary border-2 border-border",
  {
    variants: {
      variant: {
        default: "",
        neon: "border-neon-cyan/30",
        success: "border-neon-green/30",
        warning: "border-neon-yellow/30",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  }
)

const indicatorVariants = cva(
  "h-full w-full flex-1 transition-all duration-300",
  {
    variants: {
      variant: {
        default: "bg-primary",
        neon: "bg-neon-cyan shadow-[0_0_8px_rgba(0,212,255,0.4)]",
        success: "bg-neon-green shadow-[0_0_8px_rgba(0,255,136,0.4)]",
        warning: "bg-neon-yellow shadow-[0_0_8px_rgba(251,191,36,0.4)]",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  }
)

interface ProgressProps
  extends React.ComponentProps<typeof ProgressPrimitive.Root>,
    VariantProps<typeof progressVariants> {}

function Progress({ className, value, variant, ...props }: ProgressProps) {
  return (
    <ProgressPrimitive.Root
      data-slot="progress"
      className={cn(progressVariants({ variant }), className)}
      {...props}
    >
      <ProgressPrimitive.Indicator
        data-slot="progress-indicator"
        className={indicatorVariants({ variant })}
        style={{ transform: `translateX(-${100 - (value || 0)}%)` }}
      />
    </ProgressPrimitive.Root>
  )
}

export { Progress }
