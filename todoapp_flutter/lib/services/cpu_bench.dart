import 'dart:io';

import '../models/filter_config.dart';
import '../providers/todo_provider.dart';

const int cpuBenchPhaseMs = 5000;
const int cpuBenchPageSize = 100;
const String cpuBenchFlag = '--cpu-bench';
const String cpuBenchPhasePrefix = '--cpu-bench-phase=';

bool cpuBenchEnabled(List<String> args) =>
    args.any((a) => a.toLowerCase() == cpuBenchFlag);

String resolveCpuBenchPhasePath(List<String> args) {
  for (final a in args) {
    if (a.toLowerCase().startsWith(cpuBenchPhasePrefix)) {
      return a.substring(cpuBenchPhasePrefix.length).trim().replaceAll('"', '');
    }
  }
  return '${Directory.systemTemp.path}${Platform.pathSeparator}todo_cpu_bench_phase.txt';
}

void writeCpuBenchPhase(String phasePath, String phase) {
  File(phasePath).writeAsStringSync(phase, flush: true);
}

Future<void> _yieldToUi() async {
  await Future<void>.delayed(Duration.zero);
}

/// idle → add → scroll → filter → done
Future<void> runCpuBench({
  required TodoNotifier notifier,
  required String phasePath,
  required Future<void> Function() quit,
}) async {
  writeCpuBenchPhase(phasePath, 'idle');
  await Future<void>.delayed(const Duration(milliseconds: cpuBenchPhaseMs));

  writeCpuBenchPhase(phasePath, 'add');
  final addDeadline = DateTime.now().millisecondsSinceEpoch + cpuBenchPhaseMs;
  var n = 0;
  while (DateTime.now().millisecondsSinceEpoch < addDeadline) {
    notifier.addItem(
      title: 'bench-$n',
      description: '',
      status: '未着手',
      priority: '中',
    );
    n += 1;
    await _yieldToUi();
  }

  // Let filter/sort isolate catch up before scroll phase
  await Future<void>.delayed(const Duration(milliseconds: 250));

  writeCpuBenchPhase(phasePath, 'scroll');
  final scrollDeadline = DateTime.now().millisecondsSinceEpoch + cpuBenchPhaseMs;
  while (DateTime.now().millisecondsSinceEpoch < scrollDeadline) {
    if (!notifier.expandVisibleWindow(cpuBenchPageSize)) {
      notifier.resetVisibleCount();
    }
    await _yieldToUi();
  }

  writeCpuBenchPhase(phasePath, 'filter');
  final filterDeadline = DateTime.now().millisecondsSinceEpoch + cpuBenchPhaseMs;
  var on = false;
  while (DateTime.now().millisecondsSinceEpoch < filterDeadline) {
    if (on) {
      notifier.setFilters([
        FilterConfig(columnId: 'title', type: 'text', value: 'bench'),
        FilterConfig(columnId: 'status', type: 'select', value: ['未着手']),
      ]);
    } else {
      notifier.setFilters([]);
    }
    on = !on;
    await _yieldToUi();
  }

  writeCpuBenchPhase(phasePath, 'done');
  await quit();
}
