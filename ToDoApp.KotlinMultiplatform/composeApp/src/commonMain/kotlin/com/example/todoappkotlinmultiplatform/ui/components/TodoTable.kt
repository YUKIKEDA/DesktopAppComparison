package com.example.todoappkotlinmultiplatform.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.runtime.snapshotFlow
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.example.todoappkotlinmultiplatform.model.SortConfig
import com.example.todoappkotlinmultiplatform.model.SortDirection
import com.example.todoappkotlinmultiplatform.model.TodoItem
import com.example.todoappkotlinmultiplatform.util.formatDateISO
import com.example.todoappkotlinmultiplatform.util.formatDateTimeISO
import kotlin.math.min

private const val PAGE_SIZE = 100

@Composable
fun TodoTable(
    items: List<TodoItem>,
    selectedIds: Set<Int>,
    sorts: List<SortConfig>,
    onToggleSelection: (Int) -> Unit,
    onSelectAll: (List<Int>) -> Unit,
    onDeselectAll: (List<Int>) -> Unit,
    onEdit: (TodoItem) -> Unit,
    onSortClick: ((String) -> Unit)? = null,
    visibleCount: Int = PAGE_SIZE,
    onExpandVisible: (() -> Unit)? = null,
    onResetVisible: (() -> Unit)? = null,
    modifier: Modifier = Modifier
) {
    val listState = rememberLazyListState()
    var localVisibleCount by remember { mutableIntStateOf(PAGE_SIZE) }
    val effectiveVisible = if (onExpandVisible != null) visibleCount else localVisibleCount

    // Reset lazy window when the filtered/sorted source changes.
    LaunchedEffect(items) {
        if (onResetVisible != null) {
            onResetVisible()
        } else {
            localVisibleCount = PAGE_SIZE
        }
    }

    val displayItems = remember(items, effectiveVisible) {
        items.take(min(effectiveVisible, items.size))
    }

    // Load more when scrolled near the end.
    LaunchedEffect(listState, items.size, effectiveVisible) {
        snapshotFlow {
            val info = listState.layoutInfo
            val lastVisible = info.visibleItemsInfo.lastOrNull()?.index ?: 0
            val total = info.totalItemsCount
            lastVisible to total
        }.collect { (lastVisible, total) ->
            if (total == 0) return@collect
            if (lastVisible >= total - 5 && effectiveVisible < items.size) {
                if (onExpandVisible != null) {
                    onExpandVisible()
                } else {
                    localVisibleCount = min(localVisibleCount + PAGE_SIZE, items.size)
                }
            }
        }
    }

    Column(
        modifier = modifier.fillMaxSize()
    ) {
        // Header
        Surface(
            modifier = Modifier.fillMaxWidth(),
            color = MaterialTheme.colorScheme.surfaceVariant,
            shadowElevation = 1.dp
        ) {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(8.dp),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                TableHeaderCheckbox(
                    items = items,
                    selectedIds = selectedIds,
                    onSelectAll = onSelectAll,
                    onDeselectAll = onDeselectAll,
                    modifier = Modifier.width(50.dp)
                )
                TableHeader("ID", "id", sorts, onSortClick, modifier = Modifier.width(80.dp))
                TableHeader("タイトル", "title", sorts, onSortClick, modifier = Modifier.width(200.dp))
                TableHeader("説明", "description", sorts, onSortClick, modifier = Modifier.width(300.dp))
                TableHeader("ステータス", "status", sorts, onSortClick, modifier = Modifier.width(120.dp))
                TableHeader("優先度", "priority", sorts, onSortClick, modifier = Modifier.width(100.dp))
                TableHeader("期限", "dueDate", sorts, onSortClick, modifier = Modifier.width(120.dp))
                TableHeader("作成日時", "createdAt", sorts, onSortClick, modifier = Modifier.width(160.dp))
                TableHeader("更新日時", "updatedAt", sorts, onSortClick, modifier = Modifier.width(160.dp))
                Spacer(modifier = Modifier.width(100.dp))
            }
        }

        HorizontalDivider()

        // Body
        LazyColumn(
            state = listState,
            modifier = Modifier.fillMaxSize()
        ) {
            itemsIndexed(
                items = displayItems,
                key = { _, item -> item.id }
            ) { index, item ->
                val isSelected = selectedIds.contains(item.id)
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .background(
                            if (isSelected) MaterialTheme.colorScheme.primaryContainer.copy(alpha = 0.3f)
                            else Color.Transparent
                        )
                        .clickable { onToggleSelection(item.id) }
                        .padding(8.dp),
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    Checkbox(
                        checked = isSelected,
                        onCheckedChange = { onToggleSelection(item.id) },
                        modifier = Modifier.width(50.dp)
                    )
                    TableCell(item.id.toString(), modifier = Modifier.width(80.dp))
                    TableCell(item.title, modifier = Modifier.width(200.dp))
                    TableCell(item.description.ifEmpty { "-" }, modifier = Modifier.width(300.dp))
                    TableCell(item.status.displayName, modifier = Modifier.width(120.dp))
                    TableCell(item.priority.displayName, modifier = Modifier.width(100.dp))
                    TableCell(
                        item.dueDate?.let { formatDateISO(it) } ?: "-",
                        modifier = Modifier.width(120.dp)
                    )
                    TableCell(formatDateTimeISO(item.createdAt), modifier = Modifier.width(160.dp))
                    TableCell(formatDateTimeISO(item.updatedAt), modifier = Modifier.width(160.dp))
                    Button(
                        onClick = { onEdit(item) },
                        modifier = Modifier.width(100.dp)
                    ) {
                        Text("編集")
                    }
                }
                if (index < displayItems.size - 1) {
                    HorizontalDivider()
                }
            }
        }
    }
}

@Composable
private fun TableHeader(
    label: String,
    columnId: String,
    sorts: List<SortConfig>,
    onSortClick: ((String) -> Unit)?,
    modifier: Modifier = Modifier
) {
    val sortConfig = sorts.find { it.columnId == columnId }
    Row(
        modifier = modifier
            .padding(horizontal = 8.dp)
            .then(
                if (onSortClick != null) {
                    Modifier.clickable { onSortClick(columnId) }
                } else {
                    Modifier
                }
            ),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(
            text = label,
            style = MaterialTheme.typography.labelMedium,
            fontWeight = MaterialTheme.typography.labelMedium.fontWeight
        )
        if (sortConfig != null) {
            Text(
                text = if (sortConfig.direction == SortDirection.ASC) "↑" else "↓",
                style = MaterialTheme.typography.labelSmall
            )
        }
    }
}

@Composable
private fun TableCell(
    text: String,
    modifier: Modifier = Modifier
) {
    Text(
        text = text,
        style = MaterialTheme.typography.bodyMedium,
        modifier = modifier.padding(horizontal = 8.dp),
        maxLines = 1,
        overflow = TextOverflow.Ellipsis
    )
}

@Composable
private fun TableHeaderCheckbox(
    items: List<TodoItem>,
    selectedIds: Set<Int>,
    onSelectAll: (List<Int>) -> Unit,
    onDeselectAll: (List<Int>) -> Unit,
    modifier: Modifier = Modifier
) {
    val allSelected = items.isNotEmpty() && items.all { selectedIds.contains(it.id) }

    Checkbox(
        checked = allSelected,
        onCheckedChange = {
            if (it) {
                onSelectAll(items.map { item -> item.id })
            } else {
                onDeselectAll(items.map { item -> item.id })
            }
        },
        modifier = modifier
    )
}
