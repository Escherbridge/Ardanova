import * as React from "react"
import { Slot } from "@radix-ui/react-slot"
import { cva, type VariantProps } from "class-variance-authority"

import { cn } from "~/lib/utils"

const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 whitespace-nowrap text-sm font-medium transition-all disabled:pointer-events-none disabled:opacity-50 [&_svg]:pointer-events-none [&_svg:not([class*='size-'])]:size-4 shrink-0 [&_svg]:shrink-0 outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background",
  {
    variants: {
      variant: {
        // Primary - Electric cyan on dark
        default:
          "bg-primary text-primary-foreground border-2 border-primary hover:bg-primary/90 hover:shadow-[0_0_20px_rgba(0,240,255,0.3)] active:scale-[0.98]",

        // Destructive - Hot red
        destructive:
          "bg-destructive text-destructive-foreground border-2 border-destructive hover:bg-destructive/90 hover:shadow-[0_0_20px_rgba(255,51,85,0.3)]",

        // Outline - Border only, fills on hover
        outline:
          "border-2 border-border bg-transparent hover:bg-secondary hover:border-primary text-foreground",

        // Secondary - Muted surface
        secondary:
          "bg-secondary text-secondary-foreground border-2 border-secondary hover:bg-secondary/80 hover:border-muted-foreground",

        // Ghost - No background until hover
        ghost:
          "border-2 border-transparent hover:bg-secondary hover:border-border text-foreground",

        // Link style
        link:
          "text-primary underline-offset-4 hover:underline border-none",

        // === ACCENT VARIANTS ===

        // Neon Cyan - Primary accent with subtle glow
        neon:
          "bg-neon-cyan text-background border-2 border-neon-cyan font-medium tracking-wide hover:shadow-[0_0_12px_rgba(0,212,255,0.4)] active:scale-[0.98]",

        // Neon Pink
        "neon-pink":
          "bg-neon-pink text-background border-2 border-neon-pink font-medium tracking-wide hover:shadow-[0_0_12px_rgba(255,0,128,0.4)] active:scale-[0.98]",

        // Neon Green
        "neon-green":
          "bg-neon-green text-background border-2 border-neon-green font-medium tracking-wide hover:shadow-[0_0_12px_rgba(0,255,136,0.4)] active:scale-[0.98]",

        // Outline Neon - Border glow only
        "outline-neon":
          "bg-transparent text-neon-cyan border-2 border-neon-cyan hover:bg-neon-cyan/10 hover:shadow-[0_0_8px_rgba(0,212,255,0.3)] font-medium",

        "outline-neon-pink":
          "bg-transparent text-neon-pink border-2 border-neon-pink hover:bg-neon-pink/10 hover:shadow-[0_0_8px_rgba(255,0,128,0.3)] font-medium",

        "outline-neon-green":
          "bg-transparent text-neon-green border-2 border-neon-green hover:bg-neon-green/10 hover:shadow-[0_0_8px_rgba(0,255,136,0.3)] font-medium",

        // Ghost Neon - Subtle until hover
        "ghost-neon":
          "bg-transparent text-neon-cyan border-2 border-transparent hover:border-neon-cyan/40 hover:bg-neon-cyan/5",
      },
      size: {
        default: "h-10 px-5 py-2",
        sm: "h-8 gap-1.5 px-3 text-xs",
        lg: "h-12 px-8 text-base",
        xl: "h-14 px-10 text-lg",
        icon: "size-10",
        "icon-sm": "size-8",
        "icon-lg": "size-12",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
)

export interface ButtonProps
  extends React.ComponentProps<"button">,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean
}

function Button({
  className,
  variant,
  size,
  asChild = false,
  ...props
}: ButtonProps) {
  const Comp = asChild ? Slot : "button"

  return (
    <Comp
      data-slot="button"
      className={cn(buttonVariants({ variant, size, className }))}
      {...props}
    />
  )
}

export { Button, buttonVariants }
