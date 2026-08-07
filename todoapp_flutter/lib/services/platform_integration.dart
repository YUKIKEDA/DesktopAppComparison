import 'dart:convert';
import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';
import 'package:local_notifier/local_notifier.dart';
import 'package:tray_manager/tray_manager.dart';
import 'package:window_manager/window_manager.dart';

import '../models/todo_item.dart';

bool get isDesktopPlatform =>
    !kIsWeb && (Platform.isWindows || Platform.isLinux || Platform.isMacOS);

/// Desktop platform helpers: clipboard, notifications, system tray.
class PlatformIntegration {
  PlatformIntegration._();

  static bool _notifierReady = false;
  static bool _trayReady = false;
  static bool _quitting = false;

  static bool get isQuitting => _quitting;

  static Future<void> initNotifications() async {
    if (!isDesktopPlatform || _notifierReady) return;
    try {
      await localNotifier.setup(appName: 'Todo App');
      _notifierReady = true;
    } catch (e) {
      debugPrint('localNotifier setup failed: $e');
    }
  }

  static Future<void> showNotification(String body) async {
    if (!isDesktopPlatform) return;
    await initNotifications();
    try {
      final notification = LocalNotification(
        title: 'Todo App',
        body: body,
      );
      await notification.show();
    } catch (e) {
      debugPrint('Notification failed: $e');
    }
  }

  static Future<void> copyTodosAsJson(List<TodoItem> items) async {
    final json = const JsonEncoder.withIndent('  ').convert(
      items.map((e) => e.toJson()).toList(),
    );
    await Clipboard.setData(ClipboardData(text: json));
  }

  static List<String> jsonPathsFromArgs(List<String> args) {
    return args
        .where((a) => a.toLowerCase().endsWith('.json'))
        .where((a) => File(a).existsSync())
        .toList();
  }

  static Future<void> initTray({
    required Future<void> Function() onShow,
    required Future<void> Function() onQuit,
  }) async {
    if (!isDesktopPlatform || _trayReady) return;
    try {
      final iconPath = Platform.isWindows
          ? 'assets/tray_icon.ico'
          : 'assets/tray_icon.png';
      await trayManager.setIcon(iconPath);
      await trayManager.setToolTip('Todo App');
      await trayManager.setContextMenu(
        Menu(
          items: [
            MenuItem(key: 'show', label: '表示'),
            MenuItem.separator(),
            MenuItem(key: 'quit', label: '終了'),
          ],
        ),
      );
      trayManager.addListener(_TrayCallbacks(onShow: onShow, onQuit: onQuit));
      _trayReady = true;
    } catch (e) {
      debugPrint('Tray init failed: $e');
    }
  }

  static Future<void> hideToTray() async {
    if (!isDesktopPlatform) return;
    try {
      await windowManager.hide();
    } catch (e) {
      debugPrint('hideToTray failed: $e');
    }
  }

  static Future<void> showFromTray() async {
    if (!isDesktopPlatform) return;
    try {
      await windowManager.show();
      await windowManager.focus();
    } catch (e) {
      debugPrint('showFromTray failed: $e');
    }
  }

  static Future<void> quitApp() async {
    if (_quitting) return;
    _quitting = true;
    try {
      if (_trayReady) {
        await trayManager.destroy();
      }
      await windowManager.setPreventClose(false);
      await windowManager.destroy();
    } catch (e) {
      debugPrint('quitApp failed: $e');
      exit(0);
    }
  }
}

class _TrayCallbacks with TrayListener {
  _TrayCallbacks({required this.onShow, required this.onQuit});

  final Future<void> Function() onShow;
  final Future<void> Function() onQuit;

  @override
  void onTrayIconMouseDown() {
    onShow();
  }

  @override
  void onTrayIconRightMouseDown() {
    trayManager.popUpContextMenu();
  }

  @override
  void onTrayMenuItemClick(MenuItem menuItem) {
    switch (menuItem.key) {
      case 'show':
        onShow();
        break;
      case 'quit':
        onQuit();
        break;
    }
  }
}
