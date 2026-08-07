import { ipcRenderer, contextBridge, webUtils } from "electron";
import type { ElectronAPI } from "../src/types/electron";

// --------- Expose some API to the Renderer process ---------
const electronAPI: ElectronAPI = {
  loadData: () => ipcRenderer.invoke("data:load"),
  saveData: (data) => ipcRenderer.invoke("data:save", data),
  loadTheme: () => ipcRenderer.invoke("theme:load"),
  saveTheme: (data) => ipcRenderer.invoke("theme:save", data),
  exportData: (data) => ipcRenderer.invoke("data:export", data),
  importData: () => ipcRenderer.invoke("data:import"),
  importFromPath: (filePath) =>
    ipcRenderer.invoke("data:importFromPath", filePath),
  openDataFolder: () => ipcRenderer.invoke("data:openFolder"),
  openDetailWindow: (itemId) =>
    ipcRenderer.invoke("window:openDetail", itemId),
  getPathForFile: (file) => {
    try {
      if (typeof webUtils?.getPathForFile === "function") {
        return webUtils.getPathForFile(file);
      }
    } catch {
      // fall through to File.path
    }
    return (file as File & { path?: string }).path ?? "";
  },
  onDataChanged: (callback) => {
    const listener = () => callback();
    ipcRenderer.on("data:changed", listener);
    return () => {
      ipcRenderer.removeListener("data:changed", listener);
    };
  },
  onThemeChanged: (callback) => {
    const listener = () => callback();
    ipcRenderer.on("theme:changed", listener);
    return () => {
      ipcRenderer.removeListener("theme:changed", listener);
    };
  },
};

contextBridge.exposeInMainWorld("electronAPI", electronAPI);

contextBridge.exposeInMainWorld("ipcRenderer", {
  on(...args: Parameters<typeof ipcRenderer.on>) {
    const [channel, listener] = args;
    return ipcRenderer.on(channel, (event, ...args) =>
      listener(event, ...args)
    );
  },
  off(...args: Parameters<typeof ipcRenderer.off>) {
    const [channel, ...omit] = args;
    return ipcRenderer.off(channel, ...omit);
  },
  send(...args: Parameters<typeof ipcRenderer.send>) {
    const [channel, ...omit] = args;
    return ipcRenderer.send(channel, ...omit);
  },
  invoke(...args: Parameters<typeof ipcRenderer.invoke>) {
    const [channel, ...omit] = args;
    return ipcRenderer.invoke(channel, ...omit);
  },
});
