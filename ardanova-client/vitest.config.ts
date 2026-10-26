import { defineConfig } from "vitest/config";
import path from "path";

export default defineConfig({
  // tsconfig keeps JSX as "preserve" for Next; component tests need it compiled.
  oxc: { jsx: { runtime: "automatic" } },
  test: {
    globals: true,
    environment: "node",
    include: ["src/**/*.test.ts", "src/**/*.test.tsx", "src/**/*.spec.ts"],
    coverage: {
      provider: "v8",
      reporter: ["text", "lcov"],
      include: ["src/**/*.ts"],
      exclude: [
        "src/**/*.test.ts",
        "src/**/*.spec.ts",
        "src/**/*.d.ts",
        "src/app/**",
        "src/lib/zod/**",
      ],
    },
  },
  resolve: {
    alias: {
      "~": path.resolve(__dirname, "./src"),
    },
  },
});
