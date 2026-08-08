import { listen } from "@tauri-apps/api/event";
import { invoke } from "@tauri-apps/api/core";
import { getCurrentWindow } from "@tauri-apps/api/window";
import { appDataDir, join } from "@tauri-apps/api/path";
import { readTextFile, remove } from "@tauri-apps/plugin-fs";
import { runUiBench } from "./lib/uiBench";
import { setQuitting } from "./lib/trayService";

type Payload = {
  outFile?: string | null;
  jsonPath?: string | null;
  processStartMs?: number;
};

let started = false;

async function runBench(
  jsonPath?: string | null,
  processStartMs?: number,
  outFile?: string | null
) {
  if (started) return;
  if (!jsonPath || !processStartMs) {
    console.error("UI bench missing jsonPath or processStartMs");
    return;
  }
  started = true;
  (window as unknown as { __uiBenchActive?: boolean }).__uiBenchActive = true;

  try {
    await runUiBench({
      processStartMs,
      jsonPath,
      writeResult: async (result) => {
        // Prefer Rust write (absolute paths outside appdata are reliable).
        try {
          await invoke("write_ui_bench_result", { result });
        } catch {
          if (outFile) {
            const { writeTextFile } = await import("@tauri-apps/plugin-fs");
            await writeTextFile(outFile, JSON.stringify(result));
          } else {
            throw new Error("write_ui_bench_result failed and no outFile");
          }
        }
      },
      quit: async () => {
        setQuitting(true);
        try {
          const dir = await appDataDir();
          const req = await join(dir, "ui_bench_request.json");
          await remove(req).catch(() => undefined);
        } catch {
          /* ignore */
        }
        try {
          await invoke("quit_app");
        } catch {
          await getCurrentWindow().close();
        }
      },
    });
  } catch (error) {
    console.error("UI bench failed:", error);
    started = false;
  }
}

async function tryStartFromRequestFile(): Promise<boolean> {
  try {
    const dir = await appDataDir();
    const reqPath = await join(dir, "ui_bench_request.json");
    const raw = await readTextFile(reqPath);
    const cfg = JSON.parse(raw) as {
      enabled?: boolean;
      outFile?: string | null;
      jsonPath?: string | null;
      processStartMs?: number;
    };
    if (!cfg?.enabled) return false;
    await runBench(cfg.jsonPath, cfg.processStartMs, cfg.outFile);
    return true;
  } catch {
    return false;
  }
}

/** Register before React mounts so StrictMode cannot drop the handler. */
export function installUiBenchListener(): void {
  void listen<Payload>("ui-bench-start", (event) => {
    void runBench(
      event.payload?.jsonPath,
      event.payload?.processStartMs,
      event.payload?.outFile
    );
  });

  const poll = async () => {
    if (started) return;
    await tryStartFromRequestFile();
  };
  void poll();
  window.setTimeout(() => void poll(), 500);
  window.setTimeout(() => void poll(), 1500);
  window.setTimeout(() => void poll(), 3000);
  window.setTimeout(() => void poll(), 5000);
  window.setTimeout(() => void poll(), 8000);
}
