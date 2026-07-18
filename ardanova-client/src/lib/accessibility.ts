import type { KeyboardEvent } from "react";

export function handleTabListKeyDown(event: KeyboardEvent<HTMLElement>) {
  const target = event.target;
  if (
    !(target instanceof HTMLElement) ||
    target.getAttribute("role") !== "tab"
  ) {
    return;
  }

  const tabs = Array.from(
    event.currentTarget.querySelectorAll<HTMLElement>(
      '[role="tab"]:not([aria-disabled="true"])',
    ),
  );
  const currentIndex = tabs.indexOf(target);
  if (currentIndex < 0 || tabs.length < 2) return;

  let nextIndex: number | undefined;
  switch (event.key) {
    case "ArrowRight":
    case "ArrowDown":
      nextIndex = (currentIndex + 1) % tabs.length;
      break;
    case "ArrowLeft":
    case "ArrowUp":
      nextIndex = (currentIndex - 1 + tabs.length) % tabs.length;
      break;
    case "Home":
      nextIndex = 0;
      break;
    case "End":
      nextIndex = tabs.length - 1;
      break;
  }

  if (nextIndex === undefined) return;
  event.preventDefault();
  tabs[nextIndex]?.focus();
  tabs[nextIndex]?.click();
}
