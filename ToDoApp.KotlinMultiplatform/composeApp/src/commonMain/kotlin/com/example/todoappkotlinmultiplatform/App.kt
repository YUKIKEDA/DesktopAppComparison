package com.example.todoappkotlinmultiplatform

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.focusable
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.input.key.*
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.compose.runtime.CompositionLocalProvider
import com.example.todoappkotlinmultiplatform.ui.components.*
import com.example.todoappkotlinmultiplatform.model.TodoItem
import com.example.todoappkotlinmultiplatform.viewmodel.TodoViewModel

@Composable
fun App() {
    MaterialTheme {
        val dataService = remember { getDataService() }
        val viewModel: TodoViewModel = viewModel { TodoViewModel(dataService) }
        
        val items by viewModel.items.collectAsState()
        val selectedIds by viewModel.selectedIds.collectAsState()
        val filters by viewModel.filters.collectAsState()
        val sorts by viewModel.sorts.collectAsState()
        val isLoading by viewModel.isLoading.collectAsState()
        
        var editingItem by remember { mutableStateOf<TodoItem?>(null) }
        var isDialogOpen by remember { mutableStateOf(false) }
        
        val filteredItems = remember(items, filters, sorts) {
            viewModel.getFilteredItems()
        }
        
        Column(
            modifier = Modifier
                .fillMaxSize()
                .onKeyEvent { keyEvent ->
                    if (keyEvent.type == KeyEventType.KeyDown) {
                        when {
                            (keyEvent.isCtrlPressed || keyEvent.isMetaPressed) && keyEvent.key == Key.N -> {
                                editingItem = null
                                isDialogOpen = true
                                true
                            }
                            (keyEvent.isCtrlPressed || keyEvent.isMetaPressed) && keyEvent.key == Key.S -> {
                                viewModel.saveData()
                                true
                            }
                            (keyEvent.isCtrlPressed || keyEvent.isMetaPressed) && keyEvent.key == Key.F -> {
                                // Focus search input - handled by FilterBar
                                false
                            }
                            keyEvent.key == Key.Delete && selectedIds.isNotEmpty() -> {
                                viewModel.deleteItems(selectedIds.toList())
                                true
                            }
                            else -> false
                        }
                    } else {
                        false
                    }
                }
                .focusable()
        ) {
            Toolbar(
                selectedCount = selectedIds.size,
                onAddClick = {
                    editingItem = null
                    isDialogOpen = true
                },
                onDeleteClick = {
                    if (selectedIds.isNotEmpty()) {
                        viewModel.deleteItems(selectedIds.toList())
                    }
                },
                onExportClick = {
                    viewModel.exportData()
                },
                onImportClick = {
                    viewModel.importData()
                },
                onOpenDataFolderClick = {
                    viewModel.openDataFolder()
                }
            )
            
            FilterBar(
                filters = filters,
                onFiltersChange = { viewModel.setFilters(it) }
            )
            
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .weight(1f)
                    .padding(8.dp)
            ) {
                if (isLoading) {
                    CircularProgressIndicator(
                        modifier = Modifier.align(androidx.compose.ui.Alignment.Center)
                    )
                } else {
                    TodoTable(
                        items = filteredItems,
                        selectedIds = selectedIds,
                        sorts = sorts,
                        onToggleSelection = { viewModel.toggleSelection(it) },
                        onSelectAll = { viewModel.selectAll(it) },
                        onDeselectAll = { viewModel.deselectAll(it) },
                        onEdit = { item ->
                            editingItem = item
                            isDialogOpen = true
                        },
                        onSortClick = { columnId ->
                            viewModel.toggleSort(columnId)
                        }
                    )
                }
            }
            
            AppDialog(
                open = isDialogOpen,
                onDismiss = {
                    isDialogOpen = false
                    editingItem = null
                },
                title = if (editingItem != null) "アイテムを編集" else "新しいアイテムを追加"
            ) {
                TodoForm(
                    item = editingItem,
                    onSubmit = { itemInput ->
                        if (editingItem != null) {
                            viewModel.updateItem(editingItem!!.id, itemInput)
                        } else {
                            viewModel.addItem(itemInput)
                        }
                        isDialogOpen = false
                        editingItem = null
                    },
                    onCancel = {
                        isDialogOpen = false
                        editingItem = null
                    }
                )
            }
        }
    }
}

// This will be provided by platform-specific code
expect fun getDataService(): com.example.todoappkotlinmultiplatform.model.IDataService
