import { ProjectData, ThemeData } from "./index";

export interface ElectronAPI {
  // Data operations
  loadData: () => Promise<ProjectData>;
  saveData: (data: ProjectData) => Promise<void>;
  loadTheme: () => Promise<ThemeData>;
  saveTheme: (data: ThemeData) => Promise<void>;

  // File operations
  exportData: (data: ProjectData) => Promise<void>;
  importData: () => Promise<ProjectData | null>;
  importFromPath: (filePath: string) => Promise<ProjectData | null>;
  openDataFolder: () => Promise<void>;

  // Window operations
  openDetailWindow: (itemId: number) => Promise<void>;
  getPathForFile: (file: File) => string;
  onDataChanged: (callback: () => void) => () => void;
  onThemeChanged: (callback: () => void) => () => void;
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type IpcEvent = any;
// eslint-disable-next-line @typescript-eslint/no-explicit-any
type IpcArgs = any[];

declare global {
  interface Window {
    electronAPI: ElectronAPI;
    ipcRenderer: {
      on: (channel: string, listener: (event: IpcEvent, ...args: IpcArgs) => void) => void;
      off: (channel: string, listener: (event: IpcEvent, ...args: IpcArgs) => void) => void;
      send: (channel: string, ...args: IpcArgs) => void;
      invoke: (channel: string, ...args: IpcArgs) => Promise<unknown>;
    };
  }
}
