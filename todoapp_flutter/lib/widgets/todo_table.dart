import 'dart:math' as math;

import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../providers/todo_provider.dart';
import '../models/todo_item.dart';
import '../models/filter_config.dart';
import '../services/ui_bench.dart';

const int _bgWorkThreshold = 5000;

/// Top-level entry for [compute] / Isolate — filter then sort.
List<Map<String, dynamic>> _filterSortWorker(Map<String, dynamic> args) {
  final items = (args['items'] as List)
      .cast<Map<String, dynamic>>()
      .map(TodoItem.fromJson)
      .toList();
  final filters = (args['filters'] as List)
      .cast<Map<String, dynamic>>()
      .map(FilterConfig.fromJson)
      .toList();
  final sortColumn = args['sortColumn'] as String?;
  final sortAscending = args['sortAscending'] as bool? ?? true;

  var result = _applyFilters(items, filters);
  result = _applySort(result, sortColumn, sortAscending);
  return result.map((e) => e.toJson()).toList();
}

List<TodoItem> _applyFilters(List<TodoItem> items, List<FilterConfig> filters) {
  var result = List<TodoItem>.from(items);

  for (final filter in filters) {
    if (filter.type == 'text' && filter.value is String) {
      final searchTerm = (filter.value as String).toLowerCase();
      result = result.where((item) {
        if (filter.columnId == 'title') {
          return item.title.toLowerCase().contains(searchTerm) ||
              item.description.toLowerCase().contains(searchTerm);
        }
        if (filter.columnId == 'description') {
          return item.description.toLowerCase().contains(searchTerm);
        }
        return true;
      }).toList();
    } else if (filter.type == 'select' && filter.value is List) {
      final filterValues = (filter.value as List).cast<String>();
      result = result.where((item) {
        if (filter.columnId == 'status') {
          return filterValues.contains(item.status);
        }
        if (filter.columnId == 'priority') {
          return filterValues.contains(item.priority);
        }
        return true;
      }).toList();
    }
  }

  return result;
}

List<TodoItem> _applySort(
  List<TodoItem> items,
  String? sortColumn,
  bool sortAscending,
) {
  if (sortColumn == null) return items;

  final sorted = List<TodoItem>.from(items);
  sorted.sort((a, b) {
    int comparison = 0;
    switch (sortColumn) {
      case 'id':
        comparison = a.id.compareTo(b.id);
        break;
      case 'title':
        comparison = a.title.compareTo(b.title);
        break;
      case 'description':
        comparison = a.description.compareTo(b.description);
        break;
      case 'status':
        comparison = a.status.compareTo(b.status);
        break;
      case 'priority':
        comparison = a.priority.compareTo(b.priority);
        break;
      case 'dueDate':
        final aDate = a.dueDate != null ? DateTime.tryParse(a.dueDate!) : null;
        final bDate = b.dueDate != null ? DateTime.tryParse(b.dueDate!) : null;
        if (aDate == null && bDate == null) return 0;
        if (aDate == null) return 1;
        if (bDate == null) return -1;
        comparison = aDate.compareTo(bDate);
        break;
      case 'createdAt':
        comparison = a.createdAt.compareTo(b.createdAt);
        break;
      case 'updatedAt':
        comparison = a.updatedAt.compareTo(b.updatedAt);
        break;
    }
    return sortAscending ? comparison : -comparison;
  });

  return sorted;
}

class TodoTable extends ConsumerStatefulWidget {
  final Function(TodoItem) onEdit;

  const TodoTable({super.key, required this.onEdit});

  @override
  ConsumerState<TodoTable> createState() => _TodoTableState();
}

class _TodoTableState extends ConsumerState<TodoTable> {
  final ScrollController _scrollController = ScrollController();
  String? _sortColumn;
  bool _sortAscending = true;

  List<TodoItem> _processedItems = const [];
  int _processGeneration = 0;
  Object? _lastProcessKey;

  @override
  void initState() {
    super.initState();
    _scrollController.addListener(_onScroll);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      final state = ref.read(todoProvider);
      _scheduleProcess(
        items: state.items,
        filters: state.filters,
        sortColumn: _sortColumn,
        sortAscending: _sortAscending,
      );
    });
  }

  @override
  void dispose() {
    _processGeneration++; // cancel in-flight isolate results
    _scrollController.removeListener(_onScroll);
    _scrollController.dispose();
    _processedItems = const [];
    super.dispose();
  }

  void _onScroll() {
    if (!_scrollController.hasClients) return;
    final pos = _scrollController.position;
    if (pos.maxScrollExtent <= 0) return;
    if (pos.pixels < pos.maxScrollExtent - 240) return;

    final total = _processedItems.length;
    final current = ref.read(todoProvider).visibleCount;
    if (current >= total) return;
    ref.read(todoProvider.notifier).expandVisibleWindow(
      kTodoPageSize,
      totalItems: total,
    );
  }

  Future<void> _scheduleProcess({
    required List<TodoItem> items,
    required List<FilterConfig> filters,
    required String? sortColumn,
    required bool sortAscending,
  }) async {
    final key = Object.hash(
      identityHashCode(items),
      items.length,
      filters.length,
      filters.map((f) => '${f.columnId}:${f.type}:${f.value}').join('|'),
      sortColumn,
      sortAscending,
    );
    if (key == _lastProcessKey) {
      // Already up to date — still unblock ui-bench waiters.
      UiBenchHooks.notifyTableUpdated();
      return;
    }
    _lastProcessKey = key;
    ref.read(todoProvider.notifier).resetVisibleCount();

    final generation = ++_processGeneration;

    if (items.length < _bgWorkThreshold) {
      final result = _applySort(
        _applyFilters(items, filters),
        sortColumn,
        sortAscending,
      );
      if (!mounted || generation != _processGeneration) {
        UiBenchHooks.notifyTableUpdated();
        return;
      }
      setState(() {
        _processedItems = result;
      });
      UiBenchHooks.notifyTableUpdated();
      return;
    }

    final maps = await compute(_filterSortWorker, {
      'items': items.map((e) => e.toJson()).toList(),
      'filters': filters.map((e) => e.toJson()).toList(),
      'sortColumn': sortColumn,
      'sortAscending': sortAscending,
    });

    if (!mounted || generation != _processGeneration) {
      UiBenchHooks.notifyTableUpdated();
      return;
    }
    final result = maps.map(TodoItem.fromJson).toList();
    setState(() {
      _processedItems = result;
    });
    UiBenchHooks.notifyTableUpdated();
  }

  void _handleSort(String column) {
    setState(() {
      if (_sortColumn == column) {
        _sortAscending = !_sortAscending;
      } else {
        _sortColumn = column;
        _sortAscending = true;
      }
    });
    ref.read(todoProvider.notifier).resetVisibleCount();
    final state = ref.read(todoProvider);
    _scheduleProcess(
      items: state.items,
      filters: state.filters,
      sortColumn: _sortColumn,
      sortAscending: _sortAscending,
    );
  }

  Widget _buildHeader(String title, String columnId, double width) {
    final isSorted = _sortColumn == columnId;
    final theme = Theme.of(context);
    final headerBg = theme.colorScheme.surfaceContainerHighest;
    final border = theme.dividerColor;
    return GestureDetector(
      onTap: () => _handleSort(columnId),
      child: Container(
        width: width,
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: BoxDecoration(
          color: headerBg,
          border: Border(bottom: BorderSide(color: border)),
        ),
        child: Row(
          children: [
            Text(
              title,
              style: TextStyle(
                fontWeight: FontWeight.w600,
                fontSize: 14,
                color: theme.colorScheme.onSurface,
              ),
            ),
            if (isSorted)
              Icon(
                _sortAscending ? Icons.arrow_upward : Icons.arrow_downward,
                size: 16,
                color: theme.colorScheme.onSurfaceVariant,
              ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(todoProvider);

    ref.listen<TodoState>(todoProvider, (previous, next) {
      if (previous?.items == next.items && previous?.filters == next.filters) {
        return;
      }
      _scheduleProcess(
        items: next.items,
        filters: next.filters,
        sortColumn: _sortColumn,
        sortAscending: _sortAscending,
      );
    });

    final sortedItems = _processedItems;
    final displayCount = math.min(state.visibleCount, sortedItems.length);
    final allFilteredSelected =
        sortedItems.isNotEmpty &&
        sortedItems.every((item) => state.selectedIds.contains(item.id));
    final someFilteredSelected = sortedItems.any(
      (item) => state.selectedIds.contains(item.id),
    );
    final theme = Theme.of(context);
    final headerBg = theme.colorScheme.surfaceContainerHighest;
    final border = theme.dividerColor;
    final selectedBg = theme.colorScheme.primary.withValues(alpha: 0.12);

    return Container(
      decoration: BoxDecoration(
        border: Border.all(color: border),
      ),
      child: Column(
        children: [
          // ヘッダー
          Row(
            children: [
              // チェックボックス列
              Container(
                width: 50,
                padding: const EdgeInsets.symmetric(
                  horizontal: 16,
                  vertical: 12,
                ),
                decoration: BoxDecoration(
                  color: headerBg,
                  border: Border(
                    bottom: BorderSide(color: border),
                  ),
                ),
                child: Checkbox(
                  value: allFilteredSelected
                      ? true
                      : (someFilteredSelected ? null : false),
                  tristate: true,
                  onChanged: (value) {
                    final notifier = ref.read(todoProvider.notifier);
                    if (value == true) {
                      for (final item in sortedItems) {
                        if (!state.selectedIds.contains(item.id)) {
                          notifier.toggleSelection(item.id);
                        }
                      }
                    } else {
                      for (final item in sortedItems) {
                        if (state.selectedIds.contains(item.id)) {
                          notifier.toggleSelection(item.id);
                        }
                      }
                    }
                  },
                ),
              ),
              _buildHeader('ID', 'id', 80),
              _buildHeader('タイトル', 'title', 200),
              _buildHeader('説明', 'description', 300),
              _buildHeader('ステータス', 'status', 120),
              _buildHeader('優先度', 'priority', 100),
              _buildHeader('期限', 'dueDate', 120),
              _buildHeader('作成日時', 'createdAt', 160),
              _buildHeader('更新日時', 'updatedAt', 160),
              Container(
                width: 100,
                padding: const EdgeInsets.symmetric(
                  horizontal: 16,
                  vertical: 12,
                ),
                decoration: BoxDecoration(
                  color: headerBg,
                  border: Border(
                    bottom: BorderSide(color: border),
                  ),
                ),
                child: Text(
                  '操作',
                  style: TextStyle(
                    fontWeight: FontWeight.w600,
                    fontSize: 14,
                    color: theme.colorScheme.onSurface,
                  ),
                ),
              ),
            ],
          ),
          // テーブル本体
          Expanded(
            child: sortedItems.isEmpty
                ? const Center(child: Text('データがありません'))
                : ListView.builder(
                    controller: _scrollController,
                    itemCount: displayCount,
                    itemBuilder: (context, index) {
                      final item = sortedItems[index];
                      final isSelected = state.selectedIds.contains(item.id);
                      final dateFormat = DateFormat('yyyy-MM-dd');
                      final dateTimeFormat = DateFormat('yyyy-MM-dd HH:mm');

                      return InkWell(
                        onTap: () {
                          ref
                              .read(todoProvider.notifier)
                              .toggleSelection(item.id);
                        },
                        onDoubleTap: () => widget.onEdit(item),
                        child: Container(
                          color: isSelected ? selectedBg : null,
                          child: Row(
                            children: [
                              // チェックボックス
                              Container(
                                width: 50,
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 16,
                                  vertical: 8,
                                ),
                                child: Checkbox(
                                  value: isSelected,
                                  onChanged: (_) {
                                    ref
                                        .read(todoProvider.notifier)
                                        .toggleSelection(item.id);
                                  },
                                ),
                              ),
                              // ID
                              SizedBox(
                                width: 80,
                                child: Padding(
                                  padding: const EdgeInsets.symmetric(
                                    horizontal: 16,
                                    vertical: 8,
                                  ),
                                  child: Text(
                                    item.id.toString(),
                                    style: const TextStyle(fontSize: 14),
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                ),
                              ),
                              // タイトル
                              SizedBox(
                                width: 200,
                                child: Padding(
                                  padding: const EdgeInsets.symmetric(
                                    horizontal: 16,
                                    vertical: 8,
                                  ),
                                  child: Text(
                                    item.title,
                                    style: const TextStyle(fontSize: 14),
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                ),
                              ),
                              // 説明
                              SizedBox(
                                width: 300,
                                child: Padding(
                                  padding: const EdgeInsets.symmetric(
                                    horizontal: 16,
                                    vertical: 8,
                                  ),
                                  child: Text(
                                    item.description.isEmpty
                                        ? '-'
                                        : item.description,
                                    style: const TextStyle(fontSize: 14),
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                ),
                              ),
                              // ステータス
                              SizedBox(
                                width: 120,
                                child: Padding(
                                  padding: const EdgeInsets.symmetric(
                                    horizontal: 16,
                                    vertical: 8,
                                  ),
                                  child: Text(
                                    item.status,
                                    style: const TextStyle(fontSize: 14),
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                ),
                              ),
                              // 優先度
                              SizedBox(
                                width: 100,
                                child: Padding(
                                  padding: const EdgeInsets.symmetric(
                                    horizontal: 16,
                                    vertical: 8,
                                  ),
                                  child: Text(
                                    item.priority,
                                    style: const TextStyle(fontSize: 14),
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                ),
                              ),
                              // 期限
                              SizedBox(
                                width: 120,
                                child: Padding(
                                  padding: const EdgeInsets.symmetric(
                                    horizontal: 16,
                                    vertical: 8,
                                  ),
                                  child: Text(
                                    item.dueDate != null
                                        ? dateFormat.format(
                                            DateTime.parse(item.dueDate!),
                                          )
                                        : '-',
                                    style: const TextStyle(fontSize: 14),
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                ),
                              ),
                              // 作成日時
                              SizedBox(
                                width: 160,
                                child: Padding(
                                  padding: const EdgeInsets.symmetric(
                                    horizontal: 16,
                                    vertical: 8,
                                  ),
                                  child: Text(
                                    dateTimeFormat.format(
                                      DateTime.parse(item.createdAt),
                                    ),
                                    style: const TextStyle(fontSize: 14),
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                ),
                              ),
                              // 更新日時
                              SizedBox(
                                width: 160,
                                child: Padding(
                                  padding: const EdgeInsets.symmetric(
                                    horizontal: 16,
                                    vertical: 8,
                                  ),
                                  child: Text(
                                    dateTimeFormat.format(
                                      DateTime.parse(item.updatedAt),
                                    ),
                                    style: const TextStyle(fontSize: 14),
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                ),
                              ),
                              // 操作
                              SizedBox(
                                width: 100,
                                child: Padding(
                                  padding: const EdgeInsets.symmetric(
                                    horizontal: 16,
                                    vertical: 8,
                                  ),
                                  child: OutlinedButton(
                                    onPressed: () => widget.onEdit(item),
                                    style: OutlinedButton.styleFrom(
                                      padding: const EdgeInsets.symmetric(
                                        horizontal: 12,
                                        vertical: 4,
                                      ),
                                    ),
                                    child: const Text(
                                      '編集',
                                      style: TextStyle(fontSize: 12),
                                    ),
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                      );
                    },
                  ),
          ),
        ],
      ),
    );
  }
}
