import 'dart:async';
import 'package:desktop_drop/desktop_drop.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:window_manager/window_manager.dart';
import 'providers/todo_provider.dart';
import 'providers/theme_provider.dart';
import 'services/data_service.dart';
import 'services/platform_integration.dart';
import 'theme/app_theme.dart';
import 'widgets/toolbar.dart';
import 'widgets/filter_bar.dart';
import 'widgets/todo_table.dart';
import 'widgets/todo_form.dart';
import 'widgets/dialog.dart';
import 'models/todo_item.dart';

// Multi-window: Flutter desktop has no first-class multi-window API.
// Skipped (desktop_multi_window is fragile); parent marks as FW limitation.

bool get _isDesktop => isDesktopPlatform;

List<String> _startupArgs = const [];

Future<void> main(List<String> args) async {
  WidgetsFlutterBinding.ensureInitialized();
  _startupArgs = args;

  if (_isDesktop) {
    await windowManager.ensureInitialized();
    await PlatformIntegration.initNotifications();

    const defaultSize = Size(1280, 720);
    final saved = await DataService.loadWindowBounds();
    final size = saved != null
        ? Size(saved.width, saved.height)
        : defaultSize;

    final windowOptions = WindowOptions(
      size: size,
      center: saved == null,
      skipTaskbar: false,
      titleBarStyle: TitleBarStyle.normal,
    );

    windowManager.waitUntilReadyToShow(windowOptions, () async {
      if (saved != null) {
        await windowManager.setPosition(Offset(saved.x, saved.y));
      }
      // Best-effort transparency; may be limited on Windows Flutter.
      try {
        await windowManager.setOpacity(0.95);
      } catch (e) {
        debugPrint('setOpacity skipped: $e');
      }
      await windowManager.show();
      await windowManager.focus();
    });
  }

  runApp(const ProviderScope(child: MyApp()));
}

class MyApp extends ConsumerWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final themeMode = ref.watch(themeProvider);
    return MaterialApp(
      title: 'Todo App',
      theme: AppTheme.light(),
      darkTheme: AppTheme.dark(),
      themeMode: themeMode,
      home: const TodoAppPage(),
    );
  }
}

class TodoAppPage extends ConsumerStatefulWidget {
  const TodoAppPage({super.key});

  @override
  ConsumerState<TodoAppPage> createState() => _TodoAppPageState();
}

class _TodoAppPageState extends ConsumerState<TodoAppPage> with WindowListener {
  TodoItem? _editingItem;
  bool _isDialogOpen = false;
  Timer? _saveTimer;
  bool _dragging = false;

  @override
  void initState() {
    super.initState();
    _setupKeyboardShortcuts();
    if (_isDesktop) {
      windowManager.addListener(this);
      windowManager.setPreventClose(true);
      WidgetsBinding.instance.addPostFrameCallback((_) async {
        await PlatformIntegration.initTray(
          onShow: () => PlatformIntegration.showFromTray(),
          onQuit: () async {
            await _persistWindowBounds();
            await PlatformIntegration.quitApp();
          },
        );
      });
    }
    // 最初のフレームが構築された後にデータを読み込む
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadData();
    });
  }

  @override
  void dispose() {
    _saveTimer?.cancel();
    if (_isDesktop) {
      windowManager.removeListener(this);
    }
    super.dispose();
  }

  @override
  void onWindowClose() async {
    if (PlatformIntegration.isQuitting) {
      await _persistWindowBounds();
      await windowManager.setPreventClose(false);
      await windowManager.close();
      return;
    }
    await _persistWindowBounds();
    await PlatformIntegration.hideToTray();
  }

  Future<void> _persistWindowBounds() async {
    if (!_isDesktop) return;
    try {
      final position = await windowManager.getPosition();
      final size = await windowManager.getSize();
      await DataService.saveWindowBounds(
        WindowBounds(
          x: position.dx,
          y: position.dy,
          width: size.width,
          height: size.height,
        ),
      );
    } catch (e) {
      debugPrint('Failed to persist window bounds: $e');
    }
  }

  Future<void> _loadData() async {
    await ref.read(todoProvider.notifier).loadData();
    final paths = PlatformIntegration.jsonPathsFromArgs(_startupArgs);
    for (final path in paths) {
      await _importFromDroppedPath(path, fromArgv: true);
    }
  }

  void _setupKeyboardShortcuts() {
    // キーボードショートカットはShortcutsウィジェットで処理
  }

  void _handleEditItem(TodoItem? item) {
    setState(() {
      _editingItem = item;
      _isDialogOpen = true;
    });
  }

  Future<void> _handleSave({
    required String title,
    required String description,
    required String status,
    required String priority,
    String? dueDate,
  }) async {
    final notifier = ref.read(todoProvider.notifier);
    if (_editingItem != null) {
      notifier.updateItem(
        _editingItem!.id,
        title: title,
        description: description,
        status: status,
        priority: priority,
        dueDate: dueDate,
      );
    } else {
      notifier.addItem(
        title: title,
        description: description,
        status: status,
        priority: priority,
        dueDate: dueDate,
      );
    }
    setState(() {
      _isDialogOpen = false;
      _editingItem = null;
    });
    _scheduleAutoSave();
  }

  void _scheduleAutoSave() {
    _saveTimer?.cancel();
    _saveTimer = Timer(const Duration(seconds: 2), () {
      ref.read(todoProvider.notifier).saveData();
    });
  }

  Future<void> _manualSave() async {
    try {
      await ref.read(todoProvider.notifier).saveData();
      await PlatformIntegration.showNotification('保存しました');
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('保存しました')),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('保存に失敗しました: $e')),
        );
      }
    }
  }

  Future<void> _importFromDroppedPath(
    String path, {
    bool fromArgv = false,
  }) async {
    try {
      final data = await DataService.importFromPath(path);
      if (data == null) {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('インポートに失敗しました')),
          );
        }
        return;
      }
      final notifier = ref.read(todoProvider.notifier);
      notifier.setItems(data.items);
      await notifier.saveData();
      await PlatformIntegration.showNotification('インポートしました');
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              fromArgv
                  ? '起動引数の JSON をインポートしました'
                  : 'JSON をインポートしました',
            ),
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('インポートに失敗しました: $e')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    // アイテム変更を監視
    ref.listen<TodoState>(todoProvider, (previous, next) {
      if (previous != null &&
          previous.items.isNotEmpty &&
          next.items != previous.items) {
        _scheduleAutoSave();
      }
    });

    return Shortcuts(
      shortcuts: {
        LogicalKeySet(LogicalKeyboardKey.control, LogicalKeyboardKey.keyN):
            const _NewItemIntent(),
        LogicalKeySet(LogicalKeyboardKey.control, LogicalKeyboardKey.keyS):
            const _SaveIntent(),
        LogicalKeySet(LogicalKeyboardKey.delete): const _DeleteIntent(),
      },
      child: Actions(
        actions: {
          _NewItemIntent: CallbackAction<_NewItemIntent>(
            onInvoke: (_) => _handleEditItem(null),
          ),
          _SaveIntent: CallbackAction<_SaveIntent>(
            onInvoke: (_) {
              _manualSave();
              return null;
            },
          ),
          _DeleteIntent: CallbackAction<_DeleteIntent>(
            onInvoke: (_) {
              final state = ref.read(todoProvider);
              if (state.selectedIds.isNotEmpty) {
                ref
                    .read(todoProvider.notifier)
                    .deleteItems(state.selectedIds.toList());
                ref.read(todoProvider.notifier).saveData();
              }
              return null;
            },
          ),
        },
        child: Focus(
          autofocus: true,
          child: DropTarget(
            onDragEntered: (_) => setState(() => _dragging = true),
            onDragExited: (_) => setState(() => _dragging = false),
            onDragDone: (detail) async {
              setState(() => _dragging = false);
              final jsonPath = detail.files
                  .map((f) => f.path)
                  .where((p) => p.toLowerCase().endsWith('.json'))
                  .firstOrNull;
              if (jsonPath != null) {
                await _importFromDroppedPath(jsonPath);
              }
            },
            child: Scaffold(
              body: Stack(
                children: [
                  Column(
                    children: [
                      Toolbar(
                        onEditItem: _handleEditItem,
                        onImportSuccess: () => PlatformIntegration
                            .showNotification('インポートしました'),
                      ),
                      const FilterBar(),
                      Expanded(
                        child: TodoTable(
                          onEdit: (item) => _handleEditItem(item),
                        ),
                      ),
                    ],
                  ),
                  // ダイアログ
                  AppDialog(
                    open: _isDialogOpen,
                    title: _editingItem != null ? 'アイテムを編集' : '新しいアイテムを追加',
                    onClose: () {
                      setState(() {
                        _isDialogOpen = false;
                        _editingItem = null;
                      });
                    },
                    child: TodoForm(
                      item: _editingItem,
                      onSubmit: _handleSave,
                      onCancel: () {
                        setState(() {
                          _isDialogOpen = false;
                          _editingItem = null;
                        });
                      },
                    ),
                  ),
                  if (_dragging)
                    Positioned.fill(
                      child: IgnorePointer(
                        child: ColoredBox(
                          color: Colors.blue.withValues(alpha: 0.12),
                          child: const Center(
                            child: Text(
                              'JSON をドロップしてインポート',
                              style: TextStyle(
                                fontSize: 18,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ),
                        ),
                      ),
                    ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

// キーボードショートカット用のIntentクラス
class _NewItemIntent extends Intent {
  const _NewItemIntent();
}

class _SaveIntent extends Intent {
  const _SaveIntent();
}

class _DeleteIntent extends Intent {
  const _DeleteIntent();
}
