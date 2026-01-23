import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"

import { cn } from "~/lib/utils"

const inputVariants = cva(
  "flex w-full min-w-0 bg-transparent text-foreground transition-all outline-none disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 placeholder:text-muted-foreground file:border-0 file:bg-transparent file:text-sm file:font-medium file:text-foreground",
  {
    variants: {
      variant: {
        // Default - Clean brutalist
        default:
          "border-2 border-input bg-background focus:border-primary focus:ring-2 focus:ring-primary/20",

        // Ghost - Minimal border
        ghost:
          "border-2 border-transparent bg-secondary/50 focus:border-border focus:bg-background",

        // Underline - Bottom border only
        underline:
          "border-0 border-b-2 border-input bg-transparent px-0 focus:border-primary",

        // Neon - Cyberpunk glow on focus
        neon:
          "border-2 border-input bg-background focus:border-neon-cyan focus:shadow-[0_0_10px_rgba(0,240,255,0.2)]",

        // Neon Pink
        "neon-pink":
          "border-2 border-input bg-background focus:border-neon-pink focus:shadow-[0_0_10px_rgba(255,0,170,0.2)]",

        // Filled - Solid background
        filled:
          "border-2 border-transparent bg-secondary focus:border-primary focus:bg-background",
      },
      inputSize: {
        default: "h-10 px-4 py-2 text-sm",
        sm: "h-8 px-3 py-1 text-xs",
        lg: "h-12 px-5 py-3 text-base",
      },
    },
    defaultVariants: {
      variant: "default",
      inputSize: "default",
    },
  }
)

export interface InputProps
  extends Omit<React.ComponentProps<"input">, "size">,
    VariantProps<typeof inputVariants> {}

function Input({
  className,
  type,
  variant,
  inputSize,
  ...props
}: InputProps) {
  return (
    <input
      type={type}
      data-slot="input"
      className={cn(inputVariants({ variant, inputSize }), className)}
      {...props}
    />
  )
}

export { Input, inputVariants }
