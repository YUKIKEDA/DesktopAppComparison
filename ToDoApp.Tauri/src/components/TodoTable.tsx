import {
  useReactTable,
  getCoreRowModel,
  flexRender,
  type ColumnDef,
  type SortingState,
} from "@tanstack/react-table";
import { useVirtualizer } from "@tanstack/react-virtual";
import { useEffect, useMemo, useRef, useState } from "react";
import type { TodoItem } from "../types";
import { useTodoStore } from "../store/useTodoStore";
import { filterAndSort } from "../lib/filterSort";
import { scheduleWork } from "../lib/scheduleWork";
import { Checkbox } from "./ui/Checkbox";
import { Button } from "./ui/Button";
import { format } from "date-fns";
import { cn } from "../lib/utils";

const PAGE_SIZE = 100;

interface TodoTableProps {
  onEdit?: (item: TodoItem) => void;
}

export function TodoTable({ onEdit }: TodoTableProps) {
  const {
    items,
    selectedIds,
    filters,
    toggleSelection,
    visibleCount,
    setVisibleCount,
    resetVisibleCount,
  } = useTodoStore();

  const [sorting, setSorting] = useState<SortingState>([]);
  const [processedItems, setProcessedItems] = useState<TodoItem[]>([]);
  const tableContainerRef = useRef<HTMLDivElement>(null);
  const processGenRef = useRef(0);

  // Reset windowed count when filter/sort/source changes
  useEffect(() => {
    resetVisibleCount();
  }, [items, filters, sorting, resetVisibleCount]);

  // Filter/sort off the critical path (idle / setTimeout(0))
  useEffect(() => {
    const gen = ++processGenRef.current;
    const cancel = scheduleWork(() => {
      if (gen !== processGenRef.current) return;
      setProcessedItems(filterAndSort(items, filters, sorting));
    });
    return () => {
      cancel();
    };
  }, [items, filters, sorting]);

  const displayItems = useMemo(
    () => processedItems.slice(0, visibleCount),
    [processedItems, visibleCount]
  );

  const allFilteredSelected = useMemo(() => {
    if (processedItems.length === 0) return false;
    return processedItems.every((item) => selectedIds.has(item.id));
  }, [processedItems, selectedIds]);

  const someFilteredSelected = useMemo(() => {
    return processedItems.some((item) => selectedIds.has(item.id));
  }, [processedItems, selectedIds]);

  const columns = useMemo<ColumnDef<TodoItem>[]>(
    () => [
      {
        id: "select",
        header: () => (
          <Checkbox
            checked={allFilteredSelected}
            ref={(el) => {
              if (el) {
                el.indeterminate = someFilteredSelected && !allFilteredSelected;
              }
            }}
            onChange={(e) => {
              if (e.target.checked) {
                processedItems.forEach((item) => {
                  if (!selectedIds.has(item.id)) {
                    toggleSelection(item.id);
                  }
                });
              } else {
                processedItems.forEach((item) => {
                  if (selectedIds.has(item.id)) {
                    toggleSelection(item.id);
                  }
                });
              }
            }}
          />
        ),
        cell: ({ row }) => (
          <Checkbox
            checked={selectedIds.has(row.original.id)}
            onChange={() => toggleSelection(row.original.id)}
          />
        ),
        size: 50,
      },
      {
        accessorKey: "id",
        header: "ID",
        size: 80,
      },
      {
        accessorKey: "title",
        header: "タイトル",
        size: 200,
        cell: ({ row }) => (
          <div className="truncate" title={row.original.title}>
            {row.original.title}
          </div>
        ),
      },
      {
        accessorKey: "description",
        header: "説明",
        size: 300,
        cell: ({ row }) => (
          <div className="truncate" title={row.original.description || ""}>
            {row.original.description || "-"}
          </div>
        ),
      },
      {
        accessorKey: "status",
        header: "ステータス",
        size: 120,
      },
      {
        accessorKey: "priority",
        header: "優先度",
        size: 100,
      },
      {
        accessorKey: "dueDate",
        header: "期限",
        cell: ({ row }) =>
          row.original.dueDate
            ? format(new Date(row.original.dueDate), "yyyy-MM-dd")
            : "-",
        size: 120,
      },
      {
        accessorKey: "createdAt",
        header: "作成日時",
        cell: ({ row }) =>
          format(new Date(row.original.createdAt), "yyyy-MM-dd HH:mm"),
        size: 160,
      },
      {
        accessorKey: "updatedAt",
        header: "更新日時",
        cell: ({ row }) =>
          format(new Date(row.original.updatedAt), "yyyy-MM-dd HH:mm"),
        size: 160,
      },
      {
        id: "actions",
        header: "操作",
        cell: ({ row }) => (
          <Button
            variant="outline"
            size="sm"
            onClick={() => onEdit?.(row.original)}
          >
            編集
          </Button>
        ),
        size: 100,
      },
    ],
    [
      selectedIds,
      toggleSelection,
      onEdit,
      processedItems,
      allFilteredSelected,
      someFilteredSelected,
    ]
  );

  const table = useReactTable({
    data: displayItems,
    columns,
    getCoreRowModel: getCoreRowModel(),
    enableSorting: true,
    manualSorting: true,
    state: {
      sorting,
    },
    onSortingChange: setSorting,
  });

  const { rows } = table.getRowModel();

  const rowVirtualizer = useVirtualizer({
    count: rows.length,
    getScrollElement: () => tableContainerRef.current,
    estimateSize: () => 50,
    overscan: 10,
  });

  const virtualItems = rowVirtualizer.getVirtualItems();
  const totalSize = rowVirtualizer.getTotalSize();
  const lastVirtualIndex = virtualItems[virtualItems.length - 1]?.index ?? -1;

  // Lazy load: grow visible window when virtualizer nears the end
  useEffect(() => {
    if (lastVirtualIndex < 0) return;
    if (
      lastVirtualIndex >= displayItems.length - 10 &&
      visibleCount < processedItems.length
    ) {
      setVisibleCount(
        Math.min(visibleCount + PAGE_SIZE, processedItems.length)
      );
    }
  }, [
    lastVirtualIndex,
    displayItems.length,
    visibleCount,
    processedItems.length,
    setVisibleCount,
  ]);

  const paddingTop = virtualItems.length > 0 ? virtualItems[0]?.start ?? 0 : 0;
  const paddingBottom =
    virtualItems.length > 0
      ? totalSize - (virtualItems[virtualItems.length - 1]?.end ?? 0)
      : 0;

  return (
    <div className="h-full flex flex-col border border-gray-200 overflow-hidden dark:border-gray-700 dark:bg-gray-900">
      <div
        ref={tableContainerRef}
        className="flex-1 overflow-auto"
        style={{ contain: "strict" }}
      >
        <table className="w-full border-collapse table-fixed">
          <thead className="bg-gray-50 sticky top-0 z-10 dark:bg-gray-800">
            {table.getHeaderGroups().map((headerGroup) => (
              <tr key={headerGroup.id}>
                {headerGroup.headers.map((header) => (
                  <th
                    key={header.id}
                    className="px-4 py-3 text-left text-sm font-semibold text-gray-700 border-b border-gray-200 dark:text-gray-200 dark:border-gray-700"
                    style={{ width: header.getSize() }}
                  >
                    {header.isPlaceholder ? null : (
                      <div
                        className={cn(
                          "flex items-center gap-2",
                          header.column.getCanSort() &&
                            "cursor-pointer select-none hover:text-gray-900 dark:hover:text-white"
                        )}
                        onClick={header.column.getToggleSortingHandler()}
                      >
                        {flexRender(
                          header.column.columnDef.header,
                          header.getContext()
                        )}
                        {header.column.getIsSorted() && (
                          <span className="ml-1">
                            {header.column.getIsSorted() === "asc" ? "↑" : "↓"}
                          </span>
                        )}
                      </div>
                    )}
                  </th>
                ))}
              </tr>
            ))}
          </thead>
          <tbody>
            {paddingTop > 0 && (
              <tr>
                <td
                  colSpan={table.getAllColumns().length}
                  style={{ height: paddingTop }}
                />
              </tr>
            )}
            {virtualItems.map((virtualRow) => {
              const row = rows[virtualRow.index];
              if (!row) return null;
              return (
                <tr
                  key={row.id}
                  className={cn(
                    "border-b border-gray-100 hover:bg-gray-50 cursor-pointer dark:border-gray-800 dark:hover:bg-gray-800/80 dark:text-gray-100",
                    selectedIds.has(row.original.id) &&
                      "bg-primary-50 dark:bg-primary-950/60"
                  )}
                  style={{
                    height: `${virtualRow.size}px`,
                  }}
                  onDoubleClick={() => onEdit?.(row.original)}
                >
                  {row.getVisibleCells().map((cell) => (
                    <td
                      key={cell.id}
                      className="px-4 py-2 text-sm overflow-hidden text-ellipsis whitespace-nowrap"
                      style={{ width: cell.column.getSize() }}
                      title={
                        typeof cell.getValue() === "string"
                          ? (cell.getValue() as string)
                          : undefined
                      }
                    >
                      {flexRender(
                        cell.column.columnDef.cell,
                        cell.getContext()
                      )}
                    </td>
                  ))}
                </tr>
              );
            })}
            {paddingBottom > 0 && (
              <tr>
                <td
                  colSpan={table.getAllColumns().length}
                  style={{ height: paddingBottom }}
                />
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
