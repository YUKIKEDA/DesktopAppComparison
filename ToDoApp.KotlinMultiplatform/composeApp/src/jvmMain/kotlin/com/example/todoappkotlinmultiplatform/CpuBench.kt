package com.example.todoappkotlinmultiplatform

import com.example.todoappkotlinmultiplatform.model.FilterConfig
import com.example.todoappkotlinmultiplatform.model.FilterType
import com.example.todoappkotlinmultiplatform.model.FilterValue
import com.example.todoappkotlinmultiplatform.model.TodoPriority
import com.example.todoappkotlinmultiplatform.model.TodoStatus
import com.example.todoappkotlinmultiplatform.viewmodel.TodoItemInput
import com.example.todoappkotlinmultiplatform.viewmodel.TodoViewModel
import kotlinx.coroutines.delay
import kotlinx.coroutines.yield
import java.io.File

private const val PHASE_MS = 5000L
private const val PAGE_SIZE = 100
private const val FLAG = "--cpu-bench"
private const val PHASE_PREFIX = "--cpu-bench-phase="

fun cpuBenchEnabled(args: Array<String>): Boolean =
    args.any { it.equals(FLAG, ignoreCase = true) }

fun resolveCpuBenchPhasePath(args: Array<String>): String {
    val phaseArg = args.firstOrNull { it.startsWith(PHASE_PREFIX, ignoreCase = true) }
    if (phaseArg != null) {
        return phaseArg.substring(PHASE_PREFIX.length).trim().trim('"')
    }
    return File(System.getProperty("java.io.tmpdir"), "todo_cpu_bench_phase.txt").absolutePath
}

fun writeCpuBenchPhase(phasePath: String, phase: String) {
    File(phasePath).writeText(phase, Charsets.US_ASCII)
}

/** idle → add → scroll → filter → done */
suspend fun runCpuBench(viewModel: TodoViewModel, phasePath: String) {
    writeCpuBenchPhase(phasePath, "idle")
    delay(PHASE_MS)

    writeCpuBenchPhase(phasePath, "add")
    val addDeadline = System.currentTimeMillis() + PHASE_MS
    var n = 0
    while (System.currentTimeMillis() < addDeadline) {
        viewModel.addItem(
            TodoItemInput(
                title = "bench-$n",
                description = "",
                status = TodoStatus.未着手,
                priority = TodoPriority.中,
                dueDate = null,
                isCompleted = false
            )
        )
        n += 1
        delay(16)
    }

    // Let filter/sort catch up before scroll phase
    delay(250)

    writeCpuBenchPhase(phasePath, "scroll")
    val scrollDeadline = System.currentTimeMillis() + PHASE_MS
    while (System.currentTimeMillis() < scrollDeadline) {
        if (!viewModel.expandVisibleWindow(PAGE_SIZE)) {
            viewModel.resetVisibleCount()
        }
        delay(16)
    }

    writeCpuBenchPhase(phasePath, "filter")
    val filterDeadline = System.currentTimeMillis() + PHASE_MS
    var on = false
    while (System.currentTimeMillis() < filterDeadline) {
        if (on) {
            viewModel.setFilters(
                listOf(
                    FilterConfig("title", FilterType.TEXT, FilterValue.Text("bench")),
                    FilterConfig(
                        "status",
                        FilterType.SELECT,
                        FilterValue.Select(listOf(TodoStatus.未着手.name))
                    )
                )
            )
        } else {
            viewModel.setFilters(emptyList())
        }
        on = !on
        delay(16)
    }

    writeCpuBenchPhase(phasePath, "done")
}
