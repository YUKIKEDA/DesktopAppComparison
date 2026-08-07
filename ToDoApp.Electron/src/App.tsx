import { useEffect, useState, useCallback, useRef } from "react";
import { useTodoStore } from "./store/useTodoStore";
import { DataService } from "./lib/dataService";
import { applyThemeClass } from "./lib/utils";
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
    if (!window.electronAPI?.onThemeChanged) return;
    return window.electronAPI.onThemeChanged(async () => {
      try {
        const data = await DataService.loadTheme();
        setTheme(data.theme);
        applyThemeClass(data.theme);
      } catch (error) {
        console.error("Failed to reload theme:", error);
      }
    });
  }, [setTheme]);
}

function MainApp() {
  const { items, setItems, setLoading, addItem, updateItem } = useTodoStore();
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<TodoItem | null>(null);
  const [isDragOver, setIsDragOver] = useState(false);
  const skipSaveRef = useRef(false);

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
    },
    [setItems]
  );

  const handleDropFiles = useCallback(
    async (files: FileList | File[]) => {
      const list = Array.from(files);
      const jsonFile = list.find((f) =>
        f.name.toLowerCase().endsWith(".json")
      );
      if (!jsonFile) return;

      let filePath = "";
      if (window.electronAPI?.getPathForFile) {
        filePath = window.electronAPI.getPathForFile(jsonFile);
      }
      if (!filePath) {
        filePath = (jsonFile as File & { path?: string }).path ?? "";
      }
      if (!filePath) {
        console.error("Could not resolve dropped file path");
        return;
      }

      try {
        const data = await DataService.importFromPath(filePath);
        await applyImportedData(data);
      } catch (error) {
        console.error("Drop import failed:", error);
      }
    },
    [applyImportedData]
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

  // Sync when another window saves
  useEffect(() => {
    if (!window.electronAPI?.onDataChanged) return;
    return window.electronAPI.onDataChanged(async () => {
      try {
        const data = await DataService.loadData();
        skipSaveRef.current = true;
        setItems(data.items);
      } catch (error) {
        console.error("Failed to reload data:", error);
      }
    });
  }, [setItems]);

  // Auto-save with debounce
  useEffect(() => {
    if (skipSaveRef.current) {
      skipSaveRef.current = false;
      return;
    }
    if (items.length === 0) return;

    const timeoutId = setTimeout(async () => {
      try {
        await DataService.saveData({ items });
      } catch (error) {
        console.error("Failed to save data:", error);
      }
    }, 2000);

    return () => clearTimeout(timeoutId);
  }, [items]);

  // Keyboard shortcuts
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === "s") {
        e.preventDefault();
        DataService.saveData({ items }).catch(console.error);
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [items]);

  return (
    <div
      className={`h-screen flex flex-col bg-gray-50 dark:bg-gray-900 ${
        isDragOver ? "ring-2 ring-inset ring-primary-400" : ""
      }`}
      onDragEnter={(e) => {
        e.preventDefault();
        e.stopPropagation();
        setIsDragOver(true);
      }}
      onDragOver={(e) => {
        e.preventDefault();
        e.stopPropagation();
        setIsDragOver(true);
      }}
      onDragLeave={(e) => {
        e.preventDefault();
        e.stopPropagation();
        if (e.currentTarget === e.target) setIsDragOver(false);
      }}
      onDrop={(e) => {
        e.preventDefault();
        e.stopPropagation();
        setIsDragOver(false);
        void handleDropFiles(e.dataTransfer.files);
      }}
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
