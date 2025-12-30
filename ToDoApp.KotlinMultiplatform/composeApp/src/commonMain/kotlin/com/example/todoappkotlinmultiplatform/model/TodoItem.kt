package com.example.todoappkotlinmultiplatform.model

import kotlinx.serialization.Serializable

@Serializable
data class TodoItem(
    val id: Int,
    val title: String,
    val description: String = "",
    val status: TodoStatus,
    val priority: TodoPriority,
    val dueDate: String? = null,
    val createdAt: String,
    val updatedAt: String,
    val isCompleted: Boolean = false
)

@Serializable
enum class TodoStatus(val displayName: String) {
    未着手("未着手"),
    進行中("進行中"),
    完了("完了")
}

@Serializable
enum class TodoPriority(val displayName: String) {
    低("低"),
    中("中"),
    高("高")
}
