"use client";
import { useId } from "react";
import type { MoneySlots, CardSlotProps } from "azoa-sdk/react";
import { Button } from "~/components/ui/button";
import { Input } from "~/components/ui/input";

function MoneyCard({ title, description, children, className }: CardSlotProps) {
  const id = useId();
  return (
    <section
      aria-labelledby={title ? id : undefined}
      className={`border-border space-y-4 border p-5 ${className ?? ""}`}
    >
      {title ? (
        <h2 id={id} className="font-mono text-lg font-semibold">
          {title}
        </h2>
      ) : null}
      {description ? (
        <div className="text-muted-foreground text-sm">{description}</div>
      ) : null}
      {children}
    </section>
  );
}

export const ardaMoneySlots: MoneySlots = {
  Button: ({ variant, ...props }) => (
    <Button
      {...props}
      type={props.type ?? "button"}
      variant={variant === "secondary" ? "outline" : "default"}
    />
  ),
  Input: (props) => <Input {...props} />,
  Card: MoneyCard,
  Alert: ({ tone, children, className }) => (
    <div
      role={tone === "error" || tone === "warning" ? "alert" : "status"}
      className={`border-border border-l-2 p-3 text-sm ${className ?? ""}`}
    >
      {children}
    </div>
  ),
};
