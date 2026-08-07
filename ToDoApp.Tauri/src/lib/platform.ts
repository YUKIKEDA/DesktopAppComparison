import {
  isPermissionGranted,
  requestPermission,
  sendNotification,
} from "@tauri-apps/plugin-notification";
import { writeText } from "@tauri-apps/plugin-clipboard-manager";

/** OS notification via Tauri notification plugin (fallback: Web Notification). */
export async function showNotification(
  title: string,
  body: string
): Promise<void> {
  try {
    let granted = await isPermissionGranted();
    if (!granted) {
      const permission = await requestPermission();
      granted = permission === "granted";
    }
    if (granted) {
      sendNotification({ title, body });
      return;
    }
  } catch {
    // fall through to Web Notification
  }

  try {
    if (typeof Notification !== "undefined") {
      if (Notification.permission === "granted") {
        new Notification(title, { body });
      } else if (Notification.permission !== "denied") {
        const permission = await Notification.requestPermission();
        if (permission === "granted") {
          new Notification(title, { body });
        }
      }
    }
  } catch (error) {
    console.error("Notification failed:", error);
  }
}

/** Copy text via Tauri clipboard plugin (fallback: navigator.clipboard). */
export async function copyText(text: string): Promise<void> {
  try {
    await writeText(text);
    return;
  } catch {
    // fall through
  }
  if (navigator.clipboard?.writeText) {
    await navigator.clipboard.writeText(text);
    return;
  }
  throw new Error("Clipboard API unavailable");
}
