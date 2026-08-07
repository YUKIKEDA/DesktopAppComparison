import type { ProjectData, ThemeData } from "../types";
import { runInBackground } from "./scheduleWork";

function parseThemeData(raw: unknown): ThemeData {
  const data = raw as { theme?: string };
  if (data?.theme === "dark" || data?.theme === "light") {
    return { theme: data.theme };
  }
  return { theme: "light" };
}

export class DataService {
  static async loadData(): Promise<ProjectData> {
    if (window.electronAPI) {
      return await window.electronAPI.loadData();
    }
    // Fallback for development
    const stored = localStorage.getItem("project-data");
    return stored ? JSON.parse(stored) : { items: [] };
  }

  static async saveData(data: ProjectData): Promise<void> {
    if (window.electronAPI) {
      // Yield so filter/sort UI work stays responsive before IPC clone
      await runInBackground(() => undefined);
      await window.electronAPI.saveData(data);
    } else {
      const json = await runInBackground(() => JSON.stringify(data));
      localStorage.setItem("project-data", json);
    }
  }

  static async loadTheme(): Promise<ThemeData> {
    if (window.electronAPI?.loadTheme) {
      return await window.electronAPI.loadTheme();
    }
    const stored = localStorage.getItem("theme-data");
    return stored ? parseThemeData(JSON.parse(stored)) : { theme: "light" };
  }

  static async saveTheme(data: ThemeData): Promise<void> {
    if (window.electronAPI?.saveTheme) {
      await window.electronAPI.saveTheme(data);
    } else {
      localStorage.setItem("theme-data", JSON.stringify(data));
    }
  }

  static async exportData(data: ProjectData): Promise<void> {
    if (window.electronAPI) {
      await window.electronAPI.exportData(data);
    }
  }

  static async importData(): Promise<ProjectData | null> {
    if (window.electronAPI) {
      return await window.electronAPI.importData();
    }
    return null;
  }

  static async importFromPath(filePath: string): Promise<ProjectData | null> {
    if (window.electronAPI) {
      return await window.electronAPI.importFromPath(filePath);
    }
    return null;
  }

  static async openDataFolder(): Promise<void> {
    if (window.electronAPI) {
      await window.electronAPI.openDataFolder();
    }
  }

  static async openDetailWindow(itemId: number): Promise<void> {
    if (window.electronAPI) {
      await window.electronAPI.openDetailWindow(itemId);
    }
  }
}
