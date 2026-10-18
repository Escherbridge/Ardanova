---
type: reference
---

# styles

## Semantic tokens

`:root`, `.dark` and `.public-shell` define the semantic palette (`primary`,
`system`, `success`, `warning`, `destructive`, `muted`, ...). App UI uses these
so it flips with the theme. Status colours (priority dots, badges) should map
to `success` / `warning` / `destructive` / `system`, not raw `bg-blue-500`.

## brand-palette

`--color-brand-*` in the first `@theme` block is a **fixed** palette. It does
not change under `.dark`. The landing page and brand marks use it for sections
that are deliberately inverted (ink background, paper text, cyan accents)
whatever the app theme is. Each token is the exact hex the landing page used
before, so moving to tokens changed no pixels.

Use `brand-*` only for marketing/brand surfaces. In-app UI uses the semantic
tokens.

Small brand-red text (eyebrows, `text-xs` labels) uses `brand-signal-deep`
(#b8322a, 5.2:1 on paper). `--primary` / `brand-signal` (#ef4638) is about
3.3:1 on paper. Keep it for fills, borders and display-size type.

## text-safe-accents

`text-primary` appeared about 200 times, often as `text-xs` eyebrows, at
3.28:1 on paper. The tinted-badge recipe `bg-X/20 text-X` also failed AA for
primary (2.56), system/neon (3.38), neon-pink (3.85), destructive (4.03),
warning (4.08) and success (4.22). Darkening `--primary` would also have
darkened every brand fill. Instead the theme splits text from fill:

- Tailwind v4 resolves `text-X` from `--text-color-X` before `--color-X`
  (`themeKeys: ["--text-color", "--color"]` in tailwindcss 4.1). The
  `@theme inline` block maps `--text-color-{primary,destructive,system,
  success,warning}` and their `neon-*` aliases to `--{accent}-text`. `bg-*`,
  `border-*` and `fill-*` still read `--color-X`, so fills keep the brand hue.
- `--primary-text` and the other text tokens are set per theme block. In light
  themes they are darker (`#a32b23`, `#0b5f69`, `#1d5a43`, `#6b4a00`). In
  `.dark` only red changes, to `#ff8b80`; the other dark accents already
  passed.
- The `token contrast (WCAG AA)` gate recomputes every pair from `globals.css`:
  text on background, card and muted surfaces, text on its own 10% and 20%
  tint, and solid `X-foreground` on `X`. A new accent or a retuned hex that
  fails 4.5:1 turns the suite red.
- `text-primary-foreground` and other `-foreground` keys are separate theme
  keys and are unaffected.

## neon-aliases

`neon-*` are legacy aliases kept so older screens compile. New code uses the
semantic token directly.

- `neon` (bare): `text-neon`, `bg-neon/10` and similar appear about 74 times
  (tasks board "In Progress", events, guild and opportunity forms). Until
  loop2 no `--color-neon` existed, so Tailwind v4 generated **no CSS** for
  them and those states rendered unstyled. It now maps to `--neon-cyan`, which
  is `system`.
- `neon-pink`: in light themes this is now #b8322a (text-safe red) instead of
  `--primary`, because it is mostly used as `text-neon-pink` at body size. In
  `.dark` it stays `--primary` (#ff7164), which already passes on ink.
- `neon-purple`: maps to `chart-5` (#5d4b91 light, #b9a7ff dark). Before, it
  was `system`, the same colour as `neon-cyan`, so the board's "In Progress"
  and "In Review" columns could not be told apart.

## design-gate

`design-gate.test.ts` runs with the normal vitest suite and fails when:

1. a semantic-family colour class (`neon|system|primary|brand|success|...`)
   names a token that `globals.css` does not define. A missing class fails
   silently in Tailwind v4, and the visual scorecard cannot see it.
   `KNOWN_UNDEFINED_TOKENS` lists misses that belong to other lanes, as
   `file:token`.
2. raw Tailwind palette classes (`bg-blue-500` and similar) grow past
   `RAW_PALETTE_CEILING`;
3. JSX `{…error.message}` expressions grow past `RAW_ERROR_MESSAGE_CEILING`
   (see `components/ui/AGENTS.md` §mutation-error).
4. `toast(...)` / `alert(...)` calls whose arguments show any `x.message` grow
   past `RAW_ERROR_NOTIFIER_CEILING`. The JSX regex cannot see these, and toasts
   were where most mutation failures leaked raw text.

5. `border-white/N` / `divide-white/N` classes grow past
   `WHITE_ALPHA_BORDER_CEILING`. These were left over from the old dark theme
   and do not show on `#f4efeb` paper. Replace them with `border-border`.
6. `app/**/page.tsx` files whose page and ancestor layouts (excluding the
   root layout) set no `metadata.title` / `generateMetadata` grow past
   `UNTITLED_PAGE_CEILING`. Without one, the route serves the root default
   title, so browser tabs and screen-reader announcements cannot tell the
   routes apart (WCAG 2.4.2). The usual fix is a sibling server `layout.tsx`
   with `export const metadata = { title: "Projects" }`; the root template
   appends " — ArdaNova".
7. any text token pair falls below 4.5:1 (see §text-safe-accents).

The ceilings were re-measured at loop3 (palette 156 → 144). The ceilings are a ratchet. Lower them in the same commit that pays down the
debt, and never raise them.

## fonts

`fonts.ts` defines Inter and JetBrains Mono once. The root layout and
`app/global-error.tsx` both put `fontVariables` on `<html>`. global-error
replaces the root layout, so without it the last-resort screen fell back to
system fonts.
