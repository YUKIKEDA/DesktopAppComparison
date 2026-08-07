import 'dart:async';
import 'dart:io';
import 'package:desktop_drop/desktop_drop.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:window_manager/window_manager.dart';
import 'providers/todo_provider.dart';
import 'services/data_service.dart';
import 'widgets/toolbar.dart';
import 'widgets/filter_bar.dart';
import 'widgets/todo_table.dart';
import 'widgets/todo_form.dart';
import 'widgets/dialog.dart';
import 'models/todo_item.dart';

// Multi-window: Flutter desktop has no first-class multi-window API.
// Skipped (desktop_multi_window is fragile); parent marks as FW limitation.

bool get _isDesktop =>
    !kIsWeb && (Platform.isWindows || Platform.isLinux || Platform.isMacOS);

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  if (_isDesktop) {
    await windowManager.ensureInitialized();

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

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Todo App',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.blue),
        useMaterial3: true,
      ),
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
    await _persistWindowBounds();
    await windowManager.setPreventClose(false);
    await windowManager.close();
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

  Future<void> _importFromDroppedPath(String path) async {
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
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('JSON をインポートしました')),
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
              ref.read(todoProvider.notifier).saveData();
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
              backgroundColor: Colors.grey.shade50,
              body: Stack(
                children: [
                  Column(
                    children: [
                      Toolbar(onEditItem: _handleEditItem),
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
