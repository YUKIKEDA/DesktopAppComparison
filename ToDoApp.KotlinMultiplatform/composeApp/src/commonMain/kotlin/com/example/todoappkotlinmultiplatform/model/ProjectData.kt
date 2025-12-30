package com.example.todoappkotlinmultiplatform.model

import kotlinx.serialization.Serializable

@Serializable
data class ProjectData(
    val items: List<TodoItem> = emptyList()
)
