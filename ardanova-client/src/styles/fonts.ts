import { Inter, JetBrains_Mono } from "next/font/google";

/** Brand typefaces, shared by the root layout and global-error (which renders its own `<html>`). */
export const inter = Inter({
  subsets: ["latin"],
  variable: "--font-inter",
  display: "swap",
});

export const jetbrainsMono = JetBrains_Mono({
  subsets: ["latin"],
  variable: "--font-jetbrains-mono",
  display: "swap",
});

/** Class list for `<html>` that exposes both font CSS variables used by `globals.css`. */
export const fontVariables = `${inter.variable} ${jetbrainsMono.variable}`;
