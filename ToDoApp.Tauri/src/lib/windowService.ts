import { WebviewWindow } from "@tauri-apps/api/webviewWindow";

export async function openDetailWindow(itemId: number): Promise<void> {
  const label = `detail-${itemId}`;
  const existing = await WebviewWindow.getByLabel(label);
  if (existing) {
    await existing.setFocus();
    return;
  }

  const webview = new WebviewWindow(label, {
    url: `/?itemId=${itemId}`,
    title: `アイテム詳細 #${itemId}`,
    width: 520,
    height: 640,
    minWidth: 400,
    minHeight: 480,
    center: true,
  });

  webview.once("tauri://error", (e) => {
    console.error("Failed to create detail window:", e);
  });
}
