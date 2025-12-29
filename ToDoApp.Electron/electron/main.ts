import { app, BrowserWindow, ipcMain, dialog, shell } from "electron";
import { fileURLToPath } from "node:url";
import path from "node:path";
import fs from "node:fs/promises";
import type { ProjectData } from "../src/types/index";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

// Data directory
const DATA_DIR = path.join(app.getPath("userData"), "data");
const DATA_FILE = path.join(DATA_DIR, "project.json");

// The built directory structure
//
// ├─┬─┬ dist
// │ │ └── index.html
// │ │
// │ ├─┬ dist-electron
// │ │ ├── main.js
// │ │ └── preload.mjs
// │
process.env.APP_ROOT = path.join(__dirname, '..')

// 🚧 Use ['ENV_NAME'] avoid vite:define plugin - Vite@2.x
export const VITE_DEV_SERVER_URL = process.env['VITE_DEV_SERVER_URL']
export const MAIN_DIST = path.join(process.env.APP_ROOT, 'dist-electron')
export const RENDERER_DIST = path.join(process.env.APP_ROOT, 'dist')

process.env.VITE_PUBLIC = VITE_DEV_SERVER_URL ? path.join(process.env.APP_ROOT, 'public') : RENDERER_DIST

let win: BrowserWindow | null

// IPC Handlers
async function setupIpcHandlers() {
  // Load data
  ipcMain.handle("data:load", async (): Promise<ProjectData> => {
    try {
      await fs.mkdir(DATA_DIR, { recursive: true });
      const data = await fs.readFile(DATA_FILE, "utf-8").catch(() => null);
      if (data) {
        return JSON.parse(data);
      }
      return { items: [] };
    } catch (error) {
      console.error("Error loading data:", error);
      return { items: [] };
    }
  });

  // Save data
  ipcMain.handle(
    "data:save",
    async (_event, data: ProjectData): Promise<void> => {
      try {
        await fs.mkdir(DATA_DIR, { recursive: true });
        await fs.writeFile(DATA_FILE, JSON.stringify(data, null, 2), "utf-8");
      } catch (error) {
        console.error("Error saving data:", error);
        throw error;
      }
    }
  );

  // Export data
  ipcMain.handle(
    "data:export",
    async (_event, data: ProjectData): Promise<void> => {
      const result = await dialog.showSaveDialog(win!, {
        title: "データをエクスポート",
        defaultPath: "project.json",
        filters: [
          { name: "JSON Files", extensions: ["json"] },
          { name: "All Files", extensions: ["*"] },
        ],
      });

      if (!result.canceled && result.filePath) {
        await fs.writeFile(
          result.filePath,
          JSON.stringify(data, null, 2),
          "utf-8"
        );
      }
    }
  );

  // Import data
  ipcMain.handle("data:import", async (): Promise<ProjectData | null> => {
    const result = await dialog.showOpenDialog(win!, {
      title: "データをインポート",
      filters: [
        { name: "JSON Files", extensions: ["json"] },
        { name: "All Files", extensions: ["*"] },
      ],
      properties: ["openFile"],
    });

    if (!result.canceled && result.filePaths.length > 0) {
      try {
        const data = await fs.readFile(result.filePaths[0], "utf-8");
        return JSON.parse(data);
      } catch (error) {
        console.error("Error importing data:", error);
        return null;
      }
    }
    return null;
  });

  // Open data folder
  ipcMain.handle("data:openFolder", async (): Promise<void> => {
    try {
      // フォルダが存在しない場合は作成
      await fs.mkdir(DATA_DIR, { recursive: true });
      await shell.openPath(DATA_DIR);
    } catch (error) {
      console.error("Error opening data folder:", error);
      throw error;
    }
  });
}

function createWindow() {
  win = new BrowserWindow({
    width: 1400,
    height: 900,
    minWidth: 800,
    minHeight: 600,
    title: 'Todo App',
    icon: path.join(process.env.VITE_PUBLIC, 'electron-vite.svg'),
    webPreferences: {
      preload: path.join(__dirname, 'preload.mjs'),
      nodeIntegration: false,
      contextIsolation: true,
    },
  })

  if (VITE_DEV_SERVER_URL) {
    win.loadURL(VITE_DEV_SERVER_URL)
  } else {
    win.loadFile(path.join(RENDERER_DIST, 'index.html'))
  }
}

// Quit when all windows are closed, except on macOS. There, it's common
// for applications and their menu bar to stay active until the user quits
// explicitly with Cmd + Q.
app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit()
    win = null
  }
})

app.on('activate', () => {
  // On OS X it's common to re-create a window in the app when the
  // dock icon is clicked and there are no other windows open.
  if (BrowserWindow.getAllWindows().length === 0) {
    createWindow()
  }
})

app.whenReady().then(() => {
  setupIpcHandlers()
  createWindow()
})
