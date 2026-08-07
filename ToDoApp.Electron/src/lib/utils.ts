import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";
import type { ThemeMode } from "../types";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function applyThemeClass(theme: ThemeMode) {
  document.documentElement.classList.toggle("dark", theme === "dark");
}

