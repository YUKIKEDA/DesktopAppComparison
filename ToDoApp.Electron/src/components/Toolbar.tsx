import { Button } from "./ui/Button";
import { useTodoStore } from "../store/useTodoStore";
import { DataService } from "../lib/dataService";
import { applyThemeClass } from "../lib/utils";
import { copyText, showNotification } from "../lib/platform";
import { useEffect, useCallback } from "react";
import type { TodoItem } from "../types";

interface ToolbarProps {
  onEditItem: (item: TodoItem | null) => void;
}

export function Toolbar({ onEditItem }: ToolbarProps) {
  const { items, selectedIds, deleteItems, setItems, setLoading, theme, setTheme } =
    useTodoStore();

  const handleAdd = useCallback(() => {
    onEditItem(null); // Trigger add dialog
  }, [onEditItem]);

  const saveData = useCallback(
    async (opts?: { notify?: boolean }) => {
      setLoading(true);
      try {
        await DataService.saveData({ items });
        if (opts?.notify) {
          await showNotification("Todo App", "保存しました");
        }
      } finally {
        setLoading(false);
      }
    },
    [items, setLoading]
  );

  const handleCopy = useCallback(async () => {
    if (selectedIds.size === 0) return;
    const selected = items.filter((item) => selectedIds.has(item.id));
    try {
      await copyText(JSON.stringify(selected, null, 2));
    } catch (error) {
      console.error("Copy failed:", error);
    }
  }, [items, selectedIds]);

  const handleDelete = useCallback(async () => {
    if (selectedIds.size === 0) return;
    if (confirm(`${selectedIds.size}件のアイテムを削除しますか？`)) {
      deleteItems(Array.from(selectedIds));
      // 削除後の最新のitemsを取得して保存
      const { items: updatedItems } = useTodoStore.getState();
      setLoading(true);
      try {
        await DataService.saveData({ items: updatedItems });
      } finally {
        setLoading(false);
      }
    }
  }, [selectedIds, deleteItems, setLoading]);

  const handleExport = async () => {
    try {
      await DataService.exportData({ items });
    } catch (error) {
      console.error("Export failed:", error);
    }
  };

  const handleImport = async () => {
    try {
      const data = await DataService.importData();
      if (data) {
        setItems(data.items);
        await DataService.saveData({ items: data.items });
        await showNotification("Todo App", "インポートしました");
      }
    } catch (error) {
      console.error("Import failed:", error);
    }
  };

  const handleOpenDataFolder = async () => {
    try {
      await DataService.openDataFolder();
    } catch (error) {
      console.error("Failed to open data folder:", error);
    }
  };

  const handleOpenDetailWindow = async () => {
    if (selectedIds.size !== 1) return;
    const itemId = Array.from(selectedIds)[0];
    try {
      await DataService.openDetailWindow(itemId);
    } catch (error) {
      console.error("Failed to open detail window:", error);
    }
  };

  const handleToggleTheme = async () => {
    const next = theme === "light" ? "dark" : "light";
    setTheme(next);
    applyThemeClass(next);
    try {
      await DataService.saveTheme({ theme: next });
    } catch (error) {
      console.error("Failed to save theme:", error);
    }
  };

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.ctrlKey || e.metaKey) {
        if (e.key === "n") {
          e.preventDefault();
          handleAdd();
        } else if (e.key === "s") {
          e.preventDefault();
          void saveData({ notify: true });
        } else if (e.key === "c" && selectedIds.size > 0) {
          const target = e.target as HTMLElement | null;
          const tag = target?.tagName?.toLowerCase();
          if (tag === "input" || tag === "textarea" || target?.isContentEditable) {
            return;
          }
          e.preventDefault();
          void handleCopy();
        } else if (e.key === "f") {
          e.preventDefault();
          // Focus search input
          const searchInput = document.querySelector(
            'input[placeholder="タイトル・説明で検索..."]'
          ) as HTMLInputElement;
          searchInput?.focus();
        }
      } else if (e.key === "Delete" && selectedIds.size > 0) {
        handleDelete();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [selectedIds.size, handleAdd, saveData, handleDelete, handleCopy]);

  return (
    <>
      <div className="flex flex-wrap items-center gap-2 p-4 bg-white border-b border-gray-200 dark:bg-gray-800 dark:border-gray-700">
        <Button onClick={() => handleAdd()}>+ 新しいアイテム</Button>
        <Button
          variant="destructive"
          onClick={handleDelete}
          disabled={selectedIds.size === 0}
        >
          削除 ({selectedIds.size})
        </Button>
        <Button
          variant="outline"
          onClick={() => void handleCopy()}
          disabled={selectedIds.size === 0}
        >
          コピー
        </Button>
        <Button
          variant="outline"
          onClick={handleOpenDetailWindow}
          disabled={selectedIds.size !== 1}
        >
          別ウィンドウで開く
        </Button>
        <div className="flex-1 min-w-[1rem]" />
        <Button variant="outline" onClick={handleToggleTheme}>
          {theme === "light" ? "ダーク" : "ライト"}
        </Button>
        <Button variant="outline" onClick={handleExport}>
          エクスポート
        </Button>
        <Button variant="outline" onClick={handleImport}>
          インポート
        </Button>
        <Button variant="outline" onClick={handleOpenDataFolder}>
          データフォルダを開く
        </Button>
      </div>
    </>
  );
}
