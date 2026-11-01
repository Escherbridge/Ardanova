"use client";

import { useEffect, useState } from "react";

/** Whether `element` is within `rootMargin` of the viewport; always false where IntersectionObserver is missing. */
export function useInView(
  element: Element | null,
  rootMargin = "600px 0px",
): boolean {
  const [inView, setInView] = useState(false);

  useEffect(() => {
    if (!element || typeof IntersectionObserver === "undefined") return;
    const observer = new IntersectionObserver(
      (entries) => {
        const entry = entries[entries.length - 1];
        if (entry) setInView(entry.isIntersecting);
      },
      { rootMargin },
    );
    observer.observe(element);
    return () => observer.disconnect();
  }, [element, rootMargin]);

  return inView;
}
