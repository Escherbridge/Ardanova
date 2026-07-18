import * as React from "react";
import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";

import { cn } from "~/lib/utils";

const buttonVariants = cva(
  "inline-flex shrink-0 items-center justify-center gap-2 whitespace-nowrap border text-sm font-semibold transition-colors disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-45 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
  {
    variants: {
      variant: {
        default:
          "border-primary bg-primary text-primary-foreground hover:bg-primary/85",
        destructive:
          "border-destructive bg-destructive text-destructive-foreground hover:bg-destructive/85",
        outline:
          "border-border bg-background text-foreground hover:border-foreground hover:bg-secondary",
        secondary:
          "border-secondary bg-secondary text-secondary-foreground hover:border-border hover:bg-muted",
        ghost:
          "border-transparent bg-transparent text-foreground hover:border-border hover:bg-secondary",
        link: "border-transparent bg-transparent px-0 text-foreground underline decoration-primary decoration-2 underline-offset-4 hover:bg-primary/10",
        neon: "border-system bg-system text-system-foreground hover:bg-system/85",
        "neon-pink":
          "border-primary bg-primary text-primary-foreground hover:bg-primary/85",
        "neon-green":
          "border-success bg-success text-success-foreground hover:bg-success/85",
        "outline-neon":
          "border-system bg-transparent text-foreground hover:bg-accent",
        "outline-neon-pink":
          "border-primary bg-transparent text-foreground hover:bg-primary hover:text-primary-foreground",
        "outline-neon-green":
          "border-success bg-transparent text-success hover:bg-success hover:text-success-foreground",
        "ghost-neon":
          "border-transparent bg-transparent text-foreground hover:border-system hover:bg-accent",
      },
      size: {
        default: "min-h-11 px-5 py-2",
        sm: "min-h-11 gap-1.5 px-3 text-xs",
        lg: "h-12 px-7 text-base",
        xl: "h-14 px-9 text-lg",
        icon: "size-11",
        "icon-sm": "size-11",
        "icon-lg": "size-12",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  },
);

export interface ButtonProps
  extends React.ComponentProps<"button">,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean;
}

function Button({
  className,
  variant,
  size,
  asChild = false,
  ...props
}: ButtonProps) {
  const Comp = asChild ? Slot : "button";

  return (
    <Comp
      data-slot="button"
      className={cn(buttonVariants({ variant, size, className }))}
      {...props}
    />
  );
}

export { Button, buttonVariants };
