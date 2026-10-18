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

The ceilings are a ratchet. Lower them in the same commit that pays down the
debt, and never raise them.

## fonts

`fonts.ts` defines Inter and JetBrains Mono once. The root layout and
`app/global-error.tsx` both put `fontVariables` on `<html>`. global-error
replaces the root layout, so without it the last-resort screen fell back to
system fonts.
