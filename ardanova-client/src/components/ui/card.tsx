import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"

import { cn } from "~/lib/utils"

const cardVariants = cva(
  "bg-card text-card-foreground flex flex-col border-2 transition-all",
  {
    variants: {
      variant: {
        // Default - Clean brutalist
        default: "border-border hover:border-primary/50",

        // Elevated - Hard shadow effect
        elevated: "border-border shadow-[4px_4px_0_0_var(--border)] hover:shadow-[6px_6px_0_0_var(--primary)] hover:border-primary",

        // Outlined - Prominent border
        outlined: "border-primary/30 hover:border-primary",

        // Ghost - Minimal
        ghost: "border-transparent bg-transparent hover:bg-card/50 hover:border-border",

        // Neon - Subtle glow
        neon: "border-neon-cyan/40 hover:border-neon-cyan hover:shadow-[0_0_12px_rgba(0,212,255,0.15)]",

        // Neon Pink
        "neon-pink": "border-neon-pink/40 hover:border-neon-pink hover:shadow-[0_0_12px_rgba(255,0,128,0.15)]",

        // Neon Green
        "neon-green": "border-neon-green/40 hover:border-neon-green hover:shadow-[0_0_12px_rgba(0,255,136,0.15)]",

        // Interactive - For clickable cards
        interactive: "border-border cursor-pointer hover:border-primary hover:bg-card/80 active:scale-[0.99]",
      },
      padding: {
        none: "",
        sm: "p-4",
        default: "p-6",
        lg: "p-8",
      },
    },
    defaultVariants: {
      variant: "default",
      padding: "default",
    },
  }
)

export interface CardProps
  extends React.ComponentProps<"div">,
    VariantProps<typeof cardVariants> {}

function Card({ className, variant, padding, ...props }: CardProps) {
  return (
    <div
      data-slot="card"
      className={cn(cardVariants({ variant, padding }), className)}
      {...props}
    />
  )
}

function CardHeader({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="card-header"
      className={cn(
        "flex flex-col gap-1.5 [.border-b]:pb-4",
        className
      )}
      {...props}
    />
  )
}

function CardTitle({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="card-title"
      className={cn("text-lg font-bold tracking-tight", className)}
      {...props}
    />
  )
}

function CardDescription({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="card-description"
      className={cn("text-muted-foreground text-sm", className)}
      {...props}
    />
  )
}

function CardAction({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="card-action"
      className={cn("ml-auto flex items-center gap-2", className)}
      {...props}
    />
  )
}

function CardContent({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="card-content"
      className={cn("flex-1", className)}
      {...props}
    />
  )
}

function CardFooter({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="card-footer"
      className={cn("flex items-center gap-4 pt-4 [.border-t]:pt-4 border-t border-border", className)}
      {...props}
    />
  )
}

export {
  Card,
  CardHeader,
  CardFooter,
  CardTitle,
  CardAction,
  CardDescription,
  CardContent,
  cardVariants,
}
