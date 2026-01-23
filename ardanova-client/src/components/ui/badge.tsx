import * as React from "react"
import { Slot } from "@radix-ui/react-slot"
import { cva, type VariantProps } from "class-variance-authority"

import { cn } from "~/lib/utils"

const badgeVariants = cva(
  "inline-flex items-center justify-center border px-2.5 py-0.5 text-xs font-medium w-fit whitespace-nowrap shrink-0 [&>svg]:size-3 gap-1.5 [&>svg]:pointer-events-none transition-colors",
  {
    variants: {
      variant: {
        // Default - Primary color
        default:
          "border-transparent bg-primary text-primary-foreground",

        // Secondary - Muted
        secondary:
          "border-transparent bg-secondary text-secondary-foreground",

        // Destructive - Error/warning
        destructive:
          "border-transparent bg-destructive text-destructive-foreground",

        // Outline - Border only
        outline:
          "border-border bg-transparent text-foreground",

        // Success
        success:
          "border-transparent bg-neon-green/20 text-neon-green",

        // Warning
        warning:
          "border-transparent bg-neon-yellow/20 text-neon-yellow",

        // Info
        info:
          "border-transparent bg-neon-cyan/20 text-neon-cyan",

        // === NEON VARIANTS ===

        // Neon Cyan
        neon:
          "border-neon-cyan bg-neon-cyan/10 text-neon-cyan",

        // Neon Pink
        "neon-pink":
          "border-neon-pink bg-neon-pink/10 text-neon-pink",

        // Neon Green
        "neon-green":
          "border-neon-green bg-neon-green/10 text-neon-green",

        // Neon Purple
        "neon-purple":
          "border-neon-purple bg-neon-purple/10 text-neon-purple",

        // Solid Neon variants
        "neon-solid":
          "border-transparent bg-neon-cyan text-black font-bold",

        "neon-pink-solid":
          "border-transparent bg-neon-pink text-black font-bold",

        "neon-green-solid":
          "border-transparent bg-neon-green text-black font-bold",
      },
      size: {
        default: "px-2.5 py-0.5 text-xs",
        sm: "px-2 py-0 text-[10px]",
        lg: "px-3 py-1 text-sm",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
)

export interface BadgeProps
  extends React.ComponentProps<"span">,
    VariantProps<typeof badgeVariants> {
  asChild?: boolean
}

function Badge({
  className,
  variant,
  size,
  asChild = false,
  ...props
}: BadgeProps) {
  const Comp = asChild ? Slot : "span"

  return (
    <Comp
      data-slot="badge"
      className={cn(badgeVariants({ variant, size }), className)}
      {...props}
    />
  )
}

export { Badge, badgeVariants }
