package com.example.todoappkotlinmultiplatform.ui.components

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.example.todoappkotlinmultiplatform.model.FilterConfig
import com.example.todoappkotlinmultiplatform.model.FilterType
import com.example.todoappkotlinmultiplatform.model.FilterValue
import com.example.todoappkotlinmultiplatform.model.TodoPriority
import com.example.todoappkotlinmultiplatform.model.TodoStatus

@OptIn(ExperimentalLayoutApi::class)
@Composable
fun FilterBar(
    filters: List<FilterConfig>,
    onFiltersChange: (List<FilterConfig>) -> Unit,
    modifier: Modifier = Modifier
) {
    var searchText by remember { mutableStateOf("") }
    var statusFilter by remember { mutableStateOf<List<String>>(emptyList()) }
    var priorityFilter by remember { mutableStateOf<List<String>>(emptyList()) }

    var initialized by remember { mutableStateOf(false) }
    LaunchedEffect(filters) {
        if (!initialized) {
            val textFilter = filters.find { it.type == FilterType.TEXT && (it.columnId == "title" || it.columnId == "description") }
            if (textFilter != null && textFilter.value is FilterValue.Text) {
                searchText = textFilter.value.value
            }

            val statusFilterConfig = filters.find { it.type == FilterType.SELECT && it.columnId == "status" }
            if (statusFilterConfig != null && statusFilterConfig.value is FilterValue.Select) {
                statusFilter = statusFilterConfig.value.values
            }

            val priorityFilterConfig = filters.find { it.type == FilterType.SELECT && it.columnId == "priority" }
            if (priorityFilterConfig != null && priorityFilterConfig.value is FilterValue.Select) {
                priorityFilter = priorityFilterConfig.value.values
            }
            initialized = true
        }
    }

    LaunchedEffect(searchText, statusFilter, priorityFilter) {
        val newFilters = mutableListOf<FilterConfig>()

        if (searchText.isNotBlank()) {
            newFilters.add(
                FilterConfig(
                    columnId = "title",
                    type = FilterType.TEXT,
                    value = FilterValue.Text(searchText.trim())
                )
            )
        }

        if (statusFilter.isNotEmpty()) {
            newFilters.add(
                FilterConfig(
                    columnId = "status",
                    type = FilterType.SELECT,
                    value = FilterValue.Select(statusFilter)
                )
            )
        }

        if (priorityFilter.isNotEmpty()) {
            newFilters.add(
                FilterConfig(
                    columnId = "priority",
                    type = FilterType.SELECT,
                    value = FilterValue.Select(priorityFilter)
                )
            )
        }

        onFiltersChange(newFilters)
    }

    val hasActiveFilters = searchText.isNotBlank() || statusFilter.isNotEmpty() || priorityFilter.isNotEmpty()

    Surface(
        modifier = modifier.fillMaxWidth(),
        color = MaterialTheme.colorScheme.surfaceVariant
    ) {
        FlowRow(
            modifier = Modifier
                .fillMaxWidth()
                .padding(12.dp),
            horizontalArrangement = Arrangement.spacedBy(8.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            OutlinedTextField(
                value = searchText,
                onValueChange = { searchText = it },
                placeholder = { Text("タイトル・説明で検索...") },
                modifier = Modifier
                    .widthIn(min = 180.dp, max = 420.dp)
                    .defaultMinSize(minWidth = 220.dp),
                singleLine = true
            )

            var expandedStatus by remember { mutableStateOf(false) }
            Box {
                OutlinedButton(
                    onClick = { expandedStatus = true },
                    modifier = Modifier.widthIn(min = 144.dp)
                ) {
                    Text(if (statusFilter.isEmpty()) "すべてのステータス" else statusFilter.first())
                }
                DropdownMenu(
                    expanded = expandedStatus,
                    onDismissRequest = { expandedStatus = false }
                ) {
                    DropdownMenuItem(
                        text = { Text("すべてのステータス") },
                        onClick = {
                            statusFilter = emptyList()
                            expandedStatus = false
                        }
                    )
                    TodoStatus.values().forEach { status ->
                        DropdownMenuItem(
                            text = { Text(status.displayName) },
                            onClick = {
                                statusFilter = listOf(status.name)
                                expandedStatus = false
                            }
                        )
                    }
                }
            }

            var expandedPriority by remember { mutableStateOf(false) }
            Box {
                OutlinedButton(
                    onClick = { expandedPriority = true },
                    modifier = Modifier.widthIn(min = 144.dp)
                ) {
                    Text(if (priorityFilter.isEmpty()) "すべての優先度" else priorityFilter.first())
                }
                DropdownMenu(
                    expanded = expandedPriority,
                    onDismissRequest = { expandedPriority = false }
                ) {
                    DropdownMenuItem(
                        text = { Text("すべての優先度") },
                        onClick = {
                            priorityFilter = emptyList()
                            expandedPriority = false
                        }
                    )
                    TodoPriority.values().forEach { priority ->
                        DropdownMenuItem(
                            text = { Text(priority.displayName) },
                            onClick = {
                                priorityFilter = listOf(priority.name)
                                expandedPriority = false
                            }
                        )
                    }
                }
            }

            if (hasActiveFilters) {
                OutlinedButton(
                    onClick = {
                        searchText = ""
                        statusFilter = emptyList()
                        priorityFilter = emptyList()
                    }
                ) {
                    Text("クリア")
                }
            }
        }
    }
}
