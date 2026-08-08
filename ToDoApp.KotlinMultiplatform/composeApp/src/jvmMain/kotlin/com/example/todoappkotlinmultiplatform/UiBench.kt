package com.example.todoappkotlinmultiplatform

import com.example.todoappkotlinmultiplatform.model.FilterConfig
import com.example.todoappkotlinmultiplatform.model.FilterType
import com.example.todoappkotlinmultiplatform.model.FilterValue
import com.example.todoappkotlinmultiplatform.model.TodoStatus
import com.example.todoappkotlinmultiplatform.viewmodel.TodoViewModel
import kotlinx.coroutines.delay
import java.io.File
import kotlin.system.measureNanoTime

private const val SCROLL_SEC = 3.0
private const val FILTER_CYCLES = 10
private const val FLAG = "--ui-bench"
private const val OUT_PREFIX = "--ui-bench-out="

fun uiBenchEnabled(args: Array<String>): Boolean =
    args.any { it.equals(FLAG, ignoreCase = true) }

fun resolveUiBenchOutPath(args: Array<String>): String {
    val outArg = args.firstOrNull { it.startsWith(OUT_PREFIX, ignoreCase = true) }
    if (outArg != null) {
        return outArg.substring(OUT_PREFIX.length).trim().trim('"')
    }
    return File(System.getProperty("java.io.tmpdir"), "todo_ui_bench_result.json").absolutePath
}

fun resolveUiBenchJsonPath(args: Array<String>): String? =
    args.firstOrNull { arg ->
        !arg.startsWith("--") && arg.endsWith(".json", ignoreCase = true) && File(arg).isFile
    }

/** OS process creation → now (java.base ProcessHandle; no java.management). */
fun processStartupSeconds(): Double {
    val start = ProcessHandle.current().info().startInstant().orElse(null)
        ?: return 0.0
    return java.time.Duration.between(start, java.time.Instant.now()).toMillis() / 1000.0
}

private fun round2(value: Double): Double =
    "%.2f".format(value).toDouble()

private fun writeUiBenchResult(outPath: String, metrics: Map<String, Double>) {
    val file = File(outPath)
    file.parentFile?.mkdirs()
    val body = buildString {
        append('{')
        metrics.entries.forEachIndexed { index, (k, v) ->
            if (index > 0) append(',')
            append('"').append(k).append("\":").append(v)
        }
        append('}')
    }
    file.writeText(body, Charsets.UTF_8)
}

suspend fun runUiBench(
    viewModel: TodoViewModel,
    outPath: String,
    jsonPath: String,
    startupS: Double,
) {
    val render1000S = measureNanoTime {
        viewModel.importFromPathSuspend(jsonPath)
        repeat(100) {
            viewModel.awaitFiltersApplied()
            if (viewModel.filteredItems.value.isNotEmpty()) return@measureNanoTime
            delay(16)
        }
    } / 1_000_000_000.0

    val scrollFps = measureScrollFps(viewModel)

    var filterTotalMs = 0.0
    var on = false
    repeat(FILTER_CYCLES) {
        val elapsedMs = measureNanoTime {
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
            viewModel.awaitFiltersApplied()
            delay(1)
        } / 1_000_000.0
        filterTotalMs += elapsedMs
        on = !on
    }
    val filterResponseMs = filterTotalMs / FILTER_CYCLES

    writeUiBenchResult(
        outPath,
        mapOf(
            "startup_s" to round2(startupS),
            "render_1000_s" to round2(render1000S),
            "scroll_fps" to round2(scrollFps),
            "filter_response_ms" to round2(filterResponseMs),
        )
    )
}

/** Pace load-more at ~60Hz; count ticks (Compose frame clock is unreliable in this bench path). */
private suspend fun measureScrollFps(viewModel: TodoViewModel): Double {
    val pageSize = TodoViewModel.PAGE_SIZE
    var frames = 0
    val startNs = System.nanoTime()
    val deadlineNs = startNs + (SCROLL_SEC * 1_000_000_000).toLong()

    while (System.nanoTime() < deadlineNs) {
        frames++
        if (!viewModel.expandVisibleWindow(pageSize)) {
            viewModel.resetVisibleCount()
        }
        delay(16)
    }

    val elapsedS = (System.nanoTime() - startNs) / 1_000_000_000.0
    return if (elapsedS <= 0.0) 0.0 else frames / elapsedS
}
