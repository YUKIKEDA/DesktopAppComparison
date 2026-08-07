import { create } from "zustand";
import type { TodoItem, SortConfig, FilterConfig, ThemeMode } from "../types";

interface TodoStore {
  items: TodoItem[];
  selectedIds: Set<number>;
  filters: FilterConfig[];
  sorts: SortConfig[];
  isLoading: boolean;
  theme: ThemeMode;

  // Actions
  setItems: (items: TodoItem[]) => void;
  addItem: (
    item: Omit<TodoItem, "id" | "createdAt" | "updatedAt">
  ) => void;
  updateItem: (id: number, updates: Partial<TodoItem>) => void;
  deleteItems: (ids: number[]) => void;
  toggleSelection: (id: number) => void;
  selectAll: () => void;
  deselectAll: () => void;
  setFilters: (filters: FilterConfig[]) => void;
  setSorts: (sorts: SortConfig[]) => void;
  setLoading: (loading: boolean) => void;
  setTheme: (theme: ThemeMode) => void;
}

export const useTodoStore = create<TodoStore>((set, get) => ({
  items: [],
  selectedIds: new Set(),
  filters: [],
  sorts: [],
  isLoading: false,
  theme: "light",

  setItems: (items) => set({ items }),

  addItem: (itemData) => {
    const items = get().items;
    const maxId =
      items.length > 0 ? Math.max(...items.map((i) => i.id)) : 0;
    const now = new Date().toISOString();
    const newItem: TodoItem = {
      ...itemData,
      id: maxId + 1,
      createdAt: now,
      updatedAt: now,
    };
    set({ items: [...items, newItem] });
  },

  updateItem: (id, updates) => {
    const items = get().items;
    set({
      items: items.map((item) =>
        item.id === id
          ? { ...item, ...updates, updatedAt: new Date().toISOString() }
          : item
      ),
    });
  },

  deleteItems: (ids) => {
    const items = get().items;
    const selectedIds = get().selectedIds;
    set({
      items: items.filter((item) => !ids.includes(item.id)),
      selectedIds: new Set(
        [...selectedIds].filter((id) => !ids.includes(id))
      ),
    });
  },

  toggleSelection: (id) => {
    const selectedIds = new Set(get().selectedIds);
    if (selectedIds.has(id)) {
      selectedIds.delete(id);
    } else {
      selectedIds.add(id);
    }
    set({ selectedIds });
  },

  selectAll: () => {
    const items = get().items;
    set({ selectedIds: new Set(items.map((item) => item.id)) });
  },

  deselectAll: () => set({ selectedIds: new Set() }),

  setFilters: (filters) => set({ filters }),

  setSorts: (sorts) => set({ sorts }),

  setLoading: (isLoading) => set({ isLoading }),

  setTheme: (theme) => set({ theme }),
}));
