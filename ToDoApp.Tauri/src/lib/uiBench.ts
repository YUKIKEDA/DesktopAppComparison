import type { FilterConfig } from "../types";
import { useTodoStore } from "../store/useTodoStore";
import { DataService } from "./dataService";
import { runInBackground } from "./scheduleWork";

const SCROLL_MS = 3000;
const PAGE_SIZE = 100;
const FILTER_CYCLES = 10;

const BENCH_FILTERS: FilterConfig[] = [
  { columnId: "title", type: "text", value: "bench" },
  { columnId: "status", type: "select", value: ["未着手"] },
];

export interface UiBenchResult {
  startup_s: number;
  render_1000_s: number;
  scroll_fps: number;
  filter_response_ms: number;
}

export interface UiBenchDeps {
  processStartMs: number;
  jsonPath: string;
  writeResult: (result: UiBenchResult) => Promise<void> | void;
  quit: () => Promise<void> | void;
}

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function yieldToUi(): Promise<void> {
  return new Promise((resolve) => {
    requestAnimationFrame(() => resolve());
  });
}

/** TodoTable applies filters via scheduleWork — wait for that path after setFilters. */
async function waitAfterSetFilters(): Promise<void> {
  await yieldToUi();
  await runInBackground(() => undefined);
}

async function waitForLoadingDone(): Promise<void> {
  while (useTodoStore.getState().isLoading) {
    await sleep(50);
  }
}

async function measureStartupS(processStartMs: number): Promise<number> {
  await waitForLoadingDone();
  await yieldToUi();
  return (Date.now() - processStartMs) / 1000;
}

async function measureRender1000S(jsonPath: string): Promise<number> {
  const t0 = performance.now();
  const data = await DataService.importFromPath(jsonPath);
  if (data) {
    useTodoStore.getState().setItems(data.items);
  }
  await yieldToUi();
  return (performance.now() - t0) / 1000;
}

async function measureScrollFps(): Promise<number> {
  let frames = 0;
  const start = performance.now();

  return new Promise((resolve) => {
    const tick = () => {
      frames += 1;
      const elapsed = performance.now() - start;
      if (elapsed >= SCROLL_MS) {
        resolve(frames / (SCROLL_MS / 1000));
        return;
      }
      const { visibleCount, items, loadMoreVisible, resetVisibleCount } =
        useTodoStore.getState();
      if (visibleCount >= items.length) {
        resetVisibleCount();
      } else {
        loadMoreVisible(PAGE_SIZE);
      }
      requestAnimationFrame(tick);
    };
    requestAnimationFrame(tick);
  });
}

async function measureFilterResponseMs(): Promise<number> {
  const times: number[] = [];
  let on = false;

  for (let i = 0; i < FILTER_CYCLES; i += 1) {
    const t0 = performance.now();
    const { setFilters } = useTodoStore.getState();
    if (on) {
      setFilters(BENCH_FILTERS);
    } else {
      setFilters([]);
    }
    on = !on;
    await waitAfterSetFilters();
    times.push(performance.now() - t0);
  }

  return times.reduce((sum, t) => sum + t, 0) / times.length;
}

/** UI bench: startup → render 1000 → scroll fps → filter response → write result & quit. */
export async function runUiBench(deps: UiBenchDeps): Promise<void> {
  const { processStartMs, jsonPath, writeResult, quit } = deps;

  const startup_s = await measureStartupS(processStartMs);
  const render_1000_s = await measureRender1000S(jsonPath);
  const scroll_fps = await measureScrollFps();
  const filter_response_ms = await measureFilterResponseMs();

  await writeResult({
    startup_s,
    render_1000_s,
    scroll_fps,
    filter_response_ms,
  });
  await quit();
}
