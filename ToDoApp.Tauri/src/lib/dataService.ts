import { invoke } from "@tauri-apps/api/core";
import { open, save } from "@tauri-apps/plugin-dialog";
import { readTextFile, writeTextFile } from "@tauri-apps/plugin-fs";
import { openPath } from "@tauri-apps/plugin-opener";
import type { ProjectData } from "../types";

export class DataService {
  static async loadData(): Promise<ProjectData> {
    try {
      const dataDir = await invoke<string>("get_app_data_dir");
      const dataFile = `${dataDir}/project.json`;
      
      try {
        const content = await readTextFile(dataFile);
        const data: ProjectData = JSON.parse(content);
        return data;
      } catch (error) {
        // File doesn't exist, return empty data
        return { items: [] };
      }
    } catch (error) {
      console.error("Failed to load data:", error);
      return { items: [] };
    }
  }

  static async saveData(data: ProjectData): Promise<void> {
    try {
      const dataDir = await invoke<string>("get_app_data_dir");
      const dataFile = `${dataDir}/project.json`;
      const content = JSON.stringify(data, null, 2);
      await writeTextFile(dataFile, content);
    } catch (error) {
      console.error("Failed to save data:", error);
      throw error;
    }
  }

  static async exportData(data: ProjectData): Promise<void> {
    try {
      const filePath = await save({
        title: "データをエクスポート",
        defaultPath: "project.json",
        filters: [
          { name: "JSON Files", extensions: ["json"] },
          { name: "All Files", extensions: ["*"] },
        ],
      });

      if (!filePath) {
        return; // User cancelled
      }

      const content = JSON.stringify(data, null, 2);
      await writeTextFile(filePath, content);
    } catch (error) {
      console.error("Export failed:", error);
      throw error;
    }
  }

  static async importData(): Promise<ProjectData | null> {
    try {
      const filePath = await open({
        title: "データをインポート",
        filters: [
          { name: "JSON Files", extensions: ["json"] },
          { name: "All Files", extensions: ["*"] },
        ],
        multiple: false,
      });

      if (!filePath || Array.isArray(filePath)) {
        return null; // User cancelled or multiple files selected
      }

      const content = await readTextFile(filePath);
      const data: ProjectData = JSON.parse(content);
      
      // Ensure the data structure is correct
      return {
        items: data.items.map((item: any) => ({
          id: item.id,
          title: item.title,
          description: item.description,
          status: item.status,
          priority: item.priority,
          dueDate: item.dueDate ?? item.due_date ?? null,
          createdAt: item.createdAt ?? item.created_at,
          updatedAt: item.updatedAt ?? item.updated_at,
          isCompleted: item.isCompleted ?? item.is_completed ?? false,
        })),
      };
    } catch (error) {
      console.error("Import failed:", error);
      return null;
    }
  }

  static async openDataFolder(): Promise<void> {
    try {
      const dataDir = await invoke<string>("get_app_data_dir");
      await openPath(dataDir);
    } catch (error) {
      console.error("Failed to open data folder:", error);
      throw error;
    }
  }
}

