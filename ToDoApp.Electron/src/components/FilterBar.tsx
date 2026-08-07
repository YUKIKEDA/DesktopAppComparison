import { useState, useCallback, useEffect } from "react";
import { Input } from "./ui/Input";
import { Select } from "./ui/Select";
import { Button } from "./ui/Button";
import { useTodoStore } from "../store/useTodoStore";
import type { FilterConfig } from "../types";

export function FilterBar() {
  const { filters, setFilters } = useTodoStore();
  const [searchText, setSearchText] = useState("");
  const [statusFilter, setStatusFilter] = useState<string[]>([]);
  const [priorityFilter, setPriorityFilter] = useState<string[]>([]);

  // 既存のフィルタから初期値を設定（初回のみ）
  const [initialized, setInitialized] = useState(false);
  useEffect(() => {
    if (initialized) return;

    const textFilter = filters.find(
      (f) =>
        f.type === "text" &&
        (f.columnId === "title" || f.columnId === "description")
    );
    if (textFilter && typeof textFilter.value === "string") {
      setSearchText(textFilter.value);
    }

    const statusFilterConfig = filters.find(
      (f) => f.type === "select" && f.columnId === "status"
    );
    if (statusFilterConfig && Array.isArray(statusFilterConfig.value)) {
      setStatusFilter(statusFilterConfig.value as string[]);
    }

    const priorityFilterConfig = filters.find(
      (f) => f.type === "select" && f.columnId === "priority"
    );
    if (priorityFilterConfig && Array.isArray(priorityFilterConfig.value)) {
      setPriorityFilter(priorityFilterConfig.value as string[]);
    }

    setInitialized(true);
  }, [filters, initialized]);

  const applyFilters = useCallback(() => {
    const newFilters: FilterConfig[] = [];

    // テキスト検索フィルタ（タイトルと説明の両方を検索）
    if (searchText.trim()) {
      newFilters.push({
        columnId: "title",
        type: "text",
        value: searchText.trim(),
      });
    }

    // ステータスフィルタ
    if (statusFilter.length > 0) {
      newFilters.push({
        columnId: "status",
        type: "select",
        value: statusFilter,
      });
    }

    // 優先度フィルタ
    if (priorityFilter.length > 0) {
      newFilters.push({
        columnId: "priority",
        type: "select",
        value: priorityFilter,
      });
    }

    setFilters(newFilters);
  }, [searchText, statusFilter, priorityFilter, setFilters]);

  const clearFilters = useCallback(() => {
    setSearchText("");
    setStatusFilter([]);
    setPriorityFilter([]);
    setFilters([]);
  }, [setFilters]);

  const handleStatusChange = useCallback(
    (e: React.ChangeEvent<HTMLSelectElement>) => {
      const value = e.target.value;
      if (value === "") {
        setStatusFilter([]);
      } else {
        setStatusFilter([value]);
      }
    },
    []
  );

  const handlePriorityChange = useCallback(
    (e: React.ChangeEvent<HTMLSelectElement>) => {
      const value = e.target.value;
      if (value === "") {
        setPriorityFilter([]);
      } else {
        setPriorityFilter([value]);
      }
    },
    []
  );

  // フィルタ変更時に自動適用
  useEffect(() => {
    applyFilters();
  }, [searchText, statusFilter, priorityFilter, applyFilters]);

  const hasActiveFilters =
    searchText.trim() !== "" ||
    statusFilter.length > 0 ||
    priorityFilter.length > 0;

  return (
    <div className="px-4 py-3 bg-gray-50 border-b border-gray-200 shrink-0 dark:bg-gray-900 dark:border-gray-700">
      <div className="flex flex-wrap items-center gap-2">
        <div className="flex-1 min-w-[200px]">
          <Input
            type="text"
            placeholder="タイトル・説明で検索..."
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            className="w-full"
          />
        </div>

        <div className="flex flex-wrap items-center gap-2 shrink-0">
          <Select
            value={statusFilter[0] || ""}
            onChange={handleStatusChange}
            className="w-36"
          >
            <option value="">すべてのステータス</option>
            <option value="未着手">未着手</option>
            <option value="進行中">進行中</option>
            <option value="完了">完了</option>
          </Select>

          <Select
            value={priorityFilter[0] || ""}
            onChange={handlePriorityChange}
            className="w-36"
          >
            <option value="">すべての優先度</option>
            <option value="低">低</option>
            <option value="中">中</option>
            <option value="高">高</option>
          </Select>

          {hasActiveFilters && (
            <Button variant="outline" size="sm" onClick={clearFilters}>
              クリア
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}
