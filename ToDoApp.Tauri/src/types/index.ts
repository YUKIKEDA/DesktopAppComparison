export interface TodoItem {
  id: number;
  title: string;
  description: string;
  status: "未着手" | "進行中" | "完了";
  priority: "低" | "中" | "高";
  dueDate: string | null;
  createdAt: string;
  updatedAt: string;
  isCompleted: boolean;
}

export interface ProjectData {
  items: TodoItem[];
}

export type ThemeMode = "light" | "dark";

export interface ThemeData {
  theme: ThemeMode;
}

export type SortDirection = "asc" | "desc" | null;

export interface SortConfig {
  columnId: string;
  direction: SortDirection;
}

export interface FilterConfig {
  columnId: string;
  type: "text" | "date" | "select";
  value: string | string[] | { from: string | null; to: string | null };
}

export interface TableState {
  selectedIds: Set<number>;
  filters: FilterConfig[];
  sorts: SortConfig[];
}

