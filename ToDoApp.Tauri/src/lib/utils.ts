import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";
import type { ThemeMode } from "../types";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function applyThemeClass(theme: ThemeMode) {
  document.documentElement.classList.toggle("dark", theme === "dark");
  // Keep CSS opacity fallback in sync when inline backgrounds are used
  if (document.documentElement.style.backgroundColor) {
    const color =
      theme === "dark"
        ? "rgba(17, 24, 39, 0.95)"
        : "rgba(249, 250, 251, 0.95)";
    document.documentElement.style.backgroundColor = color;
    document.body.style.backgroundColor = color;
  }
}

