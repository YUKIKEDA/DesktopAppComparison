import { useEffect, useState, useCallback } from "react";
import { emit, listen } from "@tauri-apps/api/event";
import { useTodoStore } from "../store/useTodoStore";
import { DataService } from "../lib/dataService";
import { TodoForm } from "./TodoForm";
import type { TodoItem } from "../types";

interface DetailWindowProps {
  itemId: number;
}

export function DetailWindow({ itemId }: DetailWindowProps) {
  const setItems = useTodoStore((s) => s.setItems);
  const setLoading = useTodoStore((s) => s.setLoading);
  const [item, setItem] = useState<TodoItem | null>(null);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    try {
      const data = await DataService.loadData();
      // Keep only the viewed item — avoid retaining the full list in the store
      const found = data.items.find((i) => i.id === itemId) ?? null;
      setItem(found);
      setItems(found ? [found] : []);
      if (!found) {
        setError("アイテムが見つかりません");
      } else {
        setError(null);
      }
    } catch (e) {
      console.error(e);
      setError("データの読み込みに失敗しました");
    } finally {
      setLoading(false);
    }
  }, [itemId, setItems, setLoading]);

  useEffect(() => {
    void reload();
  }, [reload]);

  useEffect(() => {
    let unlisten: (() => void) | undefined;
    let cancelled = false;
    void listen("data-changed", () => {
      void reload();
    }).then((fn) => {
      if (cancelled) {
        fn();
        return;
      }
      unlisten = fn;
    });
    return () => {
      cancelled = true;
      unlisten?.();
    };
  }, [reload]);

  // Drop retained arrays on unmount (listener cleanup above)
  useEffect(() => {
    return () => {
      setItem(null);
      setItems([]);
    };
  }, [setItems]);

  const handleSave = async (data: {
    title: string;
    description?: string;
    status: "未着手" | "進行中" | "完了";
    priority: "低" | "中" | "高";
    dueDate: string | null;
  }) => {
    if (!item) return;
    const current = await DataService.loadData();
    const now = new Date().toISOString();
    const updatedItems = current.items.map((i) =>
      i.id === item.id
        ? {
            ...i,
            title: data.title,
            description: data.description || "",
            status: data.status,
            priority: data.priority,
            dueDate: data.dueDate ?? null,
            updatedAt: now,
          }
        : i
    );
    await DataService.saveData({ items: updatedItems });
    const found = updatedItems.find((i) => i.id === itemId) ?? null;
    setItem(found);
    setItems(found ? [found] : []);
    await emit("data-changed");
  };

  const handleClose = async () => {
    try {
      const { getCurrentWindow } = await import("@tauri-apps/api/window");
      await getCurrentWindow().close();
    } catch {
      window.close();
    }
  };

  if (error) {
    return (
      <div className="h-screen flex items-center justify-center bg-gray-50/95 p-6 dark:bg-gray-900/95">
        <p className="text-red-600 dark:text-red-400">{error}</p>
      </div>
    );
  }

  if (!item) {
    return (
      <div className="h-screen flex items-center justify-center bg-gray-50/95 p-6 dark:bg-gray-900/95">
        <p className="text-gray-500 dark:text-gray-400">読み込み中...</p>
      </div>
    );
  }

  return (
    <div className="h-screen overflow-auto bg-gray-50/95 p-6 dark:bg-gray-900/95">
      <h1 className="text-lg font-semibold text-gray-900 mb-4 dark:text-gray-100">
        アイテム詳細 #{item.id}
      </h1>
      <div className="bg-white/95 border border-gray-200 rounded-lg p-4 dark:bg-gray-800/95 dark:border-gray-700">
        <TodoForm
          key={item.updatedAt}
          item={item}
          onSubmit={handleSave}
          onCancel={() => {
            void handleClose();
          }}
        />
      </div>
    </div>
  );
}
