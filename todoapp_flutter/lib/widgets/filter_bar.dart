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

    if (_searchController.text.trim().isNotEmpty) {
      filters.add(
        FilterConfig(
          columnId: 'title',
          type: 'text',
          value: _searchController.text.trim(),
        ),
      );
    }

    if (_statusFilter != null && _statusFilter!.isNotEmpty) {
      filters.add(
        FilterConfig(
          columnId: 'status',
          type: 'select',
          value: [_statusFilter!],
        ),
      );
    }

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
    final theme = Theme.of(context);

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: theme.colorScheme.surfaceContainerHighest,
        border: Border(bottom: BorderSide(color: theme.dividerColor)),
      ),
      child: Wrap(
        spacing: 8,
        runSpacing: 8,
        crossAxisAlignment: WrapCrossAlignment.center,
        children: [
          ConstrainedBox(
            constraints: const BoxConstraints(minWidth: 180, maxWidth: 420),
            child: SizedBox(
              width: 320,
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
          ),
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
          if (hasActiveFilters)
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
      ),
    );
  }
}
