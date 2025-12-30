package com.example.todoappkotlinmultiplatform.ui.components

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.example.todoappkotlinmultiplatform.model.TodoItem
import com.example.todoappkotlinmultiplatform.model.TodoPriority
import com.example.todoappkotlinmultiplatform.model.TodoStatus
import com.example.todoappkotlinmultiplatform.viewmodel.TodoItemInput

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TodoForm(
    item: TodoItem?,
    onSubmit: (TodoItemInput) -> Unit,
    onCancel: () -> Unit,
    modifier: Modifier = Modifier
) {
    var title by remember { mutableStateOf(item?.title ?: "") }
    var description by remember { mutableStateOf(item?.description ?: "") }
    var status by remember { mutableStateOf(item?.status ?: TodoStatus.未着手) }
    var priority by remember { mutableStateOf(item?.priority ?: TodoPriority.中) }
    var dueDate by remember { mutableStateOf(item?.dueDate?.takeIf { it.isNotBlank() } ?: "") }

    var titleError by remember { mutableStateOf<String?>(null) }

    Column(
        modifier = modifier.fillMaxWidth(),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        OutlinedTextField(
            value = title,
            onValueChange = {
                title = it
                titleError = if (it.isBlank()) "タイトルは必須です" else if (it.length > 200) "タイトルは200文字以内です" else null
            },
            label = { Text("タイトル") },
            modifier = Modifier.fillMaxWidth(),
            singleLine = true,
            isError = titleError != null,
            supportingText = titleError?.let { { Text(it) } }
        )

        OutlinedTextField(
            value = description,
            onValueChange = {
                description = it
            },
            label = { Text("説明") },
            modifier = Modifier.fillMaxWidth(),
            minLines = 3,
            maxLines = 5,
            supportingText = if (description.length > 500) {
                { Text("説明は500文字以内です", color = MaterialTheme.colorScheme.error) }
            } else null
        )

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            var expandedStatus by remember { mutableStateOf(false) }
            Box(modifier = Modifier.weight(1f)) {
                ExposedDropdownMenuBox(
                    expanded = expandedStatus,
                    onExpandedChange = { expandedStatus = !expandedStatus }
                ) {
                    OutlinedTextField(
                        value = status.displayName,
                        onValueChange = {},
                        readOnly = true,
                        label = { Text("ステータス") },
                        trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = expandedStatus) },
                        modifier = Modifier
                            .menuAnchor()
                            .fillMaxWidth()
                    )
                    ExposedDropdownMenu(
                        expanded = expandedStatus,
                        onDismissRequest = { expandedStatus = false }
                    ) {
                        TodoStatus.values().forEach { statusOption ->
                            DropdownMenuItem(
                                text = { Text(statusOption.displayName) },
                                onClick = {
                                    status = statusOption
                                    expandedStatus = false
                                }
                            )
                        }
                    }
                }
            }

            var expandedPriority by remember { mutableStateOf(false) }
            Box(modifier = Modifier.weight(1f)) {
                ExposedDropdownMenuBox(
                    expanded = expandedPriority,
                    onExpandedChange = { expandedPriority = !expandedPriority }
                ) {
                    OutlinedTextField(
                        value = priority.displayName,
                        onValueChange = {},
                        readOnly = true,
                        label = { Text("優先度") },
                        trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = expandedPriority) },
                        modifier = Modifier
                            .menuAnchor()
                            .fillMaxWidth()
                    )
                    ExposedDropdownMenu(
                        expanded = expandedPriority,
                        onDismissRequest = { expandedPriority = false }
                    ) {
                        TodoPriority.values().forEach { priorityOption ->
                            DropdownMenuItem(
                                text = { Text(priorityOption.displayName) },
                                onClick = {
                                    priority = priorityOption
                                    expandedPriority = false
                                }
                            )
                        }
                    }
                }
            }
        }

        OutlinedTextField(
            value = dueDate,
            onValueChange = { dueDate = it },
            label = { Text("期限") },
            modifier = Modifier.fillMaxWidth(),
            placeholder = { Text("yyyy-MM-ddTHH:mm") },
            supportingText = { Text("例: 2024-12-31T23:59") }
        )

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.End,
            verticalAlignment = androidx.compose.ui.Alignment.CenterVertically
        ) {
            OutlinedButton(onClick = onCancel) {
                Text("キャンセル")
            }
            Spacer(modifier = Modifier.width(8.dp))
            Button(
                onClick = {
                    if (title.isNotBlank() && titleError == null) {
                        onSubmit(
                            TodoItemInput(
                                title = title,
                                description = description,
                                status = status,
                                priority = priority,
                                dueDate = dueDate.takeIf { it.isNotBlank() },
                                isCompleted = item?.isCompleted ?: false
                            )
                        )
                    }
                },
                enabled = title.isNotBlank() && titleError == null
            ) {
                Text(if (item != null) "更新" else "追加")
            }
        }
    }
}
