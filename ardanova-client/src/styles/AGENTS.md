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
