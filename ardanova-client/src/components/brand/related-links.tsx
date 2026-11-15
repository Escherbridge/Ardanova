import Link from "next/link";

/** Real URLs only. Omit the block at the call site when the list is empty. */
export function RelatedLinks({
  heading = "Related",
  items,
}: {
  heading?: string;
  items: readonly { href: string; label: string }[];
}) {
  return (
    <section className="space-y-2">
      <h2 className="text-xl">{heading}</h2>
      <ul className="space-y-1">
        {items.map((item) => (
          <li key={item.href}>
            <Link className="underline underline-offset-4" href={item.href}>
              {item.label}
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}
