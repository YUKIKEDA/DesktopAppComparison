/** OS notification via Electron main process (fallback: Web Notification). */
export async function showNotification(
  title: string,
  body: string
): Promise<void> {
  try {
    if (window.electronAPI?.showNotification) {
      await window.electronAPI.showNotification(title, body);
      return;
    }
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

/** Copy text to the system clipboard. */
export async function copyText(text: string): Promise<void> {
  if (navigator.clipboard?.writeText) {
    await navigator.clipboard.writeText(text);
    return;
  }
  throw new Error("Clipboard API unavailable");
}
