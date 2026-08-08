import type { FilterConfig } from "../types";
import { useTodoStore } from "../store/useTodoStore";

const PHASE_MS = 5000;
const PAGE_SIZE = 100;

export type CpuBenchPhase = "idle" | "add" | "scroll" | "filter" | "done";

export interface CpuBenchDeps {
  writePhase: (phase: CpuBenchPhase) => Promise<void> | void;
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

async function runFor(
  ms: number,
  tick: () => void | Promise<void>
): Promise<void> {
  const deadline = performance.now() + ms;
  while (performance.now() < deadline) {
    await tick();
  }
}

/** CPU bench: idle → add → scroll → filter → done (phase written before each body). */
export async function runCpuBench(deps: CpuBenchDeps): Promise<void> {
  const { writePhase, quit } = deps;

  await writePhase("idle");
  await sleep(PHASE_MS);

  await writePhase("add");
  let n = 0;
  await runFor(PHASE_MS, async () => {
    useTodoStore.getState().addItem({
      title: `bench-${n}`,
      description: "",
      status: "未着手",
      priority: "中",
      dueDate: null,
      isCompleted: false,
    });
    n += 1;
    await yieldToUi();
  });

  await writePhase("scroll");
  await runFor(PHASE_MS, async () => {
    const { visibleCount, items, loadMoreVisible, resetVisibleCount } =
      useTodoStore.getState();
    if (visibleCount >= items.length) {
      resetVisibleCount();
    } else {
      loadMoreVisible(PAGE_SIZE);
    }
    await yieldToUi();
  });

  await writePhase("filter");
  let on = false;
  await runFor(PHASE_MS, async () => {
    const { setFilters } = useTodoStore.getState();
    if (on) {
      const filters: FilterConfig[] = [
        { columnId: "title", type: "text", value: "bench" },
        { columnId: "status", type: "select", value: ["未着手"] },
      ];
      setFilters(filters);
    } else {
      setFilters([]);
    }
    on = !on;
    await yieldToUi();
  });

  await writePhase("done");
  await quit();
}
