import 'dart:convert';
import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:path_provider/path_provider.dart';
import 'package:file_picker/file_picker.dart';
import 'package:open_filex/open_filex.dart';
import '../models/project_data.dart';

class WindowBounds {
  final double x;
  final double y;
  final double width;
  final double height;

  const WindowBounds({
    required this.x,
    required this.y,
    required this.width,
    required this.height,
  });

  factory WindowBounds.fromJson(Map<String, dynamic> json) {
    return WindowBounds(
      x: (json['x'] as num).toDouble(),
      y: (json['y'] as num).toDouble(),
      width: (json['width'] as num).toDouble(),
      height: (json['height'] as num).toDouble(),
    );
  }

  Map<String, dynamic> toJson() => {
        'x': x,
        'y': y,
        'width': width,
        'height': height,
      };

  bool get isReasonable =>
      width >= 200 &&
      height >= 200 &&
      width <= 10000 &&
      height <= 10000 &&
      x > -5000 &&
      y > -5000 &&
      x < 10000 &&
      y < 10000;
}

class DataService {
  static const String _dataFileName = 'project.json';
  static const String _windowFileName = 'window.json';
  static const String _themeFileName = 'theme.json';

  /// データディレクトリのパスを取得
  static Future<Directory> _getDataDirectory() async {
    final appDocDir = await getApplicationDocumentsDirectory();
    final dataDir = Directory('${appDocDir.path}/todoapp_flutter/data');
    if (!await dataDir.exists()) {
      await dataDir.create(recursive: true);
    }
    return dataDir;
  }

  /// データファイルのパスを取得
  static Future<File> _getDataFile() async {
    final dataDir = await _getDataDirectory();
    return File('${dataDir.path}/$_dataFileName');
  }

  static Future<File> _getWindowFile() async {
    final dataDir = await _getDataDirectory();
    return File('${dataDir.path}/$_windowFileName');
  }

  static Future<File> _getThemeFile() async {
    final dataDir = await _getDataDirectory();
    return File('${dataDir.path}/$_themeFileName');
  }

  /// Load theme preference (`light` | `dark`). Defaults to light.
  static Future<String> loadTheme() async {
    try {
      final file = await _getThemeFile();
      if (!await file.exists()) {
        return 'light';
      }
      final content = await file.readAsString();
      final json = jsonDecode(content) as Map<String, dynamic>;
      final theme = json['theme'] as String?;
      if (theme == 'dark' || theme == 'light') {
        return theme!;
      }
      return 'light';
    } catch (e) {
      debugPrint('Error loading theme: $e');
      return 'light';
    }
  }

  /// Persist theme preference next to project.json.
  static Future<void> saveTheme(String theme) async {
    try {
      final normalized = theme == 'dark' ? 'dark' : 'light';
      final file = await _getThemeFile();
      await file.writeAsString(jsonEncode({'theme': normalized}));
    } catch (e) {
      debugPrint('Error saving theme: $e');
    }
  }

  /// JSON 文字列を ProjectData にパース（import 系で共有）
  static ProjectData parseProjectData(String content) {
    final json = jsonDecode(content) as Map<String, dynamic>;
    return ProjectData.fromJson(json);
  }

  /// データを読み込む
  static Future<ProjectData> loadData() async {
    try {
      final file = await _getDataFile();
      if (await file.exists()) {
        final content = await file.readAsString();
        return parseProjectData(content);
      }
      return ProjectData(items: []);
    } catch (e) {
      debugPrint('Error loading data: $e');
      return ProjectData(items: []);
    }
  }

  /// データを保存する
  static Future<void> saveData(ProjectData data) async {
    try {
      final file = await _getDataFile();
      final json = jsonEncode(data.toJson());
      await file.writeAsString(json);
    } catch (e) {
      debugPrint('Error saving data: $e');
      rethrow;
    }
  }

  /// データをエクスポートする
  static Future<void> exportData(ProjectData data) async {
    try {
      final result = await FilePicker.platform.saveFile(
        dialogTitle: 'データをエクスポート',
        fileName: 'project.json',
        type: FileType.custom,
        allowedExtensions: ['json'],
      );

      if (result != null) {
        final file = File(result);
        final json = jsonEncode(data.toJson());
        await file.writeAsString(json);
      }
    } catch (e) {
      debugPrint('Error exporting data: $e');
      rethrow;
    }
  }

  /// ファイルパスからデータをインポートする（DnD / ピッカーで共有）
  static Future<ProjectData?> importFromPath(String path) async {
    try {
      final file = File(path);
      if (!await file.exists()) {
        return null;
      }
      final content = await file.readAsString();
      return parseProjectData(content);
    } catch (e) {
      debugPrint('Error importing data from path: $e');
      return null;
    }
  }

  /// ファイルピッカーでデータをインポートする
  static Future<ProjectData?> importData() async {
    try {
      final result = await FilePicker.platform.pickFiles(
        type: FileType.custom,
        allowedExtensions: ['json'],
        dialogTitle: 'データをインポート',
      );

      if (result != null && result.files.single.path != null) {
        return importFromPath(result.files.single.path!);
      }
      return null;
    } catch (e) {
      debugPrint('Error importing data: $e');
      return null;
    }
  }

  /// ウィンドウ位置・サイズを読み込む
  static Future<WindowBounds?> loadWindowBounds() async {
    try {
      final file = await _getWindowFile();
      if (!await file.exists()) {
        return null;
      }
      final content = await file.readAsString();
      final json = jsonDecode(content) as Map<String, dynamic>;
      final bounds = WindowBounds.fromJson(json);
      if (!bounds.isReasonable) {
        return null;
      }
      return bounds;
    } catch (e) {
      debugPrint('Error loading window bounds: $e');
      return null;
    }
  }

  /// ウィンドウ位置・サイズを保存する
  static Future<void> saveWindowBounds(WindowBounds bounds) async {
    try {
      final file = await _getWindowFile();
      await file.writeAsString(jsonEncode(bounds.toJson()));
    } catch (e) {
      debugPrint('Error saving window bounds: $e');
    }
  }

  /// データフォルダを開く
  static Future<void> openDataFolder() async {
    try {
      final dataDir = await _getDataDirectory();
      await OpenFilex.open(dataDir.path);
    } catch (e) {
      debugPrint('Error opening data folder: $e');
      rethrow;
    }
  }
}
