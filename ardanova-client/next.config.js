/**
 * Run `build` or `dev` with `SKIP_ENV_VALIDATION` to skip env validation. This is especially useful
 * for Docker builds.
 */
import "./src/env.js";

/** @type {import("next").NextConfig} */
const config = {
  // Turbopack: use cwd as workspace root when running dev (avoids multiple-lockfile warning)
  turbopack: { root: process.cwd() },
  // Enable standalone output for Docker deployment
  output: "standalone",
  serverExternalPackages: ["@microsoft/signalr"],
  typescript: {
    ignoreBuildErrors: false,
  },
  // Lint is run via `npm run lint` / CI; type-checked ESLint rules are noisy with `strict: false`.
  eslint: {
    ignoreDuringBuilds: true,
  },
};

export default config;
