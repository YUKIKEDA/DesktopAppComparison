import { Button } from "./ui/Button";
import { useTodoStore } from "../store/useTodoStore";
import { DataService } from "../lib/dataService";
import { openDetailWindow } from "../lib/windowService";
import { useEffect, useCallback } from "react";
import { ask } from "@tauri-apps/plugin-dialog";
import type { TodoItem } from "../types";

interface ToolbarProps {
  onEditItem: (item: TodoItem | null) => void;
}

export function Toolbar({ onEditItem }: ToolbarProps) {
  const { items, selectedIds, deleteItems, setItems, setLoading } =
    useTodoStore();

  const handleAdd = useCallback(() => {
    onEditItem(null); // Trigger add dialog
  }, [onEditItem]);

  const saveData = useCallback(async () => {
    setLoading(true);
    try {
      await DataService.saveData({ items });
    } finally {
      setLoading(false);
    }
  }, [items, setLoading]);

  const handleDelete = useCallback(async () => {
    if (selectedIds.size === 0) return;

    // 確認ダイアログを表示し、結果を待つ（同期的に待機）
    const confirmed = await ask(
      `${selectedIds.size}件のアイテムを削除しますか？`,
      "削除の確認"
    );

    if (!confirmed) {
      return; // キャンセルされた場合は何もしない
    }

    // 確認後に削除処理を実行
    const idsToDelete = Array.from(selectedIds);
    deleteItems(idsToDelete);

    // 削除後の最新のitemsを取得して保存
    const { items: updatedItems } = useTodoStore.getState();
    setLoading(true);
    try {
      await DataService.saveData({ items: updatedItems });
    } finally {
      setLoading(false);
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
        await saveData();
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
      await openDetailWindow(itemId);
    } catch (error) {
      console.error("Failed to open detail window:", error);
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
          saveData();
        } else if (e.key === "f") {
          e.preventDefault();
          // Focus search input
          const searchInput = document.querySelector(
            'input[placeholder="タイトル・説明で検索..."]'
          ) as HTMLInputElement;
          searchInput?.focus();
        }
      } else if (e.key === "Delete" && selectedIds.size > 0) {
        e.preventDefault();
        handleDelete();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [selectedIds.size, handleAdd, saveData, handleDelete]);

  return (
    <>
      <div className="flex items-center gap-2 p-4 bg-white/95 border-b border-gray-200">
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
          onClick={handleOpenDetailWindow}
          disabled={selectedIds.size !== 1}
        >
          別ウィンドウで開く
        </Button>
        <div className="flex-1" />
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
