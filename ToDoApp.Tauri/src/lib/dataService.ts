import { invoke } from "@tauri-apps/api/core";
import { open, save } from "@tauri-apps/plugin-dialog";
import { readTextFile, writeTextFile } from "@tauri-apps/plugin-fs";
import { openPath } from "@tauri-apps/plugin-opener";
import type { ProjectData, ThemeData, TodoItem } from "../types";
import { runInBackground } from "./scheduleWork";

export interface WindowBounds {
  x: number;
  y: number;
  width: number;
  height: number;
}

function parseProjectData(raw: unknown): ProjectData {
  const data = raw as {
    items?: Array<Record<string, unknown>>;
  };
  if (!data || !Array.isArray(data.items)) {
    throw new Error("Invalid project data: missing items array");
  }

  return {
    items: data.items.map(
      (item): TodoItem => ({
        id: Number(item.id),
        title: String(item.title ?? ""),
        description: String(item.description ?? ""),
        status: (item.status as TodoItem["status"]) ?? "未着手",
        priority: (item.priority as TodoItem["priority"]) ?? "中",
        dueDate:
          (item.dueDate as string | null) ??
          (item.due_date as string | null) ??
          null,
        createdAt: String(
          item.createdAt ?? item.created_at ?? new Date().toISOString()
        ),
        updatedAt: String(
          item.updatedAt ?? item.updated_at ?? new Date().toISOString()
        ),
        isCompleted: Boolean(
          item.isCompleted ?? item.is_completed ?? false
        ),
      })
    ),
  };
}

function parseThemeData(raw: unknown): ThemeData {
  const data = raw as { theme?: string };
  if (data?.theme === "dark" || data?.theme === "light") {
    return { theme: data.theme };
  }
  return { theme: "light" };
}

async function getDataDir(): Promise<string> {
  return invoke<string>("get_app_data_dir");
}

export class DataService {
  static async loadData(): Promise<ProjectData> {
    try {
      const dataDir = await getDataDir();
      const dataFile = `${dataDir}/project.json`;

      try {
        const content = await readTextFile(dataFile);
        return parseProjectData(JSON.parse(content));
      } catch {
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
      const dataDir = await getDataDir();
      const dataFile = `${dataDir}/project.json`;
      const content = await runInBackground(() =>
        JSON.stringify(data, null, 2)
      );
      await writeTextFile(dataFile, content);
    } catch (error) {
      console.error("Failed to save data:", error);
      throw error;
    }
  }

  static async loadTheme(): Promise<ThemeData> {
    try {
      const dataDir = await getDataDir();
      const content = await readTextFile(`${dataDir}/theme.json`);
      return parseThemeData(JSON.parse(content));
    } catch {
      return { theme: "light" };
    }
  }

  static async saveTheme(data: ThemeData): Promise<void> {
    try {
      const dataDir = await getDataDir();
      await writeTextFile(
        `${dataDir}/theme.json`,
        JSON.stringify(data, null, 2)
      );
    } catch (error) {
      console.error("Failed to save theme:", error);
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

      const content = await runInBackground(() =>
        JSON.stringify(data, null, 2)
      );
      await writeTextFile(filePath, content);
    } catch (error) {
      console.error("Export failed:", error);
      throw error;
    }
  }

  static async importFromPath(filePath: string): Promise<ProjectData | null> {
    try {
      const content = await readTextFile(filePath);
      return parseProjectData(JSON.parse(content));
    } catch (error) {
      console.error("Import from path failed:", error);
      return null;
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

      return await DataService.importFromPath(filePath);
    } catch (error) {
      console.error("Import failed:", error);
      return null;
    }
  }

  static async openDataFolder(): Promise<void> {
    try {
      const dataDir = await getDataDir();
      await openPath(dataDir);
    } catch (error) {
      console.error("Failed to open data folder:", error);
      throw error;
    }
  }

  static async loadWindowBounds(): Promise<WindowBounds | null> {
    try {
      const dataDir = await getDataDir();
      const content = await readTextFile(`${dataDir}/window.json`);
      const bounds = JSON.parse(content) as WindowBounds;
      if (
        typeof bounds.x === "number" &&
        typeof bounds.y === "number" &&
        typeof bounds.width === "number" &&
        typeof bounds.height === "number"
      ) {
        return bounds;
      }
    } catch {
      // no saved bounds
    }
    return null;
  }

  static async saveWindowBounds(bounds: WindowBounds): Promise<void> {
    try {
      const dataDir = await getDataDir();
      await writeTextFile(
        `${dataDir}/window.json`,
        JSON.stringify(bounds, null, 2)
      );
    } catch (error) {
      console.error("Failed to save window bounds:", error);
    }
  }
}
