import {
  app,
  BrowserWindow,
  ipcMain,
  dialog,
  shell,
  screen,
  Tray,
  Menu,
  Notification,
  nativeImage,
} from "electron";
import { fileURLToPath } from "node:url";
import path from "node:path";
import fs from "node:fs/promises";
import type { ProjectData, ThemeData, TodoItem } from "../src/types/index";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

// Data directory
const DATA_DIR = path.join(app.getPath("userData"), "data");
const DATA_FILE = path.join(DATA_DIR, "project.json");
const THEME_FILE = path.join(DATA_DIR, "theme.json");
const WINDOW_FILE = path.join(DATA_DIR, "window.json");

interface WindowBounds {
  x: number;
  y: number;
  width: number;
  height: number;
}

// The built directory structure
//
// ├─┬─┬ dist
// │ │ └── index.html
// │ │
// │ ├─┬ dist-electron
// │ │ ├── main.js
// │ │ └── preload.mjs
// │
process.env.APP_ROOT = path.join(__dirname, "..");

// 🚧 Use ['ENV_NAME'] avoid vite:define plugin - Vite@2.x
export const VITE_DEV_SERVER_URL = process.env["VITE_DEV_SERVER_URL"];
export const MAIN_DIST = path.join(process.env.APP_ROOT, "dist-electron");
export const RENDERER_DIST = path.join(process.env.APP_ROOT, "dist");

process.env.VITE_PUBLIC = VITE_DEV_SERVER_URL
  ? path.join(process.env.APP_ROOT, "public")
  : RENDERER_DIST;

let win: BrowserWindow | null = null;
let tray: Tray | null = null;
let isQuitting = false;
const pendingOpenFiles: string[] = [];

interface CpuBenchConfig {
  enabled: boolean;
  phaseFile: string | null;
  jsonPath: string | null;
}

function parseCpuBenchArgs(argv: string[]): CpuBenchConfig {
  let enabled = false;
  let phaseFile: string | null = null;
  for (const arg of argv) {
    if (!arg) continue;
    if (arg === "--cpu-bench") {
      enabled = true;
    } else if (arg.startsWith("--cpu-bench-phase=")) {
      phaseFile = arg.slice("--cpu-bench-phase=".length);
    }
  }
  return {
    enabled,
    phaseFile,
    jsonPath: findJsonFromArgv(argv),
  };
}

let cpuBenchConfig: CpuBenchConfig = {
  enabled: false,
  phaseFile: null,
  jsonPath: null,
};

function parseProjectData(content: string): ProjectData {
  const data = JSON.parse(content);
  if (!data || !Array.isArray(data.items)) {
    throw new Error("Invalid project data: missing items array");
  }
  return {
    items: data.items.map(
      (item: Record<string, unknown>): TodoItem => ({
        id: Number(item.id),
        title: String(item.title ?? ""),
        description: String(item.description ?? ""),
        status: (item.status as TodoItem["status"]) ?? "未着手",
        priority: (item.priority as TodoItem["priority"]) ?? "中",
        dueDate: (item.dueDate as string | null) ?? (item.due_date as string | null) ?? null,
        createdAt: String(item.createdAt ?? item.created_at ?? new Date().toISOString()),
        updatedAt: String(item.updatedAt ?? item.updated_at ?? new Date().toISOString()),
        isCompleted: Boolean(item.isCompleted ?? item.is_completed ?? false),
      })
    ),
  };
}

async function importFromPath(filePath: string): Promise<ProjectData | null> {
  try {
    const content = await fs.readFile(filePath, "utf-8");
    return parseProjectData(content);
  } catch (error) {
    console.error("Error importing from path:", error);
    return null;
  }
}

async function loadWindowBounds(): Promise<WindowBounds | null> {
  try {
    const content = await fs.readFile(WINDOW_FILE, "utf-8");
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

async function saveWindowBounds(bounds: WindowBounds): Promise<void> {
  try {
    await fs.mkdir(DATA_DIR, { recursive: true });
    await fs.writeFile(WINDOW_FILE, JSON.stringify(bounds, null, 2), "utf-8");
  } catch (error) {
    console.error("Error saving window bounds:", error);
  }
}

function isBoundsOnScreen(bounds: WindowBounds): boolean {
  const displays = screen.getAllDisplays();
  return displays.some((display) => {
    const a = display.workArea;
    return (
      bounds.x + bounds.width > a.x &&
      bounds.x < a.x + a.width &&
      bounds.y + bounds.height > a.y &&
      bounds.y < a.y + a.height
    );
  });
}

function loadRenderer(target: BrowserWindow, query?: Record<string, string>) {
  if (VITE_DEV_SERVER_URL) {
    const url = new URL(VITE_DEV_SERVER_URL);
    if (query) {
      for (const [key, value] of Object.entries(query)) {
        url.searchParams.set(key, value);
      }
    }
    target.loadURL(url.toString());
  } else {
    target.loadFile(path.join(RENDERER_DIST, "index.html"), {
      query: query ?? {},
    });
  }
}

function broadcastDataChanged() {
  for (const window of BrowserWindow.getAllWindows()) {
    window.webContents.send("data:changed");
  }
}

function broadcastThemeChanged() {
  for (const window of BrowserWindow.getAllWindows()) {
    window.webContents.send("theme:changed");
  }
}

function parseThemeData(content: string): ThemeData {
  const data = JSON.parse(content) as { theme?: string };
  if (data?.theme === "dark" || data?.theme === "light") {
    return { theme: data.theme };
  }
  return { theme: "light" };
}

function findJsonFromArgv(argv: string[]): string | null {
  for (const arg of argv) {
    if (!arg || arg.startsWith("-")) continue;
    const lower = arg.toLowerCase();
    if (lower.endsWith(".json") && !lower.includes("package.json")) {
      return arg;
    }
  }
  return null;
}

function persistMainWindowBounds() {
  if (!win || win.isDestroyed()) return;
  const bounds = win.getBounds();
  void saveWindowBounds({
    x: bounds.x,
    y: bounds.y,
    width: bounds.width,
    height: bounds.height,
  });
}

function showMainWindow() {
  if (!win || win.isDestroyed()) {
    void createWindow();
    return;
  }
  if (win.isMinimized()) win.restore();
  win.show();
  win.focus();
}

function sendOpenFile(filePath: string) {
  if (win && !win.isDestroyed() && !win.webContents.isLoading()) {
    win.webContents.send("app:open-file", filePath);
  } else {
    pendingOpenFiles.push(filePath);
  }
}

function flushPendingOpenFiles() {
  if (!win || win.isDestroyed()) return;
  while (pendingOpenFiles.length > 0) {
    const filePath = pendingOpenFiles.shift();
    if (filePath) {
      win.webContents.send("app:open-file", filePath);
    }
  }
}

function getTrayIcon() {
  const pngPath = path.join(process.env.VITE_PUBLIC!, "icon.png");
  const icoPath = path.join(process.env.VITE_PUBLIC!, "icon.ico");
  const png = nativeImage.createFromPath(pngPath);
  if (!png.isEmpty()) return png;
  const ico = nativeImage.createFromPath(icoPath);
  if (!ico.isEmpty()) return ico;
  return nativeImage.createEmpty();
}

function createTray() {
  if (tray) return;
  tray = new Tray(getTrayIcon());
  tray.setToolTip("Todo App");
  tray.setContextMenu(
    Menu.buildFromTemplate([
      {
        label: "表示",
        click: () => showMainWindow(),
      },
      {
        label: "終了",
        click: () => {
          isQuitting = true;
          persistMainWindowBounds();
          app.quit();
        },
      },
    ])
  );
  tray.on("click", () => showMainWindow());
  tray.on("double-click", () => showMainWindow());
}

function createDetailWindow(itemId: number) {
  const detailWin = new BrowserWindow({
    width: 520,
    height: 640,
    minWidth: 400,
    minHeight: 480,
    title: "アイテム詳細",
    opacity: 0.95,
    icon: path.join(process.env.VITE_PUBLIC!, "electron-vite.svg"),
    webPreferences: {
      preload: path.join(__dirname, "preload.mjs"),
      nodeIntegration: false,
      contextIsolation: true,
    },
  });

  loadRenderer(detailWin, { itemId: String(itemId) });
}

// IPC Handlers
async function setupIpcHandlers() {
  // Load data
  ipcMain.handle("data:load", async (): Promise<ProjectData> => {
    try {
      await fs.mkdir(DATA_DIR, { recursive: true });
      const data = await fs.readFile(DATA_FILE, "utf-8").catch(() => null);
      if (data) {
        return parseProjectData(data);
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
        broadcastDataChanged();
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

  // Import data (file dialog)
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
      return importFromPath(result.filePaths[0]);
    }
    return null;
  });

  // Import from explicit path (shared parse with data:import)
  ipcMain.handle(
    "data:importFromPath",
    async (_event, filePath: string): Promise<ProjectData | null> => {
      if (!filePath || typeof filePath !== "string") {
        return null;
      }
      return importFromPath(filePath);
    }
  );

  // Open data folder
  ipcMain.handle("data:openFolder", async (): Promise<void> => {
    try {
      await fs.mkdir(DATA_DIR, { recursive: true });
      await shell.openPath(DATA_DIR);
    } catch (error) {
      console.error("Error opening data folder:", error);
      throw error;
    }
  });

  // Open detail window for a single item
  ipcMain.handle(
    "window:openDetail",
    async (_event, itemId: number): Promise<void> => {
      createDetailWindow(itemId);
    }
  );

  // Load theme
  ipcMain.handle("theme:load", async (): Promise<ThemeData> => {
    try {
      await fs.mkdir(DATA_DIR, { recursive: true });
      const content = await fs.readFile(THEME_FILE, "utf-8").catch(() => null);
      if (content) {
        return parseThemeData(content);
      }
      return { theme: "light" };
    } catch (error) {
      console.error("Error loading theme:", error);
      return { theme: "light" };
    }
  });

  // Save theme
  ipcMain.handle(
    "theme:save",
    async (_event, data: ThemeData): Promise<void> => {
      try {
        await fs.mkdir(DATA_DIR, { recursive: true });
        const theme: ThemeData =
          data?.theme === "dark" ? { theme: "dark" } : { theme: "light" };
        await fs.writeFile(
          THEME_FILE,
          JSON.stringify(theme, null, 2),
          "utf-8"
        );
        broadcastThemeChanged();
      } catch (error) {
        console.error("Error saving theme:", error);
        throw error;
      }
    }
  );

  // OS notification
  ipcMain.handle(
    "app:notify",
    async (_event, payload: { title: string; body: string }): Promise<void> => {
      if (!Notification.isSupported()) return;
      new Notification({
        title: payload?.title || "Todo App",
        body: payload?.body || "",
      }).show();
    }
  );

  // CPU bench
  ipcMain.handle("cpu-bench:getConfig", async (): Promise<CpuBenchConfig> => {
    return cpuBenchConfig;
  });

  ipcMain.handle(
    "cpu-bench:writePhase",
    async (_event, phase: string): Promise<void> => {
      if (!cpuBenchConfig.phaseFile || typeof phase !== "string") return;
      await fs.writeFile(cpuBenchConfig.phaseFile, `${phase}\n`, "utf-8");
    }
  );

  ipcMain.handle("cpu-bench:quit", async (): Promise<void> => {
    isQuitting = true;
    app.quit();
  });
}

async function createWindow() {
  const saved = await loadWindowBounds();
  const defaults = { width: 1400, height: 900, x: undefined as number | undefined, y: undefined as number | undefined };
  const options: Electron.BrowserWindowConstructorOptions = {
    width: defaults.width,
    height: defaults.height,
    minWidth: 800,
    minHeight: 600,
    title: "Todo App",
    opacity: 0.95,
    icon: path.join(process.env.VITE_PUBLIC!, "electron-vite.svg"),
    webPreferences: {
      preload: path.join(__dirname, "preload.mjs"),
      nodeIntegration: false,
      contextIsolation: true,
    },
  };

  if (saved && isBoundsOnScreen(saved)) {
    options.x = saved.x;
    options.y = saved.y;
    options.width = Math.max(saved.width, 800);
    options.height = Math.max(saved.height, 600);
  }

  win = new BrowserWindow(options);

  win.on("close", (event) => {
    if (!win) return;
    persistMainWindowBounds();
    if (!isQuitting) {
      event.preventDefault();
      win.hide();
    }
  });

  win.webContents.on("did-finish-load", () => {
    flushPendingOpenFiles();
  });

  loadRenderer(win);
}

// Single instance: focus existing window and forward .json path
const gotTheLock = app.requestSingleInstanceLock();
if (!gotTheLock) {
  app.quit();
} else {
  app.on("second-instance", (_event, commandLine) => {
    showMainWindow();
    const jsonPath = findJsonFromArgv(commandLine);
    if (jsonPath) {
      sendOpenFile(jsonPath);
    }
  });

  // macOS: open file via Finder / file association
  app.on("open-file", (event, filePath) => {
    event.preventDefault();
    if (filePath.toLowerCase().endsWith(".json")) {
      if (app.isReady()) {
        sendOpenFile(filePath);
      } else {
        pendingOpenFiles.push(filePath);
      }
    }
  });

  // Quit when all windows are closed, except on macOS / when hidden to tray
  app.on("window-all-closed", () => {
    if (isQuitting && process.platform !== "darwin") {
      app.quit();
      win = null;
    }
  });

  app.on("before-quit", () => {
    isQuitting = true;
  });

  app.on("activate", () => {
    showMainWindow();
  });

  app.whenReady().then(() => {
    if (process.platform === "win32") {
      app.setAppUserModelId("com.yuuuu.todoapp-electron");
    }
    cpuBenchConfig = parseCpuBenchArgs(process.argv);
    setupIpcHandlers();
    createTray();
    void createWindow().then(() => {
      // When cpu-bench owns the json import path, renderer imports via getCpuBenchConfig
      if (cpuBenchConfig.enabled) return;
      const jsonPath = findJsonFromArgv(process.argv);
      if (jsonPath) {
        sendOpenFile(jsonPath);
      }
    });
  });
}
