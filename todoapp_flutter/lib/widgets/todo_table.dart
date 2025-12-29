import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../providers/todo_provider.dart';
import '../models/todo_item.dart';
import '../models/filter_config.dart';

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

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  List<TodoItem> _getFilteredItems(
    List<TodoItem> items,
    List<FilterConfig> filters,
  ) {
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

  List<TodoItem> _getSortedItems(List<TodoItem> items) {
    if (_sortColumn == null) return items;

    final sorted = List<TodoItem>.from(items);
    sorted.sort((a, b) {
      int comparison = 0;
      switch (_sortColumn) {
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
          final aDate = a.dueDate != null
              ? DateTime.tryParse(a.dueDate!)
              : null;
          final bDate = b.dueDate != null
              ? DateTime.tryParse(b.dueDate!)
              : null;
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
      return _sortAscending ? comparison : -comparison;
    });

    return sorted;
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
  }

  Widget _buildHeader(String title, String columnId, double width) {
    final isSorted = _sortColumn == columnId;
    return GestureDetector(
      onTap: () => _handleSort(columnId),
      child: Container(
        width: width,
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: BoxDecoration(
          color: Colors.grey.shade50,
          border: Border(bottom: BorderSide(color: Colors.grey.shade200)),
        ),
        child: Row(
          children: [
            Text(
              title,
              style: const TextStyle(
                fontWeight: FontWeight.w600,
                fontSize: 14,
                color: Colors.black87,
              ),
            ),
            if (isSorted)
              Icon(
                _sortAscending ? Icons.arrow_upward : Icons.arrow_downward,
                size: 16,
                color: Colors.grey.shade700,
              ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(todoProvider);
    final filteredItems = _getFilteredItems(state.items, state.filters);
    final sortedItems = _getSortedItems(filteredItems);
    final allFilteredSelected =
        sortedItems.isNotEmpty &&
        sortedItems.every((item) => state.selectedIds.contains(item.id));
    final someFilteredSelected = sortedItems.any(
      (item) => state.selectedIds.contains(item.id),
    );

    return Container(
      decoration: BoxDecoration(
        border: Border.all(color: Colors.grey.shade200),
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
                  color: Colors.grey.shade50,
                  border: Border(
                    bottom: BorderSide(color: Colors.grey.shade200),
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
                  color: Colors.grey.shade50,
                  border: Border(
                    bottom: BorderSide(color: Colors.grey.shade200),
                  ),
                ),
                child: const Text(
                  '操作',
                  style: TextStyle(
                    fontWeight: FontWeight.w600,
                    fontSize: 14,
                    color: Colors.black87,
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
                    itemCount: sortedItems.length,
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
                          color: isSelected ? Colors.blue.shade50 : null,
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
