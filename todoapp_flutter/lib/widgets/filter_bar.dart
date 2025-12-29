import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/todo_provider.dart';
import '../models/filter_config.dart';

class FilterBar extends ConsumerStatefulWidget {
  const FilterBar({super.key});

  @override
  ConsumerState<FilterBar> createState() => _FilterBarState();
}

class _FilterBarState extends ConsumerState<FilterBar> {
  final TextEditingController _searchController = TextEditingController();
  String? _statusFilter;
  String? _priorityFilter;

  @override
  void initState() {
    super.initState();
    _searchController.addListener(_applyFilters);
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  void _applyFilters() {
    final filters = <FilterConfig>[];

    // テキスト検索フィルタ
    if (_searchController.text.trim().isNotEmpty) {
      filters.add(
        FilterConfig(
          columnId: 'title',
          type: 'text',
          value: _searchController.text.trim(),
        ),
      );
    }

    // ステータスフィルタ
    if (_statusFilter != null && _statusFilter!.isNotEmpty) {
      filters.add(
        FilterConfig(
          columnId: 'status',
          type: 'select',
          value: [_statusFilter!],
        ),
      );
    }

    // 優先度フィルタ
    if (_priorityFilter != null && _priorityFilter!.isNotEmpty) {
      filters.add(
        FilterConfig(
          columnId: 'priority',
          type: 'select',
          value: [_priorityFilter!],
        ),
      );
    }

    ref.read(todoProvider.notifier).setFilters(filters);
  }

  void _clearFilters() {
    _searchController.clear();
    setState(() {
      _statusFilter = null;
      _priorityFilter = null;
    });
    ref.read(todoProvider.notifier).setFilters([]);
  }

  @override
  Widget build(BuildContext context) {
    final hasActiveFilters =
        _searchController.text.trim().isNotEmpty ||
        (_statusFilter != null && _statusFilter!.isNotEmpty) ||
        (_priorityFilter != null && _priorityFilter!.isNotEmpty);

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: Colors.grey.shade50,
        border: Border(bottom: BorderSide(color: Colors.grey.shade200)),
      ),
      child: Row(
        children: [
          Expanded(
            child: TextField(
              controller: _searchController,
              decoration: const InputDecoration(
                hintText: 'タイトル・説明で検索...',
                border: OutlineInputBorder(),
                contentPadding: EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 8,
                ),
                isDense: true,
              ),
            ),
          ),
          const SizedBox(width: 8),
          SizedBox(
            width: 160,
            child: DropdownButtonFormField<String>(
              key: ValueKey(_statusFilter),
              initialValue: _statusFilter,
              decoration: const InputDecoration(
                border: OutlineInputBorder(),
                contentPadding: EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 8,
                ),
                isDense: true,
              ),
              hint: const Text('すべてのステータス'),
              isExpanded: true,
              items: const [
                DropdownMenuItem(value: '未着手', child: Text('未着手')),
                DropdownMenuItem(value: '進行中', child: Text('進行中')),
                DropdownMenuItem(value: '完了', child: Text('完了')),
              ],
              onChanged: (value) {
                setState(() {
                  _statusFilter = value;
                });
                _applyFilters();
              },
            ),
          ),
          const SizedBox(width: 8),
          SizedBox(
            width: 160,
            child: DropdownButtonFormField<String>(
              key: ValueKey(_priorityFilter),
              initialValue: _priorityFilter,
              decoration: const InputDecoration(
                border: OutlineInputBorder(),
                contentPadding: EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 8,
                ),
                isDense: true,
              ),
              hint: const Text('すべての優先度'),
              isExpanded: true,
              items: const [
                DropdownMenuItem(value: '低', child: Text('低')),
                DropdownMenuItem(value: '中', child: Text('中')),
                DropdownMenuItem(value: '高', child: Text('高')),
              ],
              onChanged: (value) {
                setState(() {
                  _priorityFilter = value;
                });
                _applyFilters();
              },
            ),
          ),
          if (hasActiveFilters) ...[
            const SizedBox(width: 8),
            OutlinedButton(
              onPressed: _clearFilters,
              style: OutlinedButton.styleFrom(
                padding: const EdgeInsets.symmetric(
                  horizontal: 16,
                  vertical: 8,
                ),
              ),
              child: const Text('クリア'),
            ),
          ],
        ],
      ),
    );
  }
}
