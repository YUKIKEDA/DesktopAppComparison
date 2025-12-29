import { useEffect, useState } from "react";
import { useTodoStore } from "./store/useTodoStore";
import { DataService } from "./lib/dataService";
import { Toolbar } from "./components/Toolbar";
import { FilterBar } from "./components/FilterBar";
import { TodoTable } from "./components/TodoTable";
import { Dialog } from "./components/ui/Dialog";
import { TodoForm } from "./components/TodoForm";
import type { TodoItem } from "./types";
import "./App.css";

function App() {
  const { items, setItems, setLoading, addItem, updateItem } = useTodoStore();
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<TodoItem | null>(null);

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

  // Load data on mount
  useEffect(() => {
    const loadData = async () => {
      setLoading(true);
      try {
        const data = await DataService.loadData();
        setItems(data.items);
      } catch (error) {
        console.error("Failed to load data:", error);
      } finally {
        setLoading(false);
      }
    };
    loadData();
  }, [setItems, setLoading]);

  // Auto-save with debounce
  useEffect(() => {
    if (items.length === 0) return;

    const timeoutId = setTimeout(async () => {
      try {
        await DataService.saveData({ items });
      } catch (error) {
        console.error("Failed to save data:", error);
      }
    }, 2000); // 2 seconds debounce

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
    <div className="h-screen flex flex-col bg-gray-50">
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

export default App;
