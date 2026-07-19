# Development launcher contracts

- `-Install` / `--install` is the explicit fresh-install path and must use
  `npm ci` against the committed client lockfile.
- `npm ci` invokes the package `postinstall`, which delegates once to
  `npm run generate:prisma`; launchers must not add a second generator call.
- Development Compose recreates its isolated dependency volume from the
  lockfile before starting the client.
