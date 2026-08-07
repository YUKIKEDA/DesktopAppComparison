import type { SortingState } from "@tanstack/react-table";
import type { FilterConfig, TodoItem } from "../types";

function compareValues(a: unknown, b: unknown): number {
  if (a == null && b == null) return 0;
  if (a == null) return 1;
  if (b == null) return -1;
  if (typeof a === "number" && typeof b === "number") return a - b;
  if (typeof a === "boolean" && typeof b === "boolean") {
    return Number(a) - Number(b);
  }
  return String(a).localeCompare(String(b), "ja");
}

export function filterItems(
  items: TodoItem[],
  filters: FilterConfig[]
): TodoItem[] {
  let result = items;

  for (const filter of filters) {
    if (filter.type === "text" && typeof filter.value === "string") {
      const searchTerm = filter.value.toLowerCase();
      result = result.filter((item) => {
        if (filter.columnId === "title") {
          return (
            item.title.toLowerCase().includes(searchTerm) ||
            item.description.toLowerCase().includes(searchTerm)
          );
        }
        if (filter.columnId === "description") {
          return item.description.toLowerCase().includes(searchTerm);
        }
        return true;
      });
    } else if (filter.type === "select" && Array.isArray(filter.value)) {
      const filterValues = filter.value as string[];
      result = result.filter((item) => {
        if (filter.columnId === "status") {
          return filterValues.includes(item.status);
        }
        if (filter.columnId === "priority") {
          return filterValues.includes(item.priority);
        }
        return true;
      });
    }
  }

  return result;
}

export function sortItems(
  items: TodoItem[],
  sorting: SortingState
): TodoItem[] {
  if (sorting.length === 0) return items;

  const sorted = items.slice();
  sorted.sort((a, b) => {
    for (const sort of sorting) {
      const key = sort.id as keyof TodoItem;
      const cmp = compareValues(a[key], b[key]);
      if (cmp !== 0) return sort.desc ? -cmp : cmp;
    }
    return 0;
  });
  return sorted;
}

/** Pure filter + sort for background scheduling. */
export function filterAndSort(
  items: TodoItem[],
  filters: FilterConfig[],
  sorting: SortingState
): TodoItem[] {
  return sortItems(filterItems(items, filters), sorting);
}
