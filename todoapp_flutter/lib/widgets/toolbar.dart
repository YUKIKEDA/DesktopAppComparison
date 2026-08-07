import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/todo_provider.dart';
import '../providers/theme_provider.dart';
import '../services/data_service.dart';
import '../services/platform_integration.dart';
import '../models/todo_item.dart';
import '../models/project_data.dart';
import '../theme/app_theme.dart';

class Toolbar extends ConsumerStatefulWidget {
  final Function(TodoItem?) onEditItem;
  final Future<void> Function()? onImportSuccess;

  const Toolbar({
    super.key,
    required this.onEditItem,
    this.onImportSuccess,
  });

  @override
  ConsumerState<Toolbar> createState() => _ToolbarState();
}

class _ToolbarState extends ConsumerState<Toolbar> {
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
            style: TextButton.styleFrom(foregroundColor: AppColors.brandRed),
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

  Future<void> _handleCopy() async {
    final state = ref.read(todoProvider);
    if (state.selectedIds.isEmpty) return;
    final selected = state.items
        .where((item) => state.selectedIds.contains(item.id))
        .toList();
    try {
      await PlatformIntegration.copyTodosAsJson(selected);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('${selected.length}件をクリップボードにコピーしました')),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('コピーに失敗しました: $e')),
        );
      }
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
        await widget.onImportSuccess?.call();
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('インポートしました')),
          );
        }
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
    final themeMode = ref.watch(themeProvider);
    final selectedCount = state.selectedIds.length;
    final theme = Theme.of(context);
    final isDark = themeMode == ThemeMode.dark;

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: theme.colorScheme.surface,
        border: Border(bottom: BorderSide(color: theme.dividerColor)),
      ),
      child: Wrap(
        spacing: 8,
        runSpacing: 8,
        crossAxisAlignment: WrapCrossAlignment.center,
        children: [
          ElevatedButton.icon(
            onPressed: _handleAdd,
            icon: const Icon(Icons.add, size: 18),
            label: const Text('新しいアイテム'),
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.brandBlue,
              foregroundColor: Colors.white,
            ),
          ),
          ElevatedButton.icon(
            onPressed: selectedCount > 0 ? _handleDelete : null,
            icon: const Icon(Icons.delete, size: 18),
            label: Text('削除 ($selectedCount)'),
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.brandRed,
              foregroundColor: Colors.white,
              disabledBackgroundColor:
                  AppColors.brandRed.withValues(alpha: 0.4),
              disabledForegroundColor: Colors.white70,
            ),
          ),
          OutlinedButton.icon(
            onPressed: selectedCount > 0 ? _handleCopy : null,
            icon: const Icon(Icons.copy, size: 18),
            label: const Text('コピー'),
          ),
          OutlinedButton(
            onPressed: _handleExport,
            child: const Text('エクスポート'),
          ),
          OutlinedButton(
            onPressed: _handleImport,
            child: const Text('インポート'),
          ),
          OutlinedButton(
            onPressed: _handleOpenDataFolder,
            child: const Text('データフォルダを開く'),
          ),
          OutlinedButton.icon(
            onPressed: () => ref.read(themeProvider.notifier).toggle(),
            icon: Icon(isDark ? Icons.light_mode : Icons.dark_mode, size: 18),
            label: Text(isDark ? 'テーマ: ダーク' : 'テーマ: ライト'),
          ),
        ],
      ),
    );
  }
}
