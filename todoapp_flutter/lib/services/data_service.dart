import 'dart:convert';
import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:path_provider/path_provider.dart';
import 'package:file_picker/file_picker.dart';
import 'package:open_filex/open_filex.dart';
import '../models/project_data.dart';

class DataService {
  static const String _dataFileName = 'project.json';

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

  /// データを読み込む
  static Future<ProjectData> loadData() async {
    try {
      final file = await _getDataFile();
      if (await file.exists()) {
        final content = await file.readAsString();
        final json = jsonDecode(content) as Map<String, dynamic>;
        return ProjectData.fromJson(json);
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

  /// データをインポートする
  static Future<ProjectData?> importData() async {
    try {
      final result = await FilePicker.platform.pickFiles(
        type: FileType.custom,
        allowedExtensions: ['json'],
        dialogTitle: 'データをインポート',
      );

      if (result != null && result.files.single.path != null) {
        final file = File(result.files.single.path!);
        final content = await file.readAsString();
        final json = jsonDecode(content) as Map<String, dynamic>;
        return ProjectData.fromJson(json);
      }
      return null;
    } catch (e) {
      debugPrint('Error importing data: $e');
      return null;
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
