import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/todo_provider.dart';
import '../services/data_service.dart';
import '../models/todo_item.dart';
import '../models/project_data.dart';

class Toolbar extends ConsumerStatefulWidget {
  final Function(TodoItem?) onEditItem;

  const Toolbar({
    super.key,
    required this.onEditItem,
  });

  @override
  ConsumerState<Toolbar> createState() => _ToolbarState();
}

class _ToolbarState extends ConsumerState<Toolbar> {
  @override
  void initState() {
    super.initState();
    // キーボードショートカットを設定
    _setupKeyboardShortcuts();
  }

  void _setupKeyboardShortcuts() {
    // この実装は後でmain.dartで行う
  }

  Future<void> _handleAdd() async {
    widget.onEditItem(null);
  }

  Future<void> _handleDelete() async {
    final state = ref.read(todoProvider);
    if (state.selectedIds.isEmpty) return;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('削除確認'),
        content: Text('${state.selectedIds.length}件のアイテムを削除しますか？'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('キャンセル'),
          ),
          TextButton(
            onPressed: () => Navigator.of(context).pop(true),
            style: TextButton.styleFrom(foregroundColor: Colors.red),
            child: const Text('削除'),
          ),
        ],
      ),
    );

    if (confirmed == true) {
      final notifier = ref.read(todoProvider.notifier);
      notifier.deleteItems(state.selectedIds.toList());
      await notifier.saveData();
    }
  }

  Future<void> _handleExport() async {
    try {
      final state = ref.read(todoProvider);
      final projectData = ProjectData(items: state.items);
      await DataService.exportData(projectData);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('エクスポートに失敗しました: $e')),
        );
      }
    }
  }

  Future<void> _handleImport() async {
    try {
      final data = await DataService.importData();
      if (data != null) {
        final notifier = ref.read(todoProvider.notifier);
        notifier.setItems(data.items);
        await notifier.saveData();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('インポートに失敗しました: $e')),
        );
      }
    }
  }

  Future<void> _handleOpenDataFolder() async {
    try {
      await DataService.openDataFolder();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('フォルダを開けませんでした: $e')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(todoProvider);
    final selectedCount = state.selectedIds.length;

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border(bottom: BorderSide(color: Colors.grey.shade200)),
      ),
      child: Row(
        children: [
          ElevatedButton.icon(
            onPressed: _handleAdd,
            icon: const Icon(Icons.add, size: 18),
            label: const Text('新しいアイテム'),
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.blue.shade600,
              foregroundColor: Colors.white,
            ),
          ),
          const SizedBox(width: 8),
          ElevatedButton.icon(
            onPressed: selectedCount > 0 ? _handleDelete : null,
            icon: const Icon(Icons.delete, size: 18),
            label: Text('削除 ($selectedCount)'),
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.red.shade600,
              foregroundColor: Colors.white,
            ),
          ),
          const Spacer(),
          OutlinedButton(
            onPressed: _handleExport,
            child: const Text('エクスポート'),
          ),
          const SizedBox(width: 8),
          OutlinedButton(
            onPressed: _handleImport,
            child: const Text('インポート'),
          ),
          const SizedBox(width: 8),
          OutlinedButton(
            onPressed: _handleOpenDataFolder,
            child: const Text('データフォルダを開く'),
          ),
        ],
      ),
    );
  }
}

