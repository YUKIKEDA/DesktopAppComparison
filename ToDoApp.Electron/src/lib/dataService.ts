import type { ProjectData } from "../types";

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
      await window.electronAPI.saveData(data);
    } else {
      // Fallback for development
      localStorage.setItem("project-data", JSON.stringify(data));
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
