import { invoke } from "@tauri-apps/api/core";
import { defaultWindowIcon } from "@tauri-apps/api/app";
import { Menu } from "@tauri-apps/api/menu";
import { TrayIcon } from "@tauri-apps/api/tray";
import { getCurrentWindow } from "@tauri-apps/api/window";
import { DataService } from "./dataService";

/** Shared flag so close-to-tray can distinguish tray 「終了」. */
export let isQuitting = false;

export function setQuitting(value: boolean) {
  isQuitting = value;
}

async function showMainWindow() {
  const win = getCurrentWindow();
  await win.show();
  await win.unminimize();
  await win.setFocus();
}

async function quitApp() {
  setQuitting(true);
  try {
    const win = getCurrentWindow();
    const position = await win.outerPosition();
    const size = await win.outerSize();
    const scale = await win.scaleFactor();
    await DataService.saveWindowBounds({
      x: position.x / scale,
      y: position.y / scale,
      width: size.width / scale,
      height: size.height / scale,
    });
  } catch {
    // ignore save errors on quit
  }
  try {
    await invoke("quit_app");
  } catch {
    await getCurrentWindow().close();
  }
}

let trayInitialized = false;

/** Create system tray with 表示 / 終了. Safe to call once. */
export async function setupSystemTray(): Promise<void> {
  if (trayInitialized) return;
  trayInitialized = true;

  try {
    const menu = await Menu.new({
      items: [
        {
          id: "show",
          text: "表示",
          action: () => {
            void showMainWindow();
          },
        },
        {
          id: "quit",
          text: "終了",
          action: () => {
            void quitApp();
          },
        },
      ],
    });

    const icon = await defaultWindowIcon();
    await TrayIcon.new({
      icon: icon ?? undefined,
      tooltip: "Todo App",
      menu,
      menuOnLeftClick: false,
      action: (event) => {
        if (
          event.type === "Click" &&
          event.button === "Left" &&
          event.buttonState === "Up"
        ) {
          void showMainWindow();
        } else if (event.type === "DoubleClick") {
          void showMainWindow();
        }
      },
    });
  } catch (error) {
    console.error("Failed to setup system tray:", error);
    trayInitialized = false;
  }
}
