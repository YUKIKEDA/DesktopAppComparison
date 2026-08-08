import { useEffect, useState, useCallback, useRef } from "react";
import { listen, emit } from "@tauri-apps/api/event";
import { getCurrentWindow } from "@tauri-apps/api/window";
import { invoke } from "@tauri-apps/api/core";
import { useTodoStore } from "./store/useTodoStore";
import { DataService } from "./lib/dataService";
import { applyThemeClass } from "./lib/utils";
import { showNotification } from "./lib/platform";
import { isQuitting, setupSystemTray } from "./lib/trayService";
import { Toolbar } from "./components/Toolbar";
import { FilterBar } from "./components/FilterBar";
import { TodoTable } from "./components/TodoTable";
import { Dialog } from "./components/ui/Dialog";
import { TodoForm } from "./components/TodoForm";
import { DetailWindow } from "./components/DetailWindow";
import type { TodoItem } from "./types";
import "./App.css";

function getDetailItemId(): number | null {
  const params = new URLSearchParams(window.location.search);
  const raw = params.get("itemId");
  if (!raw) return null;
  const id = Number(raw);
  return Number.isFinite(id) ? id : null;
}

function useThemeBootstrap() {
  const setTheme = useTodoStore((s) => s.setTheme);

  useEffect(() => {
    const apply = async () => {
      try {
        const data = await DataService.loadTheme();
        setTheme(data.theme);
        applyThemeClass(data.theme);
      } catch (error) {
        console.error("Failed to load theme:", error);
      }
    };
    void apply();
  }, [setTheme]);

  useEffect(() => {
    let unlisten: (() => void) | undefined;
    void listen("theme-changed", async () => {
      try {
        const data = await DataService.loadTheme();
        setTheme(data.theme);
        applyThemeClass(data.theme);
      } catch (error) {
        console.error("Failed to reload theme:", error);
      }
    }).then((fn) => {
      unlisten = fn;
    });
    return () => {
      unlisten?.();
    };
  }, [setTheme]);
}

function MainApp() {
  const { items, setItems, setLoading, addItem, updateItem } = useTodoStore();
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<TodoItem | null>(null);
  const [isDragOver, setIsDragOver] = useState(false);
  const skipSaveRef = useRef(false);
  const cpuBenchSkipSaveRef = useRef(false);
  const opacityAppliedRef = useRef(false);

  const handleEdit = (item: TodoItem | null) => {
    setEditingItem(item);
    setIsDialogOpen(true);
  };

  const handleSave = async (data: {
    title: string;
    description?: string;
    status: "未着手" | "進行中" | "完了";
    priority: "低" | "中" | "高";
    dueDate: string | null;
  }) => {
    const itemData = {
      title: data.title,
      description: data.description || "",
      status: data.status,
      priority: data.priority,
      dueDate: data.dueDate ?? null,
      isCompleted: editingItem ? editingItem.isCompleted : false,
    };

    if (editingItem) {
      updateItem(editingItem.id, itemData);
    } else {
      addItem(itemData);
    }
    setIsDialogOpen(false);
    setEditingItem(null);
  };

  const applyImportedData = useCallback(
    async (data: { items: TodoItem[] } | null) => {
      if (!data) return;
      setItems(data.items);
      await DataService.saveData({ items: data.items });
      await emit("data-changed");
      await showNotification("Todo App", "インポートしました");
    },
    [setItems]
  );

  // Load data on mount
  useEffect(() => {
    const loadData = async () => {
      setLoading(true);
      try {
        const data = await DataService.loadData();
        skipSaveRef.current = true;
        setItems(data.items);
      } catch (error) {
        console.error("Failed to load data:", error);
      } finally {
        setLoading(false);
      }
    };
    void loadData();
  }, [setItems, setLoading]);

  // System tray
  useEffect(() => {
    void setupSystemTray();
  }, []);

  // Transparency: prefer real window opacity; CSS alpha fallback otherwise
  useEffect(() => {
    if (opacityAppliedRef.current) return;
    opacityAppliedRef.current = true;

    void (async () => {
      try {
        const applied = await invoke<boolean>("set_window_opacity", {
          opacity: 0.95,
        });
        if (!applied) {
          const theme = useTodoStore.getState().theme;
          const color =
            theme === "dark"
              ? "rgba(17, 24, 39, 0.95)"
              : "rgba(249, 250, 251, 0.95)";
          document.documentElement.style.backgroundColor = color;
          document.body.style.backgroundColor = color;
        }
      } catch {
        const theme = useTodoStore.getState().theme;
        const color =
          theme === "dark"
            ? "rgba(17, 24, 39, 0.95)"
            : "rgba(249, 250, 251, 0.95)";
        document.documentElement.style.backgroundColor = color;
        document.body.style.backgroundColor = color;
      }
    })();
  }, []);

  // Close-to-tray: hide instead of quit (終了 from tray sets isQuitting)
  useEffect(() => {
    const appWindow = getCurrentWindow();
    let unlisten: (() => void) | undefined;

    void appWindow
      .onCloseRequested(async (event) => {
        try {
          const position = await appWindow.outerPosition();
          const size = await appWindow.outerSize();
          const scale = await appWindow.scaleFactor();
          await DataService.saveWindowBounds({
            x: position.x / scale,
            y: position.y / scale,
            width: size.width / scale,
            height: size.height / scale,
          });
        } catch (error) {
          console.error("Failed to save window bounds:", error);
        }

        if (!isQuitting) {
          event.preventDefault();
          try {
            await appWindow.hide();
          } catch (error) {
            console.error("Failed to hide window:", error);
          }
        }
      })
      .then((fn) => {
        unlisten = fn;
      });

    return () => {
      unlisten?.();
    };
  }, []);

  // Drag & drop via Tauri window events (gives file paths)
  useEffect(() => {
    const appWindow = getCurrentWindow();
    let unlisten: (() => void) | undefined;

    void appWindow
      .onDragDropEvent(async (event) => {
        if (event.payload.type === "over" || event.payload.type === "enter") {
          setIsDragOver(true);
        } else if (event.payload.type === "leave") {
          setIsDragOver(false);
        } else if (event.payload.type === "drop") {
          setIsDragOver(false);
          const jsonPath = event.payload.paths.find((p) =>
            p.toLowerCase().endsWith(".json")
          );
          if (!jsonPath) return;
          try {
            const data = await DataService.importFromPath(jsonPath);
            await applyImportedData(data);
          } catch (error) {
            console.error("Drop import failed:", error);
          }
        }
      })
      .then((fn) => {
        unlisten = fn;
      });

    return () => {
      unlisten?.();
    };
  }, [applyImportedData]);

  // File association / CLI args / Opened event → importFromPath
  useEffect(() => {
    let unlisten: (() => void) | undefined;
    void listen<string>("open-file", async (event) => {
      if (cpuBenchSkipSaveRef.current) return;
      const filePath = event.payload;
      if (!filePath || typeof filePath !== "string") return;
      try {
        const data = await DataService.importFromPath(filePath);
        await applyImportedData(data);
      } catch (error) {
        console.error("Open-file import failed:", error);
      }
    }).then((fn) => {
      unlisten = fn;
    });
    return () => {
      unlisten?.();
    };
  }, [applyImportedData]);

  // Sync when another window saves
  useEffect(() => {
    let unlisten: (() => void) | undefined;
    void listen("data-changed", async () => {
      try {
        const data = await DataService.loadData();
        skipSaveRef.current = true;
        setItems(data.items);
      } catch (error) {
        console.error("Failed to reload data:", error);
      }
    }).then((fn) => {
      unlisten = fn;
    });
    return () => {
      unlisten?.();
    };
  }, [setItems]);

  // Auto-save with debounce
  useEffect(() => {
    if ((window as unknown as { __cpuBenchActive?: boolean }).__cpuBenchActive) {
      return;
    }
    if (cpuBenchSkipSaveRef.current) return;
    if (skipSaveRef.current) {
      skipSaveRef.current = false;
      return;
    }
    if (items.length === 0) return;

    const timeoutId = setTimeout(async () => {
      try {
        await DataService.saveData({ items });
        await emit("data-changed");
      } catch (error) {
        console.error("Failed to save data:", error);
      }
    }, 2000);

    return () => clearTimeout(timeoutId);
  }, [items]);

  return (
    <div
      className={`h-screen flex flex-col bg-gray-50/95 dark:bg-gray-900/95 ${
        isDragOver ? "ring-2 ring-inset ring-primary-400" : ""
      }`}
    >
      <Toolbar onEditItem={handleEdit} />
      <FilterBar />
      <div className="flex-1 overflow-hidden">
        <TodoTable onEdit={handleEdit} />
      </div>
      <Dialog
        open={isDialogOpen}
        onOpenChange={setIsDialogOpen}
        title={editingItem ? "アイテムを編集" : "新しいアイテムを追加"}
      >
        <TodoForm
          item={editingItem || undefined}
          onSubmit={handleSave}
          onCancel={() => {
            setIsDialogOpen(false);
            setEditingItem(null);
          }}
        />
      </Dialog>
    </div>
  );
}

function App() {
  useThemeBootstrap();
  const detailItemId = getDetailItemId();
  if (detailItemId != null) {
    return <DetailWindow itemId={detailItemId} />;
  }
  return <MainApp />;
}

export default App;
