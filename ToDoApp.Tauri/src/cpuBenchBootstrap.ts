import { listen } from "@tauri-apps/api/event";
import { invoke } from "@tauri-apps/api/core";
import { getCurrentWindow } from "@tauri-apps/api/window";
import { appDataDir, join } from "@tauri-apps/api/path";
import { readTextFile, writeTextFile, remove } from "@tauri-apps/plugin-fs";
import { runCpuBench } from "./lib/cpuBench";
import { useTodoStore } from "./store/useTodoStore";
import { DataService } from "./lib/dataService";
import { setQuitting } from "./lib/trayService";

type Payload = { jsonPath?: string | null; phaseFile?: string | null };

let started = false;

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function runBench(jsonPath?: string | null, phaseFile?: string | null) {
  if (started) return;
  const w = window as unknown as { __uiBenchActive?: boolean };
  if (w.__uiBenchActive) return;
  started = true;
  (window as unknown as { __cpuBenchActive?: boolean }).__cpuBenchActive = true;

  try {
    if (phaseFile) {
      await writeTextFile(phaseFile, "event\n");
    }

    while (useTodoStore.getState().isLoading) {
      await sleep(50);
    }

    if (jsonPath) {
      try {
        const data = await DataService.importFromPath(jsonPath);
        if (data) {
          useTodoStore.getState().setItems(data.items);
        }
      } catch (error) {
        console.error("CPU bench import failed:", error);
      }
    }

    await new Promise<void>((resolve) => {
      requestAnimationFrame(() => resolve());
    });

    await runCpuBench({
      writePhase: async (phase) => {
        if (phaseFile) {
          await writeTextFile(phaseFile, `${phase}\n`);
        } else {
          await invoke("write_cpu_bench_phase", { phase });
        }
      },
      quit: async () => {
        setQuitting(true);
        try {
          const dir = await appDataDir();
          const req = await join(dir, "cpu_bench_request.json");
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
    console.error("CPU bench failed:", error);
    started = false;
  }
}

async function tryStartFromRequestFile(): Promise<boolean> {
  try {
    const dir = await appDataDir();
    const reqPath = await join(dir, "cpu_bench_request.json");
    const raw = await readTextFile(reqPath);
    const cfg = JSON.parse(raw) as {
      enabled?: boolean;
      phaseFile?: string | null;
      jsonPath?: string | null;
    };
    if (!cfg?.enabled) return false;
    await runBench(cfg.jsonPath, cfg.phaseFile);
    return true;
  } catch {
    return false;
  }
}

/** Register before React mounts so StrictMode cannot drop the handler. */
export function installCpuBenchListener(): void {
  // Prove the new bundle loaded by touching AppData.
  void (async () => {
    try {
      const dir = await appDataDir();
      await writeTextFile(
        await join(dir, "cpu_bench_frontend_alive.txt"),
        `alive ${new Date().toISOString()}\n`
      );
    } catch (error) {
      console.error("cpu-bench alive marker failed:", error);
    }
  })();

  void listen<Payload>("cpu-bench-start", (event) => {
    void runBench(event.payload?.jsonPath, event.payload?.phaseFile);
  });

  // Poll request file — works even when emit/listen or invoke ACL misbehaves.
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
