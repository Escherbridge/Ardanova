import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";

import { cn } from "~/lib/utils";

const inputVariants = cva(
  "flex w-full min-w-0 bg-transparent text-foreground transition-colors disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 placeholder:text-muted-foreground file:border-0 file:bg-transparent file:text-sm file:font-medium file:text-foreground aria-invalid:border-destructive aria-invalid:ring-2 aria-invalid:ring-destructive/20",
  {
    variants: {
      variant: {
        default:
          "border border-input bg-background focus:border-system focus:ring-2 focus:ring-system/20",
        ghost:
          "border border-transparent bg-secondary/50 focus:border-border focus:bg-background",
        underline:
          "border-0 border-b border-input bg-transparent px-0 focus:border-system",
        neon: "border border-input bg-background focus:border-system focus:ring-2 focus:ring-system/20",
        "neon-pink":
          "border border-input bg-background focus:border-primary focus:ring-2 focus:ring-primary/20",
        filled:
          "border border-transparent bg-secondary focus:border-system focus:bg-background",
      },
      inputSize: {
        default: "h-11 px-4 py-2 text-sm",
        sm: "h-11 px-3 py-1 text-xs",
        lg: "h-12 px-5 py-3 text-base",
      },
    },
    defaultVariants: {
      variant: "default",
      inputSize: "default",
    },
  },
);

export interface InputProps
  extends Omit<React.ComponentProps<"input">, "size">,
    VariantProps<typeof inputVariants> {}

function Input({ className, type, variant, inputSize, ...props }: InputProps) {
  return (
    <input
      type={type}
      data-slot="input"
      className={cn(inputVariants({ variant, inputSize }), className)}
      {...props}
    />
  );
}

export { Input, inputVariants };
