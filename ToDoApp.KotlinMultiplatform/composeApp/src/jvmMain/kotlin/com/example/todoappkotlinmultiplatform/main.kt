package com.example.todoappkotlinmultiplatform

import androidx.compose.runtime.*
import androidx.compose.ui.unit.DpSize
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Window
import androidx.compose.ui.window.WindowPosition
import androidx.compose.ui.window.WindowState
import androidx.compose.ui.window.application
import androidx.compose.ui.window.rememberWindowState
import com.example.todoappkotlinmultiplatform.service.DataService
import com.example.todoappkotlinmultiplatform.service.WindowGeometry
import com.example.todoappkotlinmultiplatform.viewmodel.TodoViewModel
import java.awt.datatransfer.DataFlavor
import java.awt.dnd.DnDConstants
import java.awt.dnd.DropTarget
import java.awt.dnd.DropTargetDropEvent
import java.io.File

fun main() = application {
    val dataService = remember { DataService() }
    val viewModel = remember { TodoViewModel(dataService) }

    val initialGeometry = remember { dataService.loadWindowGeometry() }
    val windowState = rememberWindowState(
        position = WindowPosition.Absolute(
            x = initialGeometry.x.dp,
            y = initialGeometry.y.dp
        ),
        size = DpSize(
            width = initialGeometry.width.dp,
            height = initialGeometry.height.dp
        )
    )

    var detailWindowIds by remember { mutableStateOf<Set<Int>>(emptySet()) }

    fun saveCurrentGeometry() {
        val position = windowState.position
        val size = windowState.size
        if (position is WindowPosition.Absolute) {
            dataService.saveWindowGeometry(
                WindowGeometry(
                    x = position.x.value.toInt(),
                    y = position.y.value.toInt(),
                    width = size.width.value.toInt().coerceAtLeast(100),
                    height = size.height.value.toInt().coerceAtLeast(100)
                )
            )
        } else {
            dataService.saveWindowGeometry(
                WindowGeometry(
                    width = size.width.value.toInt().coerceAtLeast(100),
                    height = size.height.value.toInt().coerceAtLeast(100)
                )
            )
        }
    }

    Window(
        onCloseRequest = {
            saveCurrentGeometry()
            exitApplication()
        },
        title = "Todo App",
        state = windowState
    ) {
        // Best-effort window transparency (~0.95). Compose Desktop support is limited.
        LaunchedEffect(Unit) {
            try {
                window.opacity = 0.95f
            } catch (_: Exception) {
                // Platform may not support translucent decorated windows
            }
        }

        DisposableEffect(Unit) {
            val dropTarget = object : DropTarget() {
                override fun drop(event: DropTargetDropEvent) {
                    try {
                        event.acceptDrop(DnDConstants.ACTION_COPY)
                        val transferable = event.transferable
                        if (transferable.isDataFlavorSupported(DataFlavor.javaFileListFlavor)) {
                            @Suppress("UNCHECKED_CAST")
                            val files = transferable.getTransferData(DataFlavor.javaFileListFlavor) as List<File>
                            files
                                .filter { it.extension.equals("json", ignoreCase = true) }
                                .forEach { viewModel.importFromPath(it.absolutePath) }
                        }
                        event.dropComplete(true)
                    } catch (e: Exception) {
                        e.printStackTrace()
                        event.dropComplete(false)
                    }
                }
            }
            window.contentPane.dropTarget = dropTarget
            onDispose {
                window.contentPane.dropTarget = null
            }
        }

        App(
            viewModel = viewModel,
            onOpenInNewWindow = { id ->
                detailWindowIds = detailWindowIds + id
            }
        )
    }

    detailWindowIds.forEach { itemId ->
        key(itemId) {
            val items by viewModel.items.collectAsState()
            val item = items.find { it.id == itemId }
            Window(
                onCloseRequest = {
                    detailWindowIds = detailWindowIds - itemId
                },
                title = item?.title?.takeIf { it.isNotBlank() } ?: "アイテム詳細",
                state = WindowState(
                    width = 560.dp,
                    height = 640.dp
                )
            ) {
                DetailWindowContent(
                    itemId = itemId,
                    viewModel = viewModel,
                    onClose = {
                        detailWindowIds = detailWindowIds - itemId
                    }
                )
            }
        }
    }
}
