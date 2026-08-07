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
  const { setItems, updateItem, setLoading } = useTodoStore();
  const [item, setItem] = useState<TodoItem | null>(null);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    try {
      const data = await DataService.loadData();
      setItems(data.items);
      const found = data.items.find((i) => i.id === itemId) ?? null;
      setItem(found);
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
    void listen("data-changed", () => {
      void reload();
    }).then((fn) => {
      unlisten = fn;
    });
    return () => {
      unlisten?.();
    };
  }, [reload]);

  const handleSave = async (data: {
    title: string;
    description?: string;
    status: "未着手" | "進行中" | "完了";
    priority: "低" | "中" | "高";
    dueDate: string | null;
  }) => {
    if (!item) return;
    updateItem(item.id, {
      title: data.title,
      description: data.description || "",
      status: data.status,
      priority: data.priority,
      dueDate: data.dueDate ?? null,
    });
    const { items: updated } = useTodoStore.getState();
    await DataService.saveData({ items: updated });
    const found = updated.find((i) => i.id === itemId) ?? null;
    setItem(found);
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
      <div className="h-screen flex items-center justify-center bg-gray-50/95 p-6">
        <p className="text-red-600">{error}</p>
      </div>
    );
  }

  if (!item) {
    return (
      <div className="h-screen flex items-center justify-center bg-gray-50/95 p-6">
        <p className="text-gray-500">読み込み中...</p>
      </div>
    );
  }

  return (
    <div className="h-screen overflow-auto bg-gray-50/95 p-6">
      <h1 className="text-lg font-semibold text-gray-900 mb-4">
        アイテム詳細 #{item.id}
      </h1>
      <div className="bg-white/95 border border-gray-200 rounded-lg p-4">
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
