import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:flutter/scheduler.dart';

import '../models/filter_config.dart';
import '../providers/todo_provider.dart';
import 'data_service.dart';

const int uiBenchPageSize = 100;
const double uiBenchScrollSeconds = 3.0;
const int uiBenchFilterCycles = 10;
const String uiBenchFlag = '--ui-bench';
const String uiBenchOutPrefix = '--ui-bench-out=';
const String processStartMsPrefix = '--process-start-ms=';

/// Optional hook for [TodoTable] to signal filter/sort processing finished.
class UiBenchHooks {
  static Completer<void>? _tableUpdateCompleter;

  static void expectTableUpdate() {
    _tableUpdateCompleter = Completer<void>();
  }

  static void notifyTableUpdated() {
    final c = _tableUpdateCompleter;
    if (c != null && !c.isCompleted) {
      c.complete();
    }
    _tableUpdateCompleter = null;
  }

  static Future<void> waitForTableUpdate({
    Duration timeout = const Duration(seconds: 5),
  }) async {
    final c = _tableUpdateCompleter;
    if (c == null) {
      await Future<void>.delayed(const Duration(milliseconds: 16));
      return;
    }
    try {
      await c.future.timeout(timeout);
    } on TimeoutException {
      // Fall through — table may already be idle / hook missed.
    } finally {
      _tableUpdateCompleter = null;
    }
    await Future<void>.delayed(const Duration(milliseconds: 16));
  }
}

bool uiBenchEnabled(List<String> args) =>
    args.any((a) => a.toLowerCase() == uiBenchFlag);

String resolveUiBenchOutPath(List<String> args) {
  for (final a in args) {
    if (a.toLowerCase().startsWith(uiBenchOutPrefix)) {
      return a.substring(uiBenchOutPrefix.length).trim().replaceAll('"', '');
    }
  }
  return '${Directory.systemTemp.path}${Platform.pathSeparator}todo_ui_bench_result.json';
}

String? resolveUiBenchJsonPath(List<String> args) {
  for (final a in args) {
    if (a.toLowerCase().startsWith('--')) continue;
    if (a.toLowerCase().endsWith('.json') && File(a).existsSync()) {
      return a;
    }
  }
  return null;
}

/// Unix epoch ms of OS process creation (injected by Windows runner).
int? resolveProcessStartMs(List<String> args) {
  for (final a in args) {
    if (a.toLowerCase().startsWith(processStartMsPrefix)) {
      return int.tryParse(
        a.substring(processStartMsPrefix.length).trim().replaceAll('"', ''),
      );
    }
  }
  return null;
}

/// Startup seconds from OS process start when available; else [fallbackStart].
double measureStartupSeconds({
  required List<String> args,
  DateTime? fallbackStart,
}) {
  final processStartMs = resolveProcessStartMs(args);
  if (processStartMs != null) {
    return (DateTime.now().millisecondsSinceEpoch - processStartMs) / 1000.0;
  }
  if (fallbackStart != null) {
    return DateTime.now().difference(fallbackStart).inMicroseconds / 1e6;
  }
  return 0;
}

Future<void> endOfFrame() async {
  final completer = Completer<void>();
  SchedulerBinding.instance.scheduleFrame();
  SchedulerBinding.instance.addPostFrameCallback((_) {
    if (!completer.isCompleted) completer.complete();
  });
  try {
    await completer.future.timeout(const Duration(seconds: 2));
  } on TimeoutException {
    // ignore
  }
}

void writeUiBenchResult(String outPath, Map<String, num> metrics) {
  final file = File(outPath);
  file.parent.createSync(recursive: true);
  file.writeAsStringSync(jsonEncode(metrics), flush: true, encoding: utf8);
}

Future<void> runUiBench({
  required TodoNotifier notifier,
  required String outPath,
  required String jsonPath,
  required double startupS,
  required Future<void> Function() quit,
}) async {
  final renderStart = DateTime.now();
  UiBenchHooks.expectTableUpdate();
  final data = await DataService.importFromPath(jsonPath);
  if (data == null) {
    throw StateError('Failed to import $jsonPath');
  }
  notifier.setItems(data.items);
  await UiBenchHooks.waitForTableUpdate();
  final render1000S =
      DateTime.now().difference(renderStart).inMicroseconds / 1e6;

  final scrollFps = await _measureScrollFps(notifier);

  var filterTotalMs = 0.0;
  // Start with filters ON so the first setFilters is never a no-op after import.
  var on = true;
  for (var i = 0; i < uiBenchFilterCycles; i++) {
    final sw = Stopwatch()..start();
    UiBenchHooks.expectTableUpdate();
    if (on) {
      notifier.setFilters([
        FilterConfig(columnId: 'title', type: 'text', value: 'bench'),
        FilterConfig(columnId: 'status', type: 'select', value: ['未着手']),
      ]);
    } else {
      notifier.setFilters([]);
    }
    await UiBenchHooks.waitForTableUpdate();
    filterTotalMs += sw.elapsedMicroseconds / 1000.0;
    on = !on;
  }
  final filterResponseMs = filterTotalMs / uiBenchFilterCycles;

  writeUiBenchResult(outPath, {
    'startup_s': _round(startupS),
    'render_1000_s': _round(render1000S),
    'scroll_fps': _round(scrollFps),
    'filter_response_ms': _round(filterResponseMs),
  });

  await quit();
}

Future<double> _measureScrollFps(TodoNotifier notifier) async {
  var frames = 0;
  final stopwatch = Stopwatch()..start();
  final binding = SchedulerBinding.instance;
  final scrollEnd = Duration(
    microseconds: (uiBenchScrollSeconds * 1e6).round(),
  );

  void onFrame(Duration _) {
    frames++;
    if (stopwatch.elapsed < scrollEnd) {
      binding.scheduleFrameCallback(onFrame);
      binding.scheduleFrame();
    }
  }

  binding.scheduleFrameCallback(onFrame);
  binding.scheduleFrame();

  while (stopwatch.elapsed < scrollEnd) {
    if (!notifier.expandVisibleWindow(uiBenchPageSize)) {
      notifier.resetVisibleCount();
    }
    // Never await endOfFrame alone — it can hang when no frame is pending.
    await Future<void>.delayed(const Duration(milliseconds: 8));
  }

  final elapsedS = stopwatch.elapsedMicroseconds / 1e6;
  if (elapsedS <= 0) return 0;
  return frames / elapsedS;
}

double _round(double value) => double.parse(value.toStringAsFixed(2));
